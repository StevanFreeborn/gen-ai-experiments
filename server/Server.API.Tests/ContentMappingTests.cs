#pragma warning disable SKEXP0020, SKEXP0001

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.Memory;

namespace Server.API.Tests;

public class ContentMappingTests : IClassFixture<AppFactory>
{
  private readonly IOnspringService _onspringService;
  private readonly IAnthropicService _anthropicService;
  private readonly ISemanticTextMemory _memory;

  public ContentMappingTests(AppFactory factory)
  {
    var scope = factory.Services.CreateScope();
    _onspringService = scope.ServiceProvider.GetRequiredService<IOnspringService>();
    _anthropicService = scope.ServiceProvider.GetRequiredService<IAnthropicService>();
    _memory = scope.ServiceProvider.GetRequiredService<ISemanticTextMemory>();
  }

  [Fact]
  public async Task GivenAControl_AndCitations_WhenAskedToMapThem_ThenShouldReturnExpectedMapping()
  {
    var controlRecord = await _onspringService.GetRandomControlAsync();
    var citations = await _onspringService.GetCitationsAsync();

    var result = await _anthropicService.MapControlToCitationsAsync(controlRecord, citations);

    result.Should().BeEquivalentTo(controlRecord.CitationIds);
  }

  [Fact]
  public async Task GivenAControl_WhenSearchingMemory_ThenShouldReturnCitationEmbeddingForControl()
  {
    var controlRecord = await _onspringService.GetRandomControlAsync();

    var result = _memory.SearchAsync("citations", controlRecord.Name, 20);

    var queryResults = new List<MemoryQueryResult>();

    await foreach (var queryResult in result)
    {
      queryResults.Add(queryResult);
    }

    var citationIds = queryResults.Select(r => int.Parse(r.Metadata.Id));

    citationIds.Should().Contain(controlRecord.CitationIds);
  }
}