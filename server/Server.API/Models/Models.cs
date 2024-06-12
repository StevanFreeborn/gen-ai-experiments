namespace Server.API.Models;

record WritingAssistanceRequest(string Input, string ExistingContent);

record AutoCompleteRequest(string Input);

record CompletionResult
{
  public string Completion { get; init; } = string.Empty;
}

record MappingResult(List<int> Mappings);

record Control(decimal Id, string Name, List<int> CitationIds)
{
  public override string ToString() => $"{Name}";
}

record Citation(decimal Id, string Guidance)
{
  public override string ToString() => $@"""
    <citation id=""{Id}"">
      {Guidance}
    </citation>
  """;
}

record ImportAnalysisResult(string AppName, List<ColumnResult> Columns);

record ColumnResult(string Name, string Description, ColumnType Type);

[JsonConverter(typeof(JsonStringEnumConverter))]
enum ColumnType
{
  Text,
  Number,
  Date,
  List,
}

class ChromaOptions
{
  public string BaseUrl { get; set; } = string.Empty;
}

class ChromaOptionsSetup(IConfiguration config) : IConfigureOptions<ChromaOptions>
{
  private const string SectionName = nameof(ChromaOptions);
  private readonly IConfiguration _config = config;

  public void Configure(ChromaOptions options)
  {
    _config.GetSection(SectionName).Bind(options);
  }
}

class OllamaTextEmbeddingOptions
{
  public string BaseUrl { get; set; } = string.Empty;
  public string ModelId { get; set; } = string.Empty;
}

class OllamaTextEmbeddingOptionsSetup(IConfiguration config) : IConfigureOptions<OllamaTextEmbeddingOptions>
{
  private const string SectionName = nameof(OllamaTextEmbeddingOptions);
  private readonly IConfiguration _config = config;

  public void Configure(OllamaTextEmbeddingOptions options)
  {
    _config.GetSection(SectionName).Bind(options);
  }
}