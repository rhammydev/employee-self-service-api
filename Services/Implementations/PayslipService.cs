using EmployeeSelfServiceAPI.Domain.Entities.Identity;
using EmployeeSelfServiceAPI.Domain.Entities.Payroll;
using EmployeeSelfServiceAPI.Domain.Enums;
using EmployeeSelfServiceAPI.DTOs.Generic;
using EmployeeSelfServiceAPI.DTOs.Payroll;
using EmployeeSelfServiceAPI.Helpers;
using EmployeeSelfServiceAPI.Repositories.Interfaces;
using EmployeeSelfServiceAPI.Services.Interfaces;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class PayslipService(
    IPayslipRepository payslipRepository,
    IEmployeeRepository employeeRepository,
    INotificationService notificationService,
    IPdfService pdfService,
    IUnitOfWork unitOfWork,
    IAppClock clock) : IPayslipService
{
    private const string NotFoundMessage = "Payslip not found.";
    private const string Currency = "NGN";

    public async Task<ApiResponse> CreateAsync(int financeId, CreatePayslipRequest request)
    {
        if (request.EmployeeId == financeId)
            return ApiResponse.Forbidden("You cannot prepare your own payslip. Another Finance user must.");

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);
        if (employee == null)
            return ApiResponse.Fail("Employee does not exist.", 400, ResponseCodes.BadRequest,
                [new ApiError("employeeId", "Employee does not exist.")]);

        if (await payslipRepository.ExistsAsync(request.EmployeeId, request.Month, request.Year))
            return ApiResponse.Conflict("A payslip already exists for this employee and period. Create a revision instead.");

        var now = clock.UtcNow;
        var version = new PayslipVersion
        {
            VersionNumber = 1,
            Status = PayslipVersionStatus.Draft,
            PreparedById = financeId,
            CreatedAt = now,
            UpdatedAt = now
        };
        var amountError = ApplyAmounts(version, request);
        if (amountError != null) return amountError;

        var payslip = new Payslip
        {
            EmployeeId = request.EmployeeId,
            Month = request.Month,
            Year = request.Year,
            CreatedAt = now,
            UpdatedAt = now
        };
        payslip.Versions.Add(version);

        await payslipRepository.AddAsync(payslip);
        await unitOfWork.SaveChangesAsync();   // unique (EmployeeId, Year, Month) catches a race -> 409

        return ApiResponse.Success("Payslip created as draft.",
            new { payslip.Id, version.VersionNumber, version.GrossPay, version.NetPay }, 201);
    }

    public async Task<ApiResponse> UpdateDraftAsync(int financeId, int payslipId, PayslipAmountsRequest request)
    {
        var (payslip, error) = await LoadForFinanceAsync(financeId, payslipId);
        if (error != null) return error;

        var draft = payslip!.Versions.FirstOrDefault(v => v.Status == PayslipVersionStatus.Draft);
        if (draft == null)
            return ApiResponse.Conflict("There is no draft to edit. Published versions are locked; create a revision.");

        foreach (var line in draft.LineItems.ToList())
        {
            payslipRepository.RemoveLineItem(line);
            draft.LineItems.Remove(line);
        }

        var amountError = ApplyAmounts(draft, request);
        if (amountError != null) return amountError;

        draft.PreparedById = financeId;
        draft.UpdatedAt = clock.UtcNow;
        payslip.UpdatedAt = clock.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success("Draft updated.", new { draft.VersionNumber, draft.GrossPay, draft.NetPay });
    }

    public async Task<ApiResponse> CreateRevisionAsync(int financeId, int payslipId, CreateRevisionRequest request)
    {
        var (payslip, error) = await LoadForFinanceAsync(financeId, payslipId);
        if (error != null) return error;

        if (payslip!.Versions.Any(v => v.Status == PayslipVersionStatus.Draft))
            return ApiResponse.Conflict("A draft is already pending for this payslip. Edit or publish it first.");
        if (payslip.Versions.All(v => v.Status != PayslipVersionStatus.Published))
            return ApiResponse.Conflict("This payslip has not been published yet; edit the draft instead.");

        var now = clock.UtcNow;
        var revision = new PayslipVersion
        {
            VersionNumber = payslip.Versions.Max(v => v.VersionNumber) + 1,
            Status = PayslipVersionStatus.Draft,
            RevisionReason = request.Reason.Trim(),
            PreparedById = financeId,
            CreatedAt = now,
            UpdatedAt = now
        };
        var amountError = ApplyAmounts(revision, request);
        if (amountError != null) return amountError;

        payslip.Versions.Add(revision);
        payslip.UpdatedAt = now;

        await unitOfWork.SaveChangesAsync();

        return ApiResponse.Success($"Revision draft (version {revision.VersionNumber}) created.",
            new { revision.VersionNumber, revision.GrossPay, revision.NetPay }, 201);
    }

    public async Task<ApiResponse> PublishAsync(int financeId, int payslipId)
    {
        var (payslip, error) = await LoadForFinanceAsync(financeId, payslipId);
        if (error != null) return error;

        var draft = payslip!.Versions.FirstOrDefault(v => v.Status == PayslipVersionStatus.Draft);
        if (draft == null)
            return ApiResponse.Conflict("There is no draft to publish.");

        var previous = payslip.Versions.FirstOrDefault(v => v.Status == PayslipVersionStatus.Published);
        var now = clock.UtcNow;

        await using var transaction = await unitOfWork.BeginTransactionAsync();

        if (previous != null)
        {
            previous.Status = PayslipVersionStatus.Superseded;   // PAY-01.6
            previous.SupersededAt = now;
            previous.UpdatedAt = now;
            await unitOfWork.SaveChangesAsync();
        }

        draft.Status = PayslipVersionStatus.Published;           // PAY-01.4: financial details locked
        draft.PublishedById = financeId;
        draft.PublishedAt = now;
        draft.UpdatedAt = now;
        payslip.UpdatedAt = now;

        var period = new DateOnly(payslip.Year, payslip.Month, 1).ToString("MMMM yyyy");
        await notificationService.NotifyAsync(payslip.Employee!,
            previous == null ? "Your payslip is ready" : "Your payslip was revised",
            previous == null
                ? $"Your payslip for {period} has been published. Sign in to view or download it."
                : $"Your payslip for {period} was revised (version {draft.VersionNumber}). Sign in to view the current version.",
            relatedEntity: nameof(Payslip), relatedEntityId: payslip.Id);   // no attachment (PAY-01.4)

        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        return ApiResponse.Success(previous == null
            ? "Payslip published."
            : $"Revision published. Version {previous.VersionNumber} is now superseded.");
    }

    public async Task<ApiResponse> GetAllAsync(PayslipQuery query)
    {
        var page = await payslipRepository.GetPagedAsync(query);
        return ApiResponse.Paged("Payslips retrieved.", new PagedResult<PayslipSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(p => ToSummary(p, includeDrafts: true)).ToList()
        });
    }

    public async Task<ApiResponse> GetMineAsync(int employeeId, PayslipQuery query)
    {
        query.EmployeeId = null;
        var page = await payslipRepository.GetPagedPublishedForEmployeeAsync(employeeId, query);
        return ApiResponse.Paged("Payslips retrieved.", new PagedResult<PayslipSummaryResponse>
        {
            Pagination = page.Pagination,
            Items = page.Items.Select(p => ToSummary(p, includeDrafts: false)).ToList()
        });
    }

    public async Task<ApiResponse> GetByIdAsync(int userId, IReadOnlyList<UserRole> roles, int payslipId)
    {
        var payslip = await payslipRepository.GetDetailsAsync(payslipId);
        var (canSeeDrafts, error) = CheckAccess(payslip, userId, roles);
        if (error != null) return error;

        var versions = payslip!.Versions
            .Where(v => canSeeDrafts || v.Status != PayslipVersionStatus.Draft)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => ToVersion(payslip.Id, v))
            .ToList();

        return ApiResponse.Success("Payslip retrieved.", new PayslipDetailResponse
        {
            Summary = ToSummary(payslip, canSeeDrafts),
            Versions = versions
        });
    }

    public async Task<(ApiResponse? Error, FileDownload? File)> GetPdfAsync(int userId, IReadOnlyList<UserRole> roles,
        int payslipId, int? versionNumber)
    {
        var payslip = await payslipRepository.GetDetailsAsync(payslipId);
        var (_, error) = CheckAccess(payslip, userId, roles);
        if (error != null) return (error, null);

        var version = versionNumber == null
            ? payslip!.Versions.FirstOrDefault(v => v.Status == PayslipVersionStatus.Published)
            : payslip!.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber
                                                   && v.Status != PayslipVersionStatus.Draft);
        if (version == null)
            return (ApiResponse.NotFound("No published payslip version found."), null);

        var pdf = pdfService.GeneratePayslip(payslip, version, clock.UtcNow);
        var fileName = $"payslip-{payslip.Employee!.EmployeeNumber}-{payslip.Year}-{payslip.Month:00}-v{version.VersionNumber}.pdf";

        return (null, new FileDownload(new MemoryStream(pdf), "application/pdf", fileName));
    }

    // ---- helpers ----

    private async Task<(Payslip? Payslip, ApiResponse? Error)> LoadForFinanceAsync(int financeId, int payslipId)
    {
        var payslip = await payslipRepository.GetForUpdateAsync(payslipId);
        if (payslip == null)
            return (null, ApiResponse.NotFound(NotFoundMessage));
        if (payslip.EmployeeId == financeId)
            return (null, ApiResponse.Forbidden("You cannot prepare or publish your own payslip. Another Finance user must."));
        return (payslip, null);
    }

    private static (bool CanSeeDrafts, ApiResponse? Error) CheckAccess(Payslip? payslip, int userId, IReadOnlyList<UserRole> roles)
    {
        if (payslip == null)
            return (false, ApiResponse.NotFound(NotFoundMessage));

        if (payslip.EmployeeId == userId)
        {
            return payslip.Versions.Any(v => v.Status == PayslipVersionStatus.Published)
                ? (false, null)
                : (false, ApiResponse.NotFound(NotFoundMessage));
        }

        return roles.Contains(UserRole.Finance)
            ? (true, null)
            : (false, ApiResponse.Forbidden("You do not have access to this payslip."));
    }

    private static ApiResponse? ApplyAmounts(PayslipVersion version, PayslipAmountsRequest request)
    {
        var (allowances, deductions, gross, net) = PayslipCalculator.Totals(
            request.BasicSalary,
            request.Allowances.Select(a => a.Amount),
            request.Deductions.Select(d => d.Amount));

        // PAY-01.3
        if (deductions > gross)
            return ApiResponse.Fail($"Deductions ({deductions:N2}) cannot exceed gross pay ({gross:N2}).", 400);

        version.Currency = Currency;
        version.BasicSalary = PayslipCalculator.Round(request.BasicSalary);
        version.TotalAllowances = allowances;
        version.TotalDeductions = deductions;
        version.GrossPay = gross;
        version.NetPay = net;

        var order = 1;
        foreach (var line in request.Allowances)
            version.LineItems.Add(new PayslipLineItem
                { Type = PayslipLineType.Allowance, Name = line.Name.Trim(), Amount = PayslipCalculator.Round(line.Amount), SortOrder = order++ });
        foreach (var line in request.Deductions)
            version.LineItems.Add(new PayslipLineItem
                { Type = PayslipLineType.Deduction, Name = line.Name.Trim(), Amount = PayslipCalculator.Round(line.Amount), SortOrder = order++ });

        return null;
    }

    private static PayslipSummaryResponse ToSummary(Payslip p, bool includeDrafts)
    {
        var current = p.Versions.FirstOrDefault(v => v.Status == PayslipVersionStatus.Published);
        return new PayslipSummaryResponse
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = Name(p.Employee),
            EmployeeNumber = p.Employee?.EmployeeNumber ?? string.Empty,
            Month = p.Month,
            Year = p.Year,
            CurrentVersion = current?.VersionNumber,
            NetPay = current?.NetPay,
            HasPendingDraft = includeDrafts && p.Versions.Any(v => v.Status == PayslipVersionStatus.Draft),
            PublishedAt = current?.PublishedAt
        };
    }

    private static PayslipVersionResponse ToVersion(int payslipId, PayslipVersion v) => new()
    {
        VersionNumber = v.VersionNumber,
        Status = v.Status,
        Currency = v.Currency,
        BasicSalary = v.BasicSalary,
        Allowances = v.LineItems.Where(l => l.Type == PayslipLineType.Allowance).OrderBy(l => l.SortOrder)
            .Select(l => new PayslipLineResponse { Name = l.Name, Amount = l.Amount }).ToList(),
        Deductions = v.LineItems.Where(l => l.Type == PayslipLineType.Deduction).OrderBy(l => l.SortOrder)
            .Select(l => new PayslipLineResponse { Name = l.Name, Amount = l.Amount }).ToList(),
        TotalAllowances = v.TotalAllowances,
        TotalDeductions = v.TotalDeductions,
        GrossPay = v.GrossPay,
        NetPay = v.NetPay,
        RevisionReason = v.RevisionReason,
        PreparedByName = Name(v.PreparedBy),
        PublishedByName = v.PublishedBy == null ? null : Name(v.PublishedBy),
        PublishedAt = v.PublishedAt,
        SupersededAt = v.SupersededAt,
        PdfUrl = v.Status == PayslipVersionStatus.Draft ? null : $"/api/v1/payslips/{payslipId}/pdf?version={v.VersionNumber}"
    };

    private static string Name(Employee? e) => e == null ? string.Empty : $"{e.FirstName} {e.LastName}";
}
