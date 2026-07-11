using System.Security.Claims;
using nsoftware.IPWorksAuth;

namespace PaperlessForms.Core.Services;

public class ActiveDirectoryService
{
    private const string RuntimeLicense = "31414E4A41424B4E4358344A345544453154414630580000000000000000000000000000000000002A00000000000000000035355732503155554E3430340000";

    // AD domain for UPN suffix (e.g. user@Crouseco.com)
    private readonly string _domain = "Crouseco.com";

    // LDAP server hostname - confirmed resolvable via DNS (172.25.2.3 / 172.25.2.2)
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
            if (ok1) return BuildPrincipal(username, password, p1!);

            // Strategy 2: NetBIOS DOMAIN\user on ldap.crouseco.com:389
            var (ok2, err2, p2) = TryBind(_ldapServer, 389, $"{_netbiosDomain}\\{username}", password, false, "NetBIOS/389/ldap.crouseco.com");
            if (ok2) return BuildPrincipal(username, password, p2!);

            // Strategy 3: UPN on fallback DC ad.crouseco.com:389
            var (ok3, err3, p3) = TryBind(_ldapFallback, 389, $"{username}@{_domain}", password, false, "UPN/389/ad.crouseco.com");
            if (ok3) return BuildPrincipal(username, password, p3!);

            // Strategy 4: UPN on ldap.crouseco.com:636 (LDAPS)
            var (ok4, err4, p4) = TryBind(_ldapServer, 636, $"{username}@{_domain}", password, true, "UPN/636-LDAPS/ldap.crouseco.com");
            if (ok4) return BuildPrincipal(username, password, p4!);

            Console.WriteLine($"[LDAP] All strategies failed. Last error: {err4}");
            return (false, "نام کاربری یا رمز عبور اشتباه است.", (ClaimsPrincipal?)null);
        });
    }

    /// <summary>
    /// After bind succeeds, perform a second LDAP search to fetch the displayName attribute.
    /// Falls back to the raw username if displayName is not found.
    /// </summary>
    private (bool, string, ClaimsPrincipal?) BuildPrincipal(string username, string password, ClaimsPrincipal tempPrincipal)
    {
        // CRITICAL: Inspector Name must NEVER be blank. Multiple fallback layers are used.
        string displayName = username; // guaranteed fallback — always non-blank

        try
        {
            using var searchLdap = new LDAP();
            searchLdap.RuntimeLicense = RuntimeLicense;
            searchLdap.ServerName = _ldapServer;
            searchLdap.ServerPort = 389;
            searchLdap.Timeout = 15;
            searchLdap.DN = $"{username}@{_domain}";
            searchLdap.Password = password;
            searchLdap.Bind();

            // Broad subtree search from domain root
            string filter = $"(sAMAccountName={username})";

            string foundName = string.Empty;
            searchLdap.OnSearchResult += (s, e) =>
            {
                // Try displayName first, then cn, then name — in priority order
                string[] candidateAttrs = { "displayName", "cn", "name" };
                foreach (var attrName in candidateAttrs)
                {
                    try
                    {
                        string val = searchLdap.Attr(attrName);
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            foundName = val;
                            Console.WriteLine($"[LDAP] Found '{attrName}' = '{val}' for user '{username}'");
                            break; // stop at first non-empty attribute
                        }
                    }
                    catch (Exception attrEx)
                    {
                        Console.WriteLine($"[LDAP WARN] Attr('{attrName}') threw: {attrEx.Message}");
                    }
                }
            };

            // Base DN derived from domain: Crouseco.com -> DC=Crouseco,DC=com
            string baseDn = string.Join(",", _domain.Split('.').Select(p => $"DC={p}"));

            // WholeSubtree scope so nested OUs are included
            searchLdap.SearchScope = LDAPSearchScopes.ssWholeSubtree;
            
            // IPWorks LDAP uses the DN property as the SearchBase!
            searchLdap.DN = baseDn;
            searchLdap.Search(filter);

            searchLdap.Unbind();

            if (!string.IsNullOrWhiteSpace(foundName))
            {
                displayName = foundName;
                Console.WriteLine($"[LDAP] Using displayName='{displayName}' for user '{username}'");
            }
            else
            {
                Console.WriteLine($"[LDAP WARN] No name attribute returned by AD for '{username}'. Falling back to username.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LDAP WARN] displayName search failed: {ex.Message}. Falling back to username '{username}'.");
            // displayName is already set to username — guaranteed non-blank
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, displayName),        // Real full name from AD
            new Claim("preferred_username", username),      // Raw AD login kept separately
            new Claim(ClaimTypes.Email, $"{username}@{_domain}")
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        return (true, string.Empty, principal);
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

            // Return a temporary principal — will be replaced by BuildPrincipal
            string plainUser = dn.Contains("@") ? dn.Split('@')[0] :
                               dn.Contains("\\") ? dn.Split('\\')[1] : dn;
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, plainUser) };
            return (true, string.Empty, new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));
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
