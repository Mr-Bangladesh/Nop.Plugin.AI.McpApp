using System.ComponentModel;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nop.Core;

namespace Nop.Plugin.AI.McpApp.Mcp.Resources;

[McpServerResourceType]
public class CartUiResources
{
    private readonly IStoreContext _storeContext;

    public CartUiResources(IStoreContext storeContext)
    {
        _storeContext = storeContext;
    }

    [McpServerResource(UriTemplate = "ui://cart/show", Name = "Cart View", MimeType = "text/html;profile=mcp-app")]
    [Description("Interactive cart view rendered in the host after a show_cart tool call.")]
    public async Task<TextResourceContents> CartView()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var storeUrl = store.Url.TrimEnd("/").ToString();

        return new()
        {
            Uri = "ui://cart/show",
            MimeType = "text/html;profile=mcp-app",
            Text = CartWidgetHtml.Markup,
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
