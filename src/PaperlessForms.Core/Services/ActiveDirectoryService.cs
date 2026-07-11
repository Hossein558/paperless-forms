using System.Security.Claims;
using nsoftware.IPWorksAuth;

namespace PaperlessForms.Core.Services;

public class ActiveDirectoryService
{
    private const string RuntimeLicense = "31414E4A41424B4E4358344A345544453154414630580000000000000000000000000000000000002A00000000000000000035355732503155554E3430340000";

    // AD domain for UPN suffix (e.g. user@Crouseco.com)
    private readonly string _domain = "Crouseco.com";

    // LDAP server hostname - confirmed resolvable via DNS (172.25.2.3 / 172.25.2.2)
    // NOTE: "Crouseco.com" is NOT directly resolvable; "ldap.crouseco.com" is the correct LDAP endpoint.
    private readonly string _ldapServer = "ldap.crouseco.com";

    // Fallback DC confirmed resolvable at 172.25.96.234
    private readonly string _ldapFallback = "ad.crouseco.com";

    private readonly string _netbiosDomain = "CROUSECO";

    public async Task<(bool IsSuccess, string ErrorMessage, ClaimsPrincipal? Principal)> AuthenticateAsync(string username, string password)
    {
        return await Task.Run(() =>
        {
            Console.WriteLine($"[LDAP] Starting authentication for user: {username}");

            // Strategy 1 (PRIMARY): UPN format on ldap.crouseco.com:389
            var (ok1, err1, p1) = TryBind(_ldapServer, 389, $"{username}@{_domain}", password, false, "UPN/389/ldap.crouseco.com");
            if (ok1) return (true, string.Empty, p1);

            // Strategy 2: NetBIOS DOMAIN\user on ldap.crouseco.com:389
            var (ok2, err2, p2) = TryBind(_ldapServer, 389, $"{_netbiosDomain}\\{username}", password, false, "NetBIOS/389/ldap.crouseco.com");
            if (ok2) return (true, string.Empty, p2);

            // Strategy 3: UPN on fallback DC ad.crouseco.com:389
            var (ok3, err3, p3) = TryBind(_ldapFallback, 389, $"{username}@{_domain}", password, false, "UPN/389/ad.crouseco.com");
            if (ok3) return (true, string.Empty, p3);

            // Strategy 4: UPN on ldap.crouseco.com:636 (LDAPS)
            var (ok4, err4, p4) = TryBind(_ldapServer, 636, $"{username}@{_domain}", password, true, "UPN/636-LDAPS/ldap.crouseco.com");
            if (ok4) return (true, string.Empty, p4);

            Console.WriteLine($"[LDAP] All strategies failed. Last error: {err4}");
            return (false, "نام کاربری یا رمز عبور اشتباه است.", null);
        });
    }

    private (bool, string, ClaimsPrincipal?) TryBind(string server, int port, string dn, string password, bool useSSL, string label)
    {
        Console.WriteLine($"[LDAP] Trying [{label}] => {server}:{port}, DN: {dn}, SSL: {useSSL}");
        var ldap = new LDAP();
        try
        {
            ldap.RuntimeLicense = RuntimeLicense;
            ldap.ServerName = server;
            ldap.ServerPort = port;
            ldap.Timeout = 10;
            ldap.DN = dn;
            ldap.Password = password;

            if (useSSL)
            {
                ldap.SSLStartMode = LDAPSSLStartModes.sslImplicit;
                ldap.SSLProvider = LDAPSSLProviders.sslpInternal;
            }

            ldap.Bind();
            Console.WriteLine($"[LDAP] SUCCESS with strategy [{label}]");

            // Extract the plain username from DN
            string plainUser = dn.Contains("@") ? dn.Split('@')[0] :
                               dn.Contains("\\") ? dn.Split('\\')[1] : dn;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, plainUser),
                new Claim(ClaimTypes.Name, plainUser),
                new Claim(ClaimTypes.Email, $"{plainUser}@{_domain}")
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            ldap.Unbind();
            return (true, string.Empty, principal);
        }
        catch (IPWorksAuthException ex)
        {
            Console.WriteLine($"[LDAP ERROR][{label}] Code: {ex.Code}, Message: {ex.Message}");
            return (false, ex.Message, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LDAP ERROR][{label}] {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[LDAP ERROR][{label}] Inner: {ex.InnerException.Message}");
            return (false, ex.Message, null);
        }
        finally
        {
            ldap.Dispose();
        }
    }
}
