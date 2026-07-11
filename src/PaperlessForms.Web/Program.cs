using PaperlessForms.Web.Components;
using PaperlessForms.Core.Services;
using PaperlessForms.Core.Interfaces;
using Aspose.Cells;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ─── Aspose.Cells License ───────────────────────────────
// The license file must exist in the build output directory.
// It is configured in PaperlessForms.Web.csproj with CopyToOutputDirectory=PreserveNewest.
var licPath = builder.Configuration["Aspose:LicensePath"]
              ?? Path.Combine(AppContext.BaseDirectory, "Aspose.Total.lic");

if (!File.Exists(licPath))
    throw new FileNotFoundException(
        $"[CRITICAL] Aspose license file not found at '{licPath}'. " +
        "Ensure 'Aspose.Total.lic' is present and set to CopyToOutputDirectory in the .csproj. " +
        "Application cannot start in Evaluation mode.");

var aspLicense = new License();
aspLicense.SetLicense(licPath);
Console.WriteLine($"[Aspose] License applied successfully from: {licPath}");



// ─── Data Services ──────────────────────────────────────
var dataFolder = builder.Configuration["Data:FolderPath"]
                 ?? @"\\datap2\Atlassian\Jira\sharedhome\paperless forms";

// Fallback برای محیط توسعه
if (!Directory.Exists(dataFolder))
{
    Console.WriteLine($"[WARNING] Network directory '{dataFolder}' is unreachable. Initializing local Data folder fallback.");
    dataFolder = Path.Combine(builder.Environment.ContentRootPath, "Data");
}
Directory.CreateDirectory(dataFolder);

var excelService = new ExcelDataService(dataFolder);
builder.Services.AddSingleton<IPartRepository>(excelService);
builder.Services.AddSingleton<IInspectionRepository>(excelService);

// ─── Razor Components / Blazor ──────────────────────────
builder.Services.AddScoped<ActiveDirectoryService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", async (HttpContext context, [FromForm] string Username, [FromForm] string Password, [FromForm] string? ReturnUrl, ActiveDirectoryService adService) =>
{
    var result = await adService.AuthenticateAsync(Username, Password);
    if (result.IsSuccess)
    {
        await context.SignInAsync("Cookies", result.Principal!);
        return Results.Redirect(string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl);
    }
    return Results.Redirect($"/login?ErrorMessage={Uri.EscapeDataString(result.ErrorMessage)}&ReturnUrl={Uri.EscapeDataString(ReturnUrl ?? "/")}");
});

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Cookies");
    return Results.Redirect("/login");
});

// ─── Profile Avatar API ──────────────────────────────────
app.MapGet("/api/user/avatar", (HttpContext context) =>
{
    if (context.User?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var username = context.User.Identity?.Name ?? string.Empty;

    // Extract numeric personnel code: "he110749" → "110749"
    var code = System.Text.RegularExpressions.Regex.Match(username, @"\d+").Value;

    if (!string.IsNullOrEmpty(code))
    {
        var imagePath = $@"\\datap2\Crouse\Services-Support-P2\Personel\1-{code}.jpg";
        if (File.Exists(imagePath))
            return Results.File(File.ReadAllBytes(imagePath), "image/jpeg");
    }

    // Fallback: return a generic grey avatar SVG
    const string avatarSvg = """<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64' width='64' height='64'><circle cx='32' cy='32' r='32' fill='#dde1e7'/><circle cx='32' cy='24' r='12' fill='#9aa5b4'/><ellipse cx='32' cy='58' rx='20' ry='14' fill='#9aa5b4'/></svg>""";
    return Results.Content(avatarSvg, "image/svg+xml");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
