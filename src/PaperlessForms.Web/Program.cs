using PaperlessForms.Web.Components;
using PaperlessForms.Core.Services;
using PaperlessForms.Core.Interfaces;
using Aspose.Cells;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;

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
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
