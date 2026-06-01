using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.Entities;

public class ApiResult
{
    public string ErrorMessage { get; set; } = string.Empty;
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
    public int StatusCode { get; set; }

    public static ApiResult Succeeded() => new ApiResult
    {
        StatusCode = 200,
        ErrorMessage = string.Empty,
    };
    public static ApiResult Failed(string errorMessage) => new ApiResult
    {
        StatusCode = 400,
        ErrorMessage = errorMessage,
    };
    public static ApiResult ServerError(string errorMessage) => new ApiResult
    {
        StatusCode = 500,
        ErrorMessage = errorMessage
    };
}

public class ApiResult<T>
{
    public T? Data { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
    public int StatusCode { get; set; }
    public static ApiResult<T> Succeeded(T data) => new ApiResult<T>
    {
        Data = data,
        StatusCode = 200,
        ErrorMessage = string.Empty,
    };
    public static ApiResult<T> Failed(string errorMessage) => new ApiResult<T>
    {

        StatusCode = 400,
        ErrorMessage = errorMessage,
    };
    public static ApiResult<T> ServerError(string errorMessage) => new ApiResult<T>
    {
        StatusCode = 500,
        ErrorMessage = errorMessage
    };
}
