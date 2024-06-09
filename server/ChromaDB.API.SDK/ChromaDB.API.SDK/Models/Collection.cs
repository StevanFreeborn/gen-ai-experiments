namespace ChromaDB.API.SDK.Models;

public class Collection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = [];
    public string Tenant { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}