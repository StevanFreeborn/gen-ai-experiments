namespace Server.API.Services;

class AnthropicOptions
{
  public string Key { get; set; } = string.Empty;
}

class AnthropicOptionsSetup(IConfiguration config) : IConfigureOptions<AnthropicOptions>
{
  private const string SectionName = nameof(AnthropicOptions);
  private readonly IConfiguration _config = config;

  public void Configure(AnthropicOptions options)
  {
    _config.GetSection(SectionName).Bind(options);
  }
}

interface IAnthropicService
{
  IAsyncEnumerable<string> GenerateResponseAsync(string userInput, string existingContent);
  Task<string> GenerateCompletionAsync(string userInput);

  Task<List<int>> MapControlToCitationsAsync(Control control, List<Citation> citations);
}

class AnthropicService(HttpClient httpClient, IOptions<AnthropicOptions> options, IPromptBuilder builder) : IAnthropicService
{
  private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };
  private readonly AnthropicClient _client = new(options.Value.Key, httpClient);
  private readonly IPromptBuilder _promptBuilder = builder;

  public async IAsyncEnumerable<string> GenerateResponseAsync(string userInput, string existingContent)
  {
    var prompt = _promptBuilder.CreateWritingAssistantPrompt(userInput, existingContent);

    var msgParams = new MessageParameters()
    {
      Messages = [new(RoleType.User, prompt)],
      Model = AnthropicModels.Claude3Sonnet,
      MaxTokens = 1024,
      Stream = true,
      Temperature = 1m,
    };

    await foreach (var response in _client.Messages.StreamClaudeMessageAsync(msgParams))
    {
      if (response.Delta is not null)
      {
        yield return response.Delta.Text;
      }
    }
  }

  public async Task<string> GenerateCompletionAsync(string userInput)
  {
    var prompt = _promptBuilder.CreateAutoCompletePrompt(userInput);

    var msgParams = new MessageParameters()
    {
      Messages = [new(RoleType.User, prompt)],
      Model = AnthropicModels.Claude3Haiku,
      MaxTokens = 1024,
      Stream = false,
      Temperature = 1.0m,
    };

    var response = await _client.Messages.GetClaudeMessageAsync(msgParams);
    var result = ParseCompletion(response.Message);
    return result.Completion;
  }



  private CompletionResult ParseCompletion(string completion)
  {
    try
    {
      return JsonSerializer.Deserialize<CompletionResult>(completion, _serializerOptions) ?? new CompletionResult();
    }
    catch (Exception ex) when (ex is JsonException)
    {
      return new CompletionResult();
    }
  }

  public async Task<List<int>> MapControlToCitationsAsync(Control control, List<Citation> citations)
  {
    var prompt = _promptBuilder.CreateControlToCitationMappingPrompt(control.Name, citations);

    var msgParams = new MessageParameters()
    {
      Messages = [new(RoleType.User, prompt)],
      Model = AnthropicModels.Claude3Haiku,
      MaxTokens = 1024,
      Stream = false,
      Temperature = 0m,
    };

    var response = await _client.Messages.GetClaudeMessageAsync(msgParams);
    var result = ParseMapping(response.Message);
    return result.Mappings;
  }

  private MappingResult ParseMapping(string mapping)
  {
    try
    {
      return JsonSerializer.Deserialize<MappingResult>(mapping, _serializerOptions) ?? new MappingResult([]);
    }
    catch (Exception ex) when (ex is JsonException)
    {
      return new MappingResult([]);
    }
  }
}