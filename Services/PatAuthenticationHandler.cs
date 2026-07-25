using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nop.Core;
using Nop.Services.Customers;

namespace Nop.Plugin.AI.McpApp.Services;

public class PatAuthenticationOptions : AuthenticationSchemeOptions { }

public class PatAuthenticationHandler : AuthenticationHandler<PatAuthenticationOptions>
{
    public const string SCHEME_NAME = "PersonalAccessToken";
    private const string HEADER_NAME = "MCP-AUTH-TOKEN";

    private readonly IPersonalAccessTokenService _tokenService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public PatAuthenticationHandler(
        IOptionsMonitor<PatAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IPersonalAccessTokenService tokenService,
        ICustomerService customerService,
        IWorkContext workContext)
        : base(options, logger, encoder)
    {
        _tokenService = tokenService;
        _customerService = customerService;
        _workContext = workContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HEADER_NAME, out var header))
            return AuthenticateResult.NoResult();

        var rawToken = header.ToString().Trim();
        if (!PatGenerator.LooksLikePat(rawToken))
            return AuthenticateResult.NoResult();

        var (token, customer) = await _tokenService.ValidateTokenAsync(rawToken);
        if (token is null || customer is null)
            return AuthenticateResult.Fail("Invalid token");

        await _workContext.SetCurrentCustomerAsync(customer);

        _ = _tokenService.UpdateLastUsedAsync(token.Id); // don't block auth on this

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new("customer_guid", customer.CustomerGuid.ToString()),
            new("token_id", token.Id.ToString())
        };
        if (!string.IsNullOrEmpty(token.Scopes))
            claims.Add(new Claim("scope", token.Scopes));

        var identity = new ClaimsIdentity(claims, SCHEME_NAME);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SCHEME_NAME);

        return AuthenticateResult.Success(ticket);
    }
}