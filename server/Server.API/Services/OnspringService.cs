using System.Collections;
using System.Text.RegularExpressions;

namespace Server.API.Services;

class OnspringOptions
{
  public string BaseUrl { get; set; } = string.Empty;
  public string ApiKey { get; set; } = string.Empty;
  public string InstanceUrl { get; set; } = string.Empty;
  public string CopilotUsername { get; set; } = string.Empty;
  public string CopilotPassword { get; set; } = string.Empty;
}

class OnspringOptionsSetup(IConfiguration config) : IConfigureOptions<OnspringOptions>
{
  private const string SectionName = nameof(OnspringOptions);
  private readonly IConfiguration _config = config;

  public void Configure(OnspringOptions options)
  {
    _config.GetSection(SectionName).Bind(options);
  }
}

interface IOnspringService
{
  Task<string> CreateAppAsync(ImportAnalysisResult analysis);
  Task<List<Citation>> GetCitationsAsync();
  Task<Control> GetRandomControlAsync();
}

class OnspringService(IOnspringClient client, IOptions<OnspringOptions> options) : IOnspringService
{
  private const int ControlAppId = 14;
  private const int ControlIdFieldId = 585;
  private const int ControlNameFieldId = 183;
  private const int ControlCitationIdsFieldId = 586;
  private const int AllCitationsReport = 27;
  private const string CitationsJsonFilePath = "citations.json";
  private const int MaxReportId = 28;
  private const int MinReportId = 29;
  private readonly IOnspringClient _client = client;
  private readonly OnspringOptions _options = options.Value;

  public async Task<List<Citation>> GetCitationsAsync()
  {
    List<Citation>? citations;

    if (File.Exists(CitationsJsonFilePath))
    {
      citations = await ReadCitationsFromJsonFile();
    }
    else
    {
      citations = await GetCitationsFromOnspring();
      await WriteCitationsToJsonFile(citations);
    }

    return citations is null || citations.Count is 0
      ? throw new InvalidOperationException("Expected citations to be populated")
      : citations;
  }

  public async Task<Control> GetRandomControlAsync()
  {
    var minReportResponse = await _client.GetReportAsync(MinReportId);
    var maxReportResponse = await _client.GetReportAsync(MaxReportId);

    var minControl = minReportResponse.Value.Rows.First().Cells.First();
    var maxControl = maxReportResponse.Value.Rows.First().Cells.First();

    if (int.TryParse(minControl.ToString(), out var minControlId) is false)
    {
      throw new InvalidOperationException("Expected control id to be an integer");
    }

    if (int.TryParse(maxControl.ToString(), out var maxControlId) is false)
    {
      throw new InvalidOperationException("Expected control id to be an integer");
    }

    ApiResponse<ResultRecord> controlResponse;

    do
    {
      var randomControlId = Random.Shared.Next(minControlId, maxControlId);
      controlResponse = await _client.GetRecordAsync(
        new() { AppId = ControlAppId, RecordId = randomControlId, FieldIds = [ControlIdFieldId, ControlNameFieldId, ControlCitationIdsFieldId] }
      );
    } while (controlResponse.IsSuccessful is false);

    var idFieldValue = controlResponse.Value.FieldData.First(f => f.FieldId == ControlIdFieldId).GetValue() as decimal?;
    var nameFieldValue = controlResponse.Value.FieldData.First(f => f.FieldId == ControlNameFieldId).GetValue() as string;
    var citationIdsFieldValue = controlResponse.Value.FieldData.FirstOrDefault(f => f.FieldId == ControlCitationIdsFieldId);

    var citationIds = new List<int>();

    if (citationIdsFieldValue?.Type is ResultValueType.String)
    {
      var value = citationIdsFieldValue.GetValue() as string ?? throw new InvalidOperationException("Expected citation id to be a string");
      var citationIdsList = value.Split(',').Select(s => s.Trim()).Select(int.Parse).ToList();
      citationIds.AddRange(citationIdsList);
    }

    var control = new Control(idFieldValue ?? 0, nameFieldValue ?? string.Empty, citationIds);
    return control;
  }

  public async Task<string> CreateAppAsync(ImportAnalysisResult analysis)
  {
    using var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    var context = await browser.NewContextAsync(new() { BaseURL = _options.InstanceUrl });
    var page = await context.NewPageAsync();

    // login
    await page.GotoAsync("/Public/Login");
    await page.GetByPlaceholder("Username").FillAsync(_options.CopilotUsername);
    await page.GetByPlaceholder("Password").FillAsync(_options.CopilotPassword);
    await page.GetByRole(AriaRole.Button, new() { NameRegex = new("login", RegexOptions.IgnoreCase) }).ClickAsync();
    await page.WaitForURLAsync(new Regex("/Dashboard"));

    // create app
    await page.GotoAsync("/Admin/Home");
    await page.Locator("#admin-create-button").HoverAsync();
    await page.Locator("#admin-create-menu").Locator("[data-event='addNewApp']").ClickAsync();

    var createAppDialog = page.GetByRole(AriaRole.Dialog, new() { NameRegex = new("create app", RegexOptions.IgnoreCase) });
    await createAppDialog.WaitForAsync();
    await createAppDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("continue", RegexOptions.IgnoreCase) }).ClickAsync();

    await createAppDialog.WaitForAsync();
    await createAppDialog.GetByLabel(new Regex("name", RegexOptions.IgnoreCase)).FillAsync(analysis.AppName);
    await createAppDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("save", RegexOptions.IgnoreCase) }).ClickAsync();
    await page.WaitForURLAsync(new Regex(@"/Admin/App/\d+"));

    var url = page.Url;

    await page.CloseAsync();
    await context.CloseAsync();
    await browser.CloseAsync();

    return url;
  }


  private async Task<List<Citation>> GetCitationsFromOnspring()
  {
    var citationsReportResponse = await _client.GetReportAsync(AllCitationsReport, default, DataFormat.Formatted);

    var citations = new List<Citation>();

    foreach (var row in citationsReportResponse.Value.Rows)
    {
      var id = row.Cells[0];
      var guidance = row.Cells[1];

      if (int.TryParse(id.ToString(), out var citationId) is false)
      {
        throw new InvalidOperationException("Expected citation id to be an integer");
      }

      citations.Add(new Citation(citationId, guidance.ToString() ?? string.Empty));
    }

    return citations;
  }

  private async Task<List<Citation>?> ReadCitationsFromJsonFile()
  {
    await using var stream = File.OpenRead(CitationsJsonFilePath);
    var citations = await JsonSerializer.DeserializeAsync<List<Citation>>(stream);
    return citations;
  }

  private async Task WriteCitationsToJsonFile(List<Citation> citations)
  {
    await using var stream = File.Create(CitationsJsonFilePath);
    await JsonSerializer.SerializeAsync(stream, citations);
    await stream.FlushAsync();
  }
}