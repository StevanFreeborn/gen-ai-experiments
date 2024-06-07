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
}

class AnthropicService(HttpClient httpClient, IOptions<AnthropicOptions> options, IPromptBuilder builder) : IAnthropicService
{
  private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };
  private const string Model = AnthropicModels.Claude3Haiku;
  private readonly AnthropicClient _client = new(options.Value.Key, httpClient);
  private readonly IPromptBuilder _promptBuilder = builder;

  public async IAsyncEnumerable<string> GenerateResponseAsync(string userInput, string existingContent)
  {
    var prompt = _promptBuilder.CreateWritingAssistantPrompt(userInput, existingContent);

    var msgParams = new MessageParameters()
    {
      Messages = [new(RoleType.User, prompt)],
      Model = Model,
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
      Model = Model,
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
}

record CompletionResult
{
  public string Completion { get; init; } = string.Empty;
}