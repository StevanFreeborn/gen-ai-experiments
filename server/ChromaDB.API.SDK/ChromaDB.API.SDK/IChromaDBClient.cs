using System.Collections.ObjectModel;

public interface IChromaDBClient
{
  Task<ApiResult<Heartbeat>> GetRootAsync();
  Task<ApiResult<Collection>> CreateCollectionAsync(CreateCollectionRequest request);
  Task<ApiResult<Collection>> CreateCollectionAsync(Action<CreateCollectionRequest> request);
}