namespace EmployeeSelfServiceAPI.DTOs.Generic;

public record PaginationMeta(int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}