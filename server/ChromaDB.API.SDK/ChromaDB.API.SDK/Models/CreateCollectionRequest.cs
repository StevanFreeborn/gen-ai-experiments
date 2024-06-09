namespace ChromaDB.API.SDK.Models;

public class CreateCollectionRequest
{
  public string Name { get; set; } = string.Empty;
  public Dictionary<string, object> Metadata { get; set; } = [];
  public string Tenant { get; set; } = "default_tenant";
  public string Database { get; set; } = "default_tenant";
}