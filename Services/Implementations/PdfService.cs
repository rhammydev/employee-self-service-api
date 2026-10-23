using System.Globalization;
using EmployeeSelfServiceAPI.Domain.Entities.Training;
using EmployeeSelfServiceAPI.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static EmployeeSelfServiceAPI.Mappings.TrainingMappings;

namespace EmployeeSelfServiceAPI.Services.Implementations;

public class PdfService : IPdfService
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public byte[] GenerateAllowanceStatement(TrainingRequest training, EstacodeAllowance allowance, DateTime generatedAtUtc)
    {
        var employee = training.Employee!;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Estacode Allowance Statement").FontSize(18).Bold();
                    column.Item().Text($"{training.Reference}  ·  Allowance version {allowance.VersionNumber}")
                        .FontColor(Colors.Grey.Darken1);
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Element(c => Section(c, "Employee",
                    [
                        ("Name", FullName(employee)),
                        ("Employee number", employee.EmployeeNumber),
                        ("Grade (at HR approval)", allowance.GradeCode)
                    ]));

                    column.Item().Element(c => Section(c, "Training",
                    [
                        ("Title", training.Title),
                        ("Provider", training.Provider),
                        ("Destination", $"{training.City}, {training.CountryCode}"),
                        ("Training dates", $"{Date(training.TrainingStartDate)} to {Date(training.TrainingEndDate)}"),
                        ("Travel dates", $"{Date(training.DepartureDate)} to {Date(training.ReturnDate)}")
                    ]));

                    var allowanceRows = new List<(string, string)>
                    {
                        ("Daily rate", Money(allowance.Currency, allowance.DailyRate)
                                       + (allowance.IsRateOverridden ? "  (overridden by HR)" : string.Empty)),
                        ("Eligible days", allowance.EligibleDays == allowance.ComputedEligibleDays
                            ? allowance.EligibleDays.ToString(Invariant)
                            : $"{allowance.EligibleDays} (computed {allowance.ComputedEligibleDays})"),
                        ("Allowance amount", Money(allowance.Currency, allowance.Amount))
                    };
                    if (!string.IsNullOrWhiteSpace(allowance.CorrectionReason))
                        allowanceRows.Add(("Correction reason", allowance.CorrectionReason!));
                    column.Item().Element(c => Section(c, "Allowance", allowanceRows));

                    column.Item().Element(c => Section(c, "Other estimated costs (not part of the allowance)",
                    [
                        ("Course fee", Money(training.CourseFeeCurrency, training.CourseFee)),
                        ("Flight estimate", Money(training.FlightEstimateCurrency, training.FlightEstimate))
                    ]));

                    column.Item().Element(c => Section(c, "Approval",
                    [
                        ("Approved by (Finance)", allowance.ApprovedBy == null ? "-" : FullName(allowance.ApprovedBy)),
                        ("Approved at", allowance.ApprovedAt == null ? "-" : Timestamp(allowance.ApprovedAt.Value))
                    ]));
                });

                page.Footer().Column(column =>
                {
                    column.Item().Text("This statement confirms an approved allowance. It does not record a payment.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Generated {Timestamp(generatedAtUtc)}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Section(IContainer container, string title, IEnumerable<(string Label, string Value)> rows)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Text(title).FontSize(12).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(170);
                    columns.RelativeColumn();
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4)
                        .Text(label).FontColor(Colors.Grey.Darken2);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4)
                        .Text(value);
                }
            });
        });
    }

    private static string Money(string currency, decimal amount) =>
        $"{currency} {amount.ToString("N2", Invariant)}";

    private static string Date(DateOnly date) => date.ToString("dd MMM yyyy", Invariant);

    private static string Timestamp(DateTime utc) => utc.ToString("dd MMM yyyy HH:mm", Invariant) + " UTC";
}
