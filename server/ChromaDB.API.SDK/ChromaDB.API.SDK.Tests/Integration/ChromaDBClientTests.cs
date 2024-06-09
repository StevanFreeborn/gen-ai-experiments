namespace ChromaDB.API.SDK.Tests.Integration;

public class ChromaDBClientTests(DatabaseFixture databaseFixture) : IClassFixture<DatabaseFixture>
{
  private readonly ChromaDBClient _client = new(new() { BaseAddress = databaseFixture.BaseUrl });

  [Fact]
  public async Task GetRootAsync_WhenCalled_ReturnsHeartbeat()
  {
    var response = await _client.GetRootAsync();

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Value.Should().NotBeNull();
    response.Value.NanosecondHeartbeat.Should().BeGreaterThan(0);
  }

  [Fact]
  public async Task CreateCollectionAsync_WithRequest_ReturnsCollection()
  {
    var collectionName = "HelloWorld";

    var response = await _client.CreateCollectionAsync(r => 
    {
      r.Name = collectionName;
      r.Metadata = new Dictionary<string, object> { { "key", "value" } };
    });

    var responseBody = await response.Raw.Content.ReadAsStringAsync();

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Value.Should().NotBeNull();
    response.Value.Id.Should().NotBeNullOrEmpty();
    response.Value.Name.Should().Be(collectionName);
    response.Value.Metadata["key"].Should().Be("value");
    response.Value.Tenant.Should().Be("default_tenant");
    response.Value.Database.Should().Be("default_tenant");
  }
}