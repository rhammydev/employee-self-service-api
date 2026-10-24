namespace EmployeeSelfServiceAPI.DTOs.Generic;

public class PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = new(1, 20, 0);
}