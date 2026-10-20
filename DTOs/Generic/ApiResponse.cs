using System.Text.Json.Serialization;

namespace EmployeeSelfServiceAPI.DTOs.Generic;

public class ApiResponse
{
    public string ResponseCode { get; init; } = string.Empty;
    public string ResponseMessage { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Pagination { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ApiError>? Errors { get; init; }

    [JsonIgnore] public int HttpStatusCode { get; init; }

    public static ApiResponse Success(string message, object? data = null, int statusCode = 200) =>
        new() { ResponseCode = ResponseCodes.Success, ResponseMessage = message, Data = data, HttpStatusCode = statusCode };

    public static ApiResponse Paged<T>(string message, PagedResult<T> page) =>
        new()
        {
            ResponseCode = ResponseCodes.Success, ResponseMessage = message,
            Data = page.Items, Pagination = page.Pagination, HttpStatusCode = 200
        };

    public static ApiResponse Fail(string message, int statusCode = 400, string code = ResponseCodes.BadRequest,
        IReadOnlyList<ApiError>? errors = null) =>
        new() { ResponseCode = code, ResponseMessage = message, Errors = errors, HttpStatusCode = statusCode };

    // Shortcuts for the 403-vs-404 rule and 409s, which every module uses
    public static ApiResponse NotFound(string message) => Fail(message, 404, ResponseCodes.NotFound);
    public static ApiResponse Forbidden(string message) => Fail(message, 403, ResponseCodes.Forbidden);
    public static ApiResponse Conflict(string message) => Fail(message, 409, ResponseCodes.Conflict);
}