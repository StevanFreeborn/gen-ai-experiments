using System.Collections;
using System.Text.RegularExpressions;

using Azure;

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
  Task<string> CreateImportAsync(ImportAnalysisResult importAnalysis, string appUrl, IFormFile importFile);
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
    var page = await CreatePageAsync();
    await LoginAsync(page);

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

    await page.GetByRole(AriaRole.Tab, new() { NameRegex = new("layouts", RegexOptions.IgnoreCase) }).ClickAsync();

    foreach (var column in analysis.Columns)
    {
      await page.Locator("[data-add-button='layout-item']").ClickAsync();

      var layoutItemMenu = page.Locator("[data-add-menu='layout-item']");
      await layoutItemMenu.WaitForAsync();
      await layoutItemMenu.GetByText(new Regex($"{column.Type}", RegexOptions.IgnoreCase)).First.ClickAsync();

      var addFieldDialog = page.GetByRole(AriaRole.Dialog, new() { NameRegex = new($"add.*field", RegexOptions.IgnoreCase) });
      await addFieldDialog.WaitForAsync();
      await addFieldDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("continue", RegexOptions.IgnoreCase) }).ClickAsync();
      await addFieldDialog.WaitForAsync();

      var frame = addFieldDialog.FrameLocator("iframe");
      await frame.Locator(".label:has-text('Field') + .data input").FillAsync(column.Name);
      await frame.Locator(".label:has-text('Description') + .data .content-area.mce-content-body").FillAsync(column.Description);
      await addFieldDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("save", RegexOptions.IgnoreCase) }).ClickAsync();
    }

    var url = page.Url;

    await page.CloseAsync();

    return url;
  }

  public async Task<string> CreateImportAsync(ImportAnalysisResult importAnalysis, string appUrl, IFormFile importFile)
  {
    var page = await CreatePageAsync();
    await LoginAsync(page);

    var importTime = DateTimeOffset.Now;
    await CreateImport(page, importAnalysis, importFile, importTime);

    bool isComplete;

    do
    {
      isComplete = await CheckIfImportHasCompletedAsync(page, importTime);
      await Task.Delay(5000);
    } while (isComplete is false);

    await CreateReport(page, importAnalysis, appUrl);

    var url = page.Url;

    await page.CloseAsync();

    return url;
  }

  private async Task CreateReport(IPage page, ImportAnalysisResult importAnalysis, string appUrl)
  {
    var appId = int.Parse(appUrl.Split('/').Last());
    await page.GotoAsync($"/Report/App/{appId}");
    await page.GetByRole(AriaRole.Button, new() { NameRegex = new("create report", RegexOptions.IgnoreCase) }).ClickAsync();

    var addReportDialog = page.GetByRole(AriaRole.Dialog, new() { NameRegex = new("add new report", RegexOptions.IgnoreCase) });
    await addReportDialog.WaitForAsync();
    await addReportDialog.GetByRole(AriaRole.Radio, new() { NameRegex = new("create as a saved report", RegexOptions.IgnoreCase) }).ClickAsync();
    await addReportDialog.GetByPlaceholder(new Regex("report name", RegexOptions.IgnoreCase)).FillAsync("All Records");
    await addReportDialog.Locator(".label:has-text('Security') + .data").GetByRole(AriaRole.Listbox).ClickAsync();
    await page.GetByRole(AriaRole.Option, new() { NameRegex = new("public", RegexOptions.IgnoreCase) }).ClickAsync();
    await addReportDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("save", RegexOptions.IgnoreCase) }).ClickAsync();

    var reportDesigner = page.GetByRole(AriaRole.Dialog, new() { NameRegex = new("report designer", RegexOptions.IgnoreCase) });
    await reportDesigner.WaitForAsync();

    foreach (var column in importAnalysis.Columns)
    {
      var frame = reportDesigner.FrameLocator("iframe");
      var fieldToDrag = frame.Locator(".item-lists").Locator($".layoutItem.draggable:has-text('{column.Name}')").First;
      var dropZone = frame.Locator(".display-field-container").Locator("[data-column]").Last;
      await fieldToDrag.DragToAsync(dropZone);
    }

    await reportDesigner.GetByRole(AriaRole.Button, new() { NameRegex = new("save changes & run", RegexOptions.IgnoreCase) }).ClickAsync();
    await page.WaitForURLAsync(new Regex(@"/Report/\d+/Display"));
  }

  private async Task<bool> CheckIfImportHasCompletedAsync(IPage page, DateTimeOffset importTime)
  {
    await page.GotoAsync("/Admin/Reporting/Messaging/History", new() { WaitUntil = WaitUntilState.NetworkIdle });

    var grid = page.Locator(".k-grid-content");
    var firstRow = grid.GetByRole(AriaRole.Row).First;

    var toCell = firstRow.GetByRole(AriaRole.Gridcell).Nth(1);
    var subjectCell = firstRow.GetByRole(AriaRole.Gridcell).Nth(4);
    var timeCreatedCell = firstRow.GetByRole(AriaRole.Gridcell).Nth(5);

    var toCellText = await toCell.TextContentAsync();
    var subjectText = await subjectCell.TextContentAsync();
    var timeCreated = await timeCreatedCell.TextContentAsync();

    var timeCreatedDate = DateTimeOffset.TryParse(timeCreated, out var date) ? date : DateTimeOffset.MinValue;

    var importTimeWithoutMilliseconds = new DateTimeOffset(
      importTime.Year,
      importTime.Month,
      importTime.Day,
      importTime.Hour,
      importTime.Minute,
      0,
      importTime.Offset
    );

    return subjectText is not null &&
      toCellText is not null &&
      subjectText.Contains("Onspring Data Import Complete", StringComparison.OrdinalIgnoreCase) &&
      timeCreatedDate >= importTimeWithoutMilliseconds &&
      toCellText.Contains("Onspring CoPilot", StringComparison.OrdinalIgnoreCase);
  }

  private async Task CreateImport(IPage page, ImportAnalysisResult importAnalysis, IFormFile importFile, DateTimeOffset importTime)
  {
    await page.GotoAsync("/Admin/Home");
    await page.Locator("#admin-create-button").HoverAsync();
    await page.Locator("#admin-create-menu").Locator("[data-dialog-function='showAddImportConfig']").ClickAsync();

    var addImportDialog = page.GetByRole(AriaRole.Dialog, new() { NameRegex = new("create import configuration", RegexOptions.IgnoreCase) });
    await addImportDialog.WaitForAsync();

    var importName = $"{importTime.ToUnixTimeMilliseconds()}_{importAnalysis.AppName}_import";
    await addImportDialog.Locator("#Name").FillAsync(importName);
    await addImportDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("save", RegexOptions.IgnoreCase) }).ClickAsync();
    await page.WaitForURLAsync(new Regex(@"/Admin/Integration/Import/\d+/Edit"));

    await page.Locator(".label:has-text('App/Survey') + .data").GetByRole(AriaRole.Listbox).ClickAsync();
    await page.GetByRole(AriaRole.Option, new() { NameRegex = new(importAnalysis.AppName, RegexOptions.IgnoreCase) }).ClickAsync();

    var fileChooser = await page.RunAndWaitForFileChooserAsync(
      async () => await page.Locator(".k-upload-button").ClickAsync()
    );

    using var memoryStream = new MemoryStream();
    await importFile.CopyToAsync(memoryStream);

    await fileChooser.SetFilesAsync(new FilePayload()
    {
      Name = importFile.FileName,
      MimeType = importFile.ContentType,
      Buffer = memoryStream.ToArray(),
    });

    await page.WaitForResponseAsync(new Regex(@"/Admin/Integration/Import/SaveImportFiles"));

    await page.GetByRole(AriaRole.Tab, new() { NameRegex = new("integration settings", RegexOptions.IgnoreCase) }).ClickAsync();

    var recordHandlingSelector = page.Locator(".label:has-text('Record Handling') + .data").GetByRole(AriaRole.Listbox);
    await recordHandlingSelector.ClickAsync();
    await page.GetByRole(AriaRole.Option, new() { NameRegex = new("update content that matches and add new content", RegexOptions.IgnoreCase) }).ClickAsync();

    var listConfigurationGrid = page.Locator(".label:has-text('List Configuration') + .data").Locator("table tbody");
    var listConfigurationRows = await listConfigurationGrid.GetByRole(AriaRole.Row).AllAsync();

    foreach (var row in listConfigurationRows)
    {
      var lastCell = row.GetByRole(AriaRole.Cell).Last;
      await lastCell.GetByRole(AriaRole.Listbox).ClickAsync();
      await page.GetByRole(AriaRole.Option, new() { NameRegex = new("Add any new values found to the existing list", RegexOptions.IgnoreCase) }).ClickAsync();
    }

    await recordHandlingSelector.ClickAsync();
    await page.GetByRole(AriaRole.Option, new() { NameRegex = new("Add new content for each record in the file", RegexOptions.IgnoreCase) }).ClickAsync();

    await page.GetByRole(AriaRole.Link, new() { NameRegex = new("save changes & run", RegexOptions.IgnoreCase) }).ClickAsync();

    var runDialog = page.GetByRole(AriaRole.Dialog, new() { NameRegex = new("run", RegexOptions.IgnoreCase) });
    await runDialog.WaitForAsync();
    await runDialog.GetByRole(AriaRole.Button, new() { NameRegex = new("run", RegexOptions.IgnoreCase) }).ClickAsync();
    await page.WaitForURLAsync(new Regex(@"/Admin/Integration/Import/\d+/Processing"));
  }

  private async Task LoginAsync(IPage page)
  {
    await page.GotoAsync("/Public/Login");
    await page.GetByPlaceholder("Username").FillAsync(_options.CopilotUsername);
    await page.GetByPlaceholder("Password").FillAsync(_options.CopilotPassword);
    await page.GetByRole(AriaRole.Button, new() { NameRegex = new("login", RegexOptions.IgnoreCase) }).ClickAsync();
    await page.WaitForURLAsync(new Regex("/Dashboard"));
  }

  private async Task<IPage> CreatePageAsync()
  {
    var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    var context = await browser.NewContextAsync(new() { BaseURL = _options.InstanceUrl });
    var page = await context.NewPageAsync();
    return page;
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