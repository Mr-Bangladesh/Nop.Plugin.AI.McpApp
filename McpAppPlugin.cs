using Nop.Plugin.AI.McpApp.Components;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.AI.McpApp;

public class McpAppPlugin : BasePlugin, IWidgetPlugin
{
    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone == PublicWidgetZones.AccountNavigationAfter)
            return typeof(McpAppCustomerNavigationViewComponent);

        return null;
    }

    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.AccountNavigationAfter });
    }

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => false;

    #endregion
}
