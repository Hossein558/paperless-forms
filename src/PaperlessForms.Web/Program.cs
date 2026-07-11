using PaperlessForms.Web.Components;
using PaperlessForms.Core.Services;
using PaperlessForms.Core.Interfaces;
using Aspose.Cells;

var builder = WebApplication.CreateBuilder(args);

// ─── Aspose.Cells License ───────────────────────────────
var licPath = builder.Configuration["Aspose:LicensePath"]
              ?? Path.Combine(AppContext.BaseDirectory, "Aspose.Total.NET.lic");
if (File.Exists(licPath))
{
    try
    {
        var license = new License();
        license.SetLicense(licPath);
        Console.WriteLine("Aspose License applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Failed to apply Aspose License: {ex.Message}");
    }
}


// ─── Data Services ──────────────────────────────────────
var dataFolder = builder.Configuration["Data:FolderPath"]
                 ?? @"\\datap2\Atlassian\Jira\sharedhome\paperless forms";

// Fallback برای محیط توسعه
if (!Directory.Exists(dataFolder))
{
    Console.WriteLine($"Network path '{dataFolder}' is unreachable. Falling back to local Data folder.");
    dataFolder = Path.Combine(builder.Environment.ContentRootPath, "Data");
}
Directory.CreateDirectory(dataFolder);

var excelService = new ExcelDataService(dataFolder);
builder.Services.AddSingleton<IPartRepository>(excelService);
builder.Services.AddSingleton<IInspectionRepository>(excelService);

// ─── Razor Components / Blazor ──────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
