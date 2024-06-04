using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOptions<AnthropicOptionsSetup>();
builder.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();
builder.Services.AddHttpClient<IAnthropicService, AnthropicService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => new { Message = "Hello, World!" });

app.MapPost("/generate", async ([FromBody] GenerateRequest request, [FromServices] IAnthropicService service) =>
{
  var response = await service.GenerateResponseAsync(request.Input);
  return new { Response = response };
});

app.MapPost("/complete", async ([FromBody] GenerateRequest request, [FromServices] IAnthropicService service) =>
{
  var response = await service.GenerateCompletionAsync(request.Input);
  return new { Response = response };
});

app.Run();


record GenerateRequest(string Input);