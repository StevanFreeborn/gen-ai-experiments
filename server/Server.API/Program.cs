#pragma warning disable SKEXP0020, SKEXP0001

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOptions<AnthropicOptionsSetup>();
builder.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddHttpClient<IAnthropicService, AnthropicService>();

builder.Services.ConfigureOptions<OnspringOptionsSetup>();
builder.Services.AddTransient<IOnspringClient>(sp =>
{
  var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
  var options = sp.GetRequiredService<IOptions<OnspringOptions>>().Value;
  httpClient.BaseAddress = new Uri(options.BaseUrl);
  return new OnspringClient(options.ApiKey, httpClient);
});
builder.Services.AddScoped<IOnspringService, OnspringService>();

builder.Services.ConfigureOptions<ChromaOptionsSetup>();

builder.Services.AddTransient<IMemoryStore>(sp =>
{
  var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
  var options = sp.GetRequiredService<IOptions<ChromaOptions>>().Value;
  var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
  return new ChromaMemoryStore(httpClient, options.BaseUrl, loggerFactory);
});

builder.Services.ConfigureOptions<OllamaTextEmbeddingOptionsSetup>();
builder.Services.AddTransient<ITextEmbeddingGenerationService, OllamaTextEmbeddingGeneration>(sp =>
{
  var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
  var options = sp.GetRequiredService<IOptions<OllamaTextEmbeddingOptions>>().Value;
  var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
  return new OllamaTextEmbeddingGeneration(options.ModelId, options.BaseUrl, httpClient, loggerFactory);
});

builder.Services.AddTransient<ISemanticTextMemory, SemanticTextMemory>();

// TODO: Add option to enable embeddings generation
// builder.Services.AddHostedService<EmbeddingsService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
  options.AddDefaultPolicy(
    builder => builder
      .AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader()
  )
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => new { Message = "Hello, World!" });

app.MapPost("/generate", async ([FromBody] WritingAssistanceRequest request, HttpContext context, [FromServices] IAnthropicService service) =>
{
  context.Response.ContentType = "text/event-stream; charset=utf-8";
  context.Response.Headers.Append("Cache-Control", "no-cache");
  context.Response.Headers.Append("Connection", "keep-alive");

  await foreach (var response in service.GenerateResponseAsync(request.Input, request.ExistingContent))
  {
    // write event for each response to the stream
    var data = Encoding.UTF8.GetBytes($"data: <response>{response}</response>\n\n");
    await context.Response.Body.WriteAsync(data);
    await context.Response.Body.FlushAsync();
  }
});

app.MapPost("/complete", async ([FromBody] AutoCompleteRequest request, [FromServices] IAnthropicService service) =>
{
  var response = await service.GenerateCompletionAsync(request.Input);
  return new { Response = response };
});

app.MapPost("/semantic-search", async ([FromBody] string query, [FromServices] ISemanticTextMemory memory) =>
{
  var results = memory.SearchAsync(collection: "citations", query, 20);

  var citations = new List<MemoryQueryResult>();

  await foreach (var result in results)
  {
    citations.Add(result);
  }

  return new { Citations = citations };
});


app.UseCors();

app.Run();


record WritingAssistanceRequest(string Input, string ExistingContent);

record AutoCompleteRequest(string Input);