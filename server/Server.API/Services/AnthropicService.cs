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
  Task<string> GenerateResponseAsync(string userInput);
  Task<string> GenerateCompletionAsync(string userInput);
}

class AnthropicService(HttpClient httpClient, IOptions<AnthropicOptions> options, IPromptBuilder builder) : IAnthropicService
{
  private readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };
  private const string Model = AnthropicModels.Claude3Haiku;
  private readonly AnthropicClient _client = new(options.Value.Key, httpClient);
  private readonly IPromptBuilder _promptBuilder = builder;

  public async Task<string> GenerateResponseAsync(string userInput)
  {
    var msgParams = new MessageParameters()
    {
      Messages = [new(RoleType.User, userInput)],
      Model = Model,
      MaxTokens = 1024,
      Stream = false,
      Temperature = 0.7m,
    };

    var response = await _client.Messages.GetClaudeMessageAsync(msgParams);
    return response.Message;
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
      Temperature = 0.7m,
    };

    var response = await _client.Messages.GetClaudeMessageAsync(msgParams);
    var result = TryParseCompletion(response.Message);
    return result.Completion;
  }

  private CompletionResult TryParseCompletion(string completion)
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