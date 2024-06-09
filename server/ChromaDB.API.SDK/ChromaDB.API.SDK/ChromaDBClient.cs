
namespace ChromaDB.API.SDK;

public class ChromaDBClient(HttpClient httpClient) : IChromaDBClient
{
  private readonly HttpClient _httpClient = httpClient;

  public async Task<ApiResult<Heartbeat>> GetRootAsync()
  {
    var response = await _httpClient.GetAsync("/api/v1");

    if (response.IsSuccessStatusCode is false)
    {
      var error = await GetApiErrorAsync(response);
      return ApiResult<Heartbeat>.Err(error, response.StatusCode, response);
    }

    var heartbeat = await response.Content.ReadFromJsonAsync<Heartbeat>();

    return heartbeat is null
      ? ApiResult<Heartbeat>.Err(new ApiError(), response.StatusCode, response)
      : ApiResult<Heartbeat>.Ok(heartbeat, response.StatusCode, response);
  }

  public Task<ApiResult<Collection>> CreateCollectionAsync(Action<CreateCollectionRequest> request)
  {
    var createCollectionRequest = new CreateCollectionRequest();
    request(createCollectionRequest);
    return CreateCollectionAsync(createCollectionRequest);
  }

  public async Task<ApiResult<Collection>> CreateCollectionAsync(CreateCollectionRequest request)
  {
    var url = $"/api/v1/collections?tenant={request.Tenant}&database={request.Database}";
    var response = await _httpClient.PostAsJsonAsync(url, new { request.Name, request.Metadata });

    if ((int)response.StatusCode is 422)
    {
      var error = await GetApiValidationErrorAsync(response);
      return ApiResult<Collection>.Err(error, response.StatusCode, response);
    }

    if (response.IsSuccessStatusCode is false)
    {
      var error = await GetApiErrorAsync(response);
      return ApiResult<Collection>.Err(error, response.StatusCode, response);
    }

    var collection = await response.Content.ReadFromJsonAsync<Collection>();

    return collection is null
      ? ApiResult<Collection>.Err(new ApiError(), response.StatusCode, response)
      : ApiResult<Collection>.Ok(collection, response.StatusCode, response);
  }

  private async Task<ApiValidationError> GetApiValidationErrorAsync(HttpResponseMessage response)
  {
    var error = await response.Content.ReadFromJsonAsync<ApiValidationError>();
    return error ?? new ApiValidationError();
  }

  private async Task<ApiError> GetApiErrorAsync(HttpResponseMessage response)
  {
    if (response.Content.Headers.ContentType?.MediaType.Contains("application/json") is true)
    {
      var error = await response.Content.ReadFromJsonAsync<ApiError>();
      return error ?? new ApiError();
    }

    return new ApiError();
  }
}