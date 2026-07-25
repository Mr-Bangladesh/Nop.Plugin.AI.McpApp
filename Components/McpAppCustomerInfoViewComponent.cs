using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Plugin.AI.McpApp.Services;
using Nop.Plugin.AI.McpApp.Models;
using Nop.Core;

namespace Nop.Plugin.AI.McpApp.Components;

public class McpAppCustomerInfoViewComponent : NopViewComponent
{
    private readonly IPersonalAccessTokenService _patService;
    private readonly IWorkContext _workContext;

    public McpAppCustomerInfoViewComponent(IPersonalAccessTokenService patService, IWorkContext workContext)
    {
        _patService = patService;
        _workContext = workContext;
    }

    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var model = new PersonalAccessTokenModel();
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer != null)
        {
            var tokens = await _patService.GetTokensByCustomerAsync(customer.CustomerGuid);
            var active = tokens?.FirstOrDefault(t => t.RevokedOnUtc == null);
            if (active != null)
            {
                var visibleHash = active.TokenHash != null && active.TokenHash.Length >= 6
                    ? active.TokenHash.Substring(0, 6)
                    : active.TokenHash ?? string.Empty;
                model.HasToken = true;
                model.Masked = $"{active.TokenPrefix}...{visibleHash}";
            }
        }

        return View("~/Plugins/AI.McpApp/Views/McpAppCustomerInfo.cshtml", model);
    }
}
