namespace ChromaDB.API.SDK.Models;

public class ApiResponse<T>
{
  public T? Data { get; init; } = default;
  public HttpStatusCode StatusCode { get; init; }
  public bool IsSuccessful => (int)StatusCode < 400;
}

public class ApiResult<T> 
{
  public readonly HttpStatusCode StatusCode;
  public readonly T Value;
  public readonly ApiError Error;
  public readonly HttpResponseMessage Raw;

  private ApiResult(T v, ApiError e, HttpStatusCode statusCode, HttpResponseMessage raw)
  {
    Value = v;
    Error = e;
    StatusCode = statusCode;
    Raw = raw;
  }

  public bool IsOk => (int)StatusCode < 400;

  public static ApiResult<T> Ok(T v, HttpStatusCode statusCode, HttpResponseMessage raw)
  {
    return new(v, default!, statusCode, raw);
  }

  public static ApiResult<T> Err(ApiError e, HttpStatusCode statusCode, HttpResponseMessage raw)
  {
    return new(default!, e, statusCode, raw);
  }

  public R Match<R>(Func<T, R> success, Func<ApiError, R> failure) => IsOk ? success(Value) : failure(Error);
}