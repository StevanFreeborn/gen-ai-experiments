namespace ChromaDB.API.SDK.Tests.Integration.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
  private const int Port = 8000;

  private readonly IContainer _container = new ContainerBuilder()
    .WithImage("chromadb/chroma")
    .WithPortBinding(Port, true)
    .WithWaitStrategy(
      Wait.ForUnixContainer()
        .UntilHttpRequestIsSucceeded(r => r.ForPort(Port)
        .ForPath("/api/v1")
        .WithMethod(HttpMethod.Get))
    )
    .Build();

  public Uri BaseUrl => new UriBuilder(Uri.UriSchemeHttp, _container.Hostname, _container.GetMappedPublicPort(Port)).Uri;

  public async Task DisposeAsync() => await _container.DisposeAsync();

  public async Task InitializeAsync() => await _container.StartAsync();
}