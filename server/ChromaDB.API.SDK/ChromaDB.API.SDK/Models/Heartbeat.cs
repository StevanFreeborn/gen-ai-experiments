namespace ChromaDB.API.SDK.Models;

public class Heartbeat
{
  [JsonPropertyName("nanosecond heartbeat")]
  public long NanosecondHeartbeat { get; init; }
}