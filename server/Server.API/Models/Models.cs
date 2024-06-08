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