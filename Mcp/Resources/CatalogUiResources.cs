using System.ComponentModel;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nop.Core;

namespace Nop.Plugin.AI.McpApp.Mcp.Resources;

[McpServerResourceType]
public class CatalogUiResources
{
    private readonly IStoreContext _storeContext;

    public CatalogUiResources(IStoreContext storeContext)
    {
        _storeContext = storeContext;
    }

    [McpServerResource(UriTemplate = "ui://catalog/search-results", Name = "Catalog Search Results", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive product grid rendered in the host after a catalog search.")]
    public async Task<TextResourceContents> CatalogSearchUi()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var storeUrl = store.Url.TrimEnd("/").ToString();
        return new()
        {
            Uri = "ui://catalog/search-results",
            MimeType = "text/html;profile=mcp-app",
            Text = CatalogWidgetHtml.Markup,
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject
                {
                    ["csp"] = new JsonObject
                    {
                        ["connectDomains"] = new JsonArray { "https://esm.sh", storeUrl },
                        ["resourceDomains"] = new JsonArray { "https://esm.sh", storeUrl }
                    }
                }
            }
        };
    }
}
