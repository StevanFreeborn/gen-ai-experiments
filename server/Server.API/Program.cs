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

var generateEmbeddings = builder.Configuration.GetValue<bool>("GenerateEmbeddings", false);

if (generateEmbeddings)
{
  builder.Services.AddHostedService<GenerateEmbeddingsService>();
}

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

app
  .MapPost("/generate", async ([FromBody] WritingAssistanceRequest request, HttpContext context, [FromServices] IAnthropicService service) =>
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

app
  .MapPost("/complete", async ([FromBody] AutoCompleteRequest request, [FromServices] IAnthropicService service) =>
  {
    var response = await service.GenerateCompletionAsync(request.Input);
    return new { Response = response };
  });

app
  .MapPost("/semantic-search", async ([FromBody] string query, [FromServices] ISemanticTextMemory memory) =>
  {
    var results = memory.SearchAsync(collection: "citations", query, 20);

    var citations = new List<MemoryQueryResult>();

    await foreach (var result in results)
    {
      citations.Add(result);
    }

    return new { Citations = citations };
  });


app
  .MapPost("/analyze-import", async ([FromForm] string hasHeader, IFormFile importFile, [FromServices] IAnthropicService anthropicService) =>
  {
    var fileStream = importFile.OpenReadStream();
    var reader = new StreamReader(fileStream);
    var fileContent = reader.ReadToEnd();
    var rows = fileContent.Split("\n");
    var sampleRowNum = hasHeader == "on" ? 2 : 1;
    var sampleRows = rows.Take(sampleRowNum).ToList();
    var analysis = await anthropicService.GenerateImportAnalysisAsync(sampleRows);
    return Results.Ok(analysis);
  })
  .DisableAntiforgery();

app.MapPost("/create-app", async ([FromBody] ImportAnalysisResult analysis, [FromServices] IOnspringService onspringService) =>
{
  var appUrl = await onspringService.CreateAppAsync(analysis);
  return Results.Ok(new { Url = appUrl });
});

app
  .MapPost("/create-import", async (IFormFile importAnalysisFile, [FromForm] string appUrl, IFormFile importFile, [FromServices] IOnspringService onspringService) =>
  {
    var importAnalysis = await JsonSerializer.DeserializeAsync<ImportAnalysisResult>(importAnalysisFile.OpenReadStream(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (importAnalysis is null)
    {
      return Results.BadRequest("Unable to parse import analysis file.");
    }

    var reportUrl = await onspringService.CreateImportAsync(importAnalysis, appUrl, importFile);
    return Results.Ok(new { Url = reportUrl });
  })
  .DisableAntiforgery();

app.UseCors();

app.Run();


record WritingAssistanceRequest(string Input, string ExistingContent);

record AutoCompleteRequest(string Input);

public partial class Program { }