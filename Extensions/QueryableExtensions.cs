using EmployeeSelfServiceAPI.DTOs.Generic;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSelfServiceAPI.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, PagedRequest request)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        
        return new PagedResult<T>
        {
            Items = items,
            Pagination = new PaginationMeta(request.PageNumber, request.PageSize, totalCount)
        };
    }
}