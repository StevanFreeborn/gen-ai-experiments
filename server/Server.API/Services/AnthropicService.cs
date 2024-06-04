using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;

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
}

class AnthropicService(HttpClient httpClient, IOptions<AnthropicOptions> options) : IAnthropicService
{
  private const string Model = AnthropicModels.Claude3Haiku;
  private readonly AnthropicClient _client = new(options.Value.Key, httpClient);

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
}