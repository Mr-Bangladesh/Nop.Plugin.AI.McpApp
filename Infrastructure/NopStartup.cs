using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Plugin.AI.McpApp.Mcp.Resources;
using Nop.Plugin.AI.McpApp.Mcp.Tools;
using Nop.Plugin.AI.McpApp.Services;
using Nop.Services.Customers;

namespace Nop.Plugin.AI.McpApp.Infrastructure;

public class NopStartup : INopStartup
{
    private static async Task ResolveCurrentCustomerAsync(ClaimsPrincipal? user, IServiceProvider? services)
    {
        if (services is null)
            return;

        var guidClaim = user?.FindFirst("customer_guid")?.Value;
        if (!Guid.TryParse(guidClaim, out var customerGuid))
            return;

        var customerService = services.GetRequiredService<ICustomerService>();
        var workContext = services.GetRequiredService<IWorkContext>();

        var customer = await customerService.GetCustomerByGuidAsync(customerGuid);
        if (customer != null)
            await workContext.SetCurrentCustomerAsync(customer);
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<NopCatalogTools>();
        services.AddScoped<CatalogUiResources>();
        services.AddScoped<CartUiResources>();
        services.AddScoped<IPersonalAccessTokenService, PersonalAccessTokenService>();

        services.AddMcpServer()
            .WithHttpTransport()
            .AddAuthorizationFilters()
            .WithRequestFilters(requestFilters =>
            {
                requestFilters.AddCallToolFilter(next => async (context, cancellationToken) =>
                {
                    await ResolveCurrentCustomerAsync(context.User, context.Services);
                    return await next(context, cancellationToken);
                });

                requestFilters.AddReadResourceFilter(next => async (context, cancellationToken) =>
                {
                    await ResolveCurrentCustomerAsync(context.User, context.Services);
                    return await next(context, cancellationToken);
                });

                requestFilters.AddGetPromptFilter(next => async (context, cancellationToken) =>
                {
                    await ResolveCurrentCustomerAsync(context.User, context.Services);
                    return await next(context, cancellationToken);
                });
            })
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders("Mcp-Session-Id");
            });
        });

        services.AddAuthentication()
            .AddScheme<PatAuthenticationOptions, PatAuthenticationHandler>(
                PatAuthenticationHandler.SCHEME_NAME, _ => { });
    }

    public void Configure(IApplicationBuilder application)
    {
        application.UseCors();
    }

    public int Order => 10;
}
