using PaperlessForms.Web.Components;
using PaperlessForms.Core.Services;
using PaperlessForms.Core.Interfaces;
using Aspose.Cells;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

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

// ─── Crowd REST Auth ────────────────────────────────────
builder.Services.AddHttpClient("crowd", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<CrowdAuthService>();

builder.Services.AddControllers();

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

var keysFolder = builder.Configuration["DataProtection:KeysFolder"] ?? "/app/Keys";
Directory.CreateDirectory(keysFolder); // Ensure the path exists inside the container
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("PaperlessFormsApp");

var app = builder.Build();

app.UsePathBase("/paperless");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", async (HttpContext context, [FromForm] string Username, [FromForm] string Password, [FromForm] string? ReturnUrl, CrowdAuthService crowdService, ActiveDirectoryService adService) =>
{
    // Strategy 1: Crowd REST API (primary)
    var result = await crowdService.AuthenticateAsync(Username, Password);

    // Strategy 2: LDAP / Active Directory fallback
    if (!result.IsSuccess)
    {
        Console.WriteLine($"[Auth] Crowd failed for '{Username}', trying LDAP fallback.");
        result = await adService.AuthenticateAsync(Username, Password);
    }

    if (result.IsSuccess)
    {
        await context.SignInAsync("Cookies", result.Principal!);
        // Ignore relative ReturnUrls, force redirect to dashboard
        return Results.Redirect("/paperless/");
    }
    return Results.Redirect($"/paperless/login?ErrorMessage={Uri.EscapeDataString(result.ErrorMessage)}");
});

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("Cookies");
    return Results.Redirect("/paperless/login");
});

// ─── Profile Avatar API ──────────────────────────────────
var avatarHandler = (HttpContext context, IConfiguration config) => 
{
    Console.WriteLine("Avatar API hit. Checking user authentication...");
    var user = context.User;
    if (user?.Identity == null || !user.Identity.IsAuthenticated) 
    {
        Console.WriteLine("User is not authenticated. Returning NotFound.");
        return Results.NotFound();
    }

    var usernameClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                        ?? user.Identity.Name ?? string.Empty;
    Console.WriteLine($"Raw username claim extracted: '{usernameClaim}'");

    if (usernameClaim.Contains("\\")) usernameClaim = usernameClaim.Split('\\').Last();
    if (usernameClaim.Contains("@")) usernameClaim = usernameClaim.Split('@').First();
    
    Console.WriteLine($"Sanitized username: '{usernameClaim}'");

    string personnelCodeStr = usernameClaim.StartsWith("he", StringComparison.OrdinalIgnoreCase) && usernameClaim.Length > 2
        ? usernameClaim.Substring(2) 
        : usernameClaim;
    
    string personnelCodeIntStr = int.TryParse(personnelCodeStr, out int parsed) ? parsed.ToString() : personnelCodeStr;
    Console.WriteLine($"Calculated Personnel Codes -> Str: '{personnelCodeStr}', Int: '{personnelCodeIntStr}'");

    var avatarBaseFolder = config["Avatar:FolderPath"] ?? "/app/Avatars";
    string[] possibleNames = { $"1-{personnelCodeStr}", $"1-{personnelCodeIntStr}" };
    string[] possibleExtensions = { ".jpg", ".JPG", ".png", ".PNG", ".jpeg", ".JPEG" };

    string actualFilePath = null;
    foreach (var name in possibleNames)
    {
        foreach (var ext in possibleExtensions)
        {
            string testPath = Path.Combine(avatarBaseFolder, name + ext);
            Console.WriteLine($"Checking physical existence of: '{testPath}'");
            
            if (File.Exists(testPath))
            {
                Console.WriteLine($"SUCCESS: File found at '{testPath}'!");
                actualFilePath = testPath;
                break;
            }
        }
        if (actualFilePath != null) break;
    }

    if (actualFilePath != null)
    {
        string contentType = actualFilePath.EndsWith("png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        Console.WriteLine($"Returning file with Content-Type: {contentType}");
        return Results.File(actualFilePath, contentType);
    }
    
    Console.WriteLine("All file checks failed. Returning fallback SVG/NotFound.");
    
    // Fallback: return a generic grey avatar SVG
    const string avatarSvg = """<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64' width='64' height='64'><circle cx='32' cy='32' r='32' fill='#dde1e7'/><circle cx='32' cy='24' r='12' fill='#9aa5b4'/><ellipse cx='32' cy='58' rx='20' ry='14' fill='#9aa5b4'/></svg>""";
    return Results.Content(avatarSvg, "image/svg+xml");
};

app.MapGet("/api/avatar", avatarHandler).RequireAuthorization();
app.MapGet("/api/user/avatar", avatarHandler).RequireAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
