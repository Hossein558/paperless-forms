using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.MvcCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace PaperlessForms.Web.Controllers;

[AllowAnonymous]
[Route("Auth")]
public class AuthController : Controller
{
    private readonly Saml2Configuration _config;

    public AuthController(IOptions<Saml2Configuration> configAccessor)
    {
        _config = configAccessor.Value;
    }

    [Route("Login")]
    public IActionResult Login(string returnUrl = null)
    {
        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string> { { "ReturnUrl", returnUrl ?? Url.Content("~/") } });
        return binding.Bind(new Saml2AuthnRequest(_config)).ToActionResult();
    }

    [Route("AssertionConsumerService")]
    public async Task<IActionResult> AssertionConsumerService()
    {
        var binding = new Saml2PostBinding();
        var saml2AuthnResponse = new Saml2AuthnResponse(_config);

        binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);
        if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
        {
            throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
        }
        
        binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);
        await saml2AuthnResponse.CreateSession(HttpContext, claimsTransform: (claimsPrincipal) => claimsPrincipal);

        var relayStateQuery = binding.GetRelayStateQuery();
        var returnUrl = relayStateQuery.ContainsKey("ReturnUrl") ? relayStateQuery["ReturnUrl"] : Url.Content("~/");
        return Redirect(returnUrl);
    }
}
