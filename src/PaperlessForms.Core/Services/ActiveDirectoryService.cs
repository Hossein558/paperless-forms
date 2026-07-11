using System.Security.Claims;
using nsoftware.IPWorksAuth;

namespace PaperlessForms.Core.Services;

public class ActiveDirectoryService
{
    // The RuntimeLicense is read from IANJA.lic to enable the IPWorks component
    private const string RuntimeLicense = "31414E4A41424B4E4358344A345544453154414630580000000000000000000000000000000000002A00000000000000000035355732503155554E3430340000";
    private readonly string _domain = "Crouseco.com";

    public async Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal? Principal)> AuthenticateAsync(string username, string password)
    {
        return await Task.Run(() =>
        {
            using var ldap = new LDAP();
            ldap.RuntimeLicense = RuntimeLicense;
            
            try
            {
                ldap.ServerName = _domain;
                ldap.Timeout = 10;
                
                // UPN format for binding
                string upn = username.Contains("@") ? username : $"{username}@{_domain}";
                ldap.DN = upn;
                ldap.Password = password;
                
                // Bind to authenticate
                ldap.Bind();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(ClaimTypes.Name, username)
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                ldap.Unbind();

                return (true, string.Empty, principal);
            }
            catch (IPWorksAuthException ex)
            {
                Console.WriteLine($"[LDAP ERROR] IPWorksAuthException Code: {ex.Code}, Message: {ex.Message}");
                return (false, "نام کاربری یا رمز عبور اشتباه است.", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LDAP ERROR] Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[LDAP ERROR] Inner Exception: {ex.InnerException.Message}");
                }
                return (false, $"خطای اتصال به سرور: {ex.Message}", null);
            }
        });
    }
}
