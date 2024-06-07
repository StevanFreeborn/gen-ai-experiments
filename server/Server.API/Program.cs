using System.Text;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOptions<AnthropicOptionsSetup>();
builder.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddHttpClient<IAnthropicService, AnthropicService>();

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

app.UseCors();

app.Run();


record WritingAssistanceRequest(string Input, string ExistingContent);

record AutoCompleteRequest(string Input);