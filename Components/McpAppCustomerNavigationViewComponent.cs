using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Customer;

namespace Nop.Plugin.AI.McpApp.Components;

public class McpAppCustomerNavigationViewComponent : NopViewComponent
{
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public McpAppCustomerNavigationViewComponent(ICustomerService customerService, IWorkContext workContext)
    {
        _customerService = customerService;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (additionalData is not CustomerNavigationModel model)
            return Content(string.Empty);

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (await _customerService.IsGuestAsync(customer))
            return Content(string.Empty);

        return await ViewAsync("~/Plugins/AI.McpApp/Views/Components/CustomerMcpAppMenu.cshtml", model);
    }
}
