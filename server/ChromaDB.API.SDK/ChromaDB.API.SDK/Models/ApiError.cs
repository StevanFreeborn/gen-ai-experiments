namespace ChromaDB.API.SDK.Models;

public class ApiError
{
  public string Error { get; init; } = "An error was encountered.";

  public ApiError()
  {
  }

  public ApiError(string error)
  {
    Error = error;
  }
}

public class ApiValidationError : ApiError
{
}