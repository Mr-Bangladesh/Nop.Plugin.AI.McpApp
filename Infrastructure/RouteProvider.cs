using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Plugin.AI.McpApp;
using Nop.Plugin.AI.McpApp.Services;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Infrastructure;

namespace Nop.Plugin.AI.McpApp.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : BaseRouteProvider, IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapMcp("/mcp")
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(PatAuthenticationHandler.SCHEME_NAME)
                .RequireAuthenticatedUser());

        var lang = GetLanguageRoutePattern();

        endpointRouteBuilder.MapControllerRoute(name: McpAppDefaults.CustomerTokensRouteName,
            pattern: $"{lang}/customer/mcp-app",
            defaults: new { controller = "McpAppPublic", action = "CustomerTokens" });

        endpointRouteBuilder.MapControllerRoute(name: McpAppDefaults.GenerateTokenRouteName,
            pattern: $"{lang}/mcpapp/generate-token",
            defaults: new { controller = "McpAppPublic", action = "GenerateToken" });

        endpointRouteBuilder.MapControllerRoute(name: McpAppDefaults.RevokeTokenRouteName,
            pattern: $"{lang}/mcpapp/revoke-token",
            defaults: new { controller = "McpAppPublic", action = "RevokeToken" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 50;
}
