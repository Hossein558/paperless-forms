using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PaperlessForms.Core.Services;

/// <summary>
/// Authenticates users against Atlassian Crowd REST API.
/// Falls back gracefully so the caller can try LDAP next.
/// </summary>
public class CrowdAuthService
{
    private readonly HttpClient _http;
    private readonly string _crowdBaseUrl;
    private readonly string _appName;
    private readonly string _appPassword;

    public CrowdAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _crowdBaseUrl = configuration["Crowd:BaseUrl"]!.TrimEnd('/');
        _appName      = configuration["Crowd:AppName"]!;
        _appPassword  = configuration["Crowd:AppPassword"]!;

        _http = httpClientFactory.CreateClient("crowd");
        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_appName}:{_appPassword}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Authenticates the user against Crowd.
    /// Returns (true, principal) on success or (false, null) on failure.
    /// </summary>
    public async Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal? Principal)> AuthenticateAsync(
        string username, string password)
    {
        try
        {
            // Step 1: Validate credentials via Crowd authentication endpoint
            var authBody = JsonSerializer.Serialize(new { value = password });
            var authContent = new StringContent(authBody, Encoding.UTF8, "application/json");
            var authResponse = await _http.PostAsync(
                $"{_crowdBaseUrl}/rest/usermanagement/1/authentication?username={Uri.EscapeDataString(username)}",
                authContent);

            if (!authResponse.IsSuccessStatusCode)
            {
                var errBody = await authResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[Crowd] Auth failed for '{username}': HTTP {(int)authResponse.StatusCode} - {errBody}");
                return (false, "نام کاربری یا رمز عبور اشتباه است.", null);
            }

            // Step 2: Fetch user details to get displayName
            var userResponse = await _http.GetAsync(
                $"{_crowdBaseUrl}/rest/usermanagement/1/user?username={Uri.EscapeDataString(username)}");

            string displayName = username; // safe fallback
            string email = string.Empty;

            if (userResponse.IsSuccessStatusCode)
            {
                var userJson = await userResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(userJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("display-name", out var dn) && !string.IsNullOrWhiteSpace(dn.GetString()))
                    displayName = dn.GetString()!;

                if (root.TryGetProperty("email", out var em))
                    email = em.GetString() ?? string.Empty;
            }

            Console.WriteLine($"[Crowd] Auth SUCCESS for '{username}', displayName='{displayName}'");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, username.ToLowerInvariant()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim("preferred_username", username.ToLowerInvariant()),
                new Claim(ClaimTypes.Email, email),
                new Claim("auth_source", "crowd")
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            return (true, string.Empty, new ClaimsPrincipal(identity));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Crowd] Exception during auth for '{username}': {ex.Message}");
            return (false, "خطای اتصال به سرویس احراز هویت.", null);
        }
    }
}
