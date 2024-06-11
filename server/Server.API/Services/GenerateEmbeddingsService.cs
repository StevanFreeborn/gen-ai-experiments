#pragma warning disable SKEXP0001

namespace Server.API.Services;

class GenerateEmbeddingsService(
  IServiceProvider serviceProvider,
  ILogger<GenerateEmbeddingsService> logger
) : IHostedService
{
  private readonly IServiceProvider _serviceProvider = serviceProvider;
  private readonly ILogger<GenerateEmbeddingsService> _logger = logger;

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    using var scope = _serviceProvider.CreateScope();
    var onspringService = scope.ServiceProvider.GetRequiredService<IOnspringService>();
    var memory = scope.ServiceProvider.GetRequiredService<ISemanticTextMemory>();

    _logger.LogInformation("Generating embeddings for citations");

    var citations = await onspringService.GetCitationsAsync();

    await Parallel.ForEachAsync(citations, async (citation, token) =>
    {
      await memory.SaveInformationAsync(
        collection: "citations",
        id: citation.Id.ToString(),
        text: citation.Guidance,
        cancellationToken: token
      );

      _logger.LogInformation("Embeddings generated for citation {Id}", citation.Id);
    });

    _logger.LogInformation("Embeddings generated for all citations");
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}