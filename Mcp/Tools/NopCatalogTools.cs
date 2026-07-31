using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nop.Plugin.AI.McpApp.Models;
using Nop.Plugin.AI.McpApp.Services;
using Nop.Web.Factories;

namespace Nop.Plugin.AI.McpApp.Mcp.Tools;

[McpServerToolType]
public class NopCatalogTools
{
    private readonly ISimplifiedProductService _simplifiedProductService;
    private readonly IProductModelFactory _productModelFactory;

    public NopCatalogTools(ISimplifiedProductService simplifiedProductService,
        IProductModelFactory productModelFactory)
    {
        _simplifiedProductService = simplifiedProductService;
        _productModelFactory = productModelFactory;
    }

    [McpServerTool(Name = "show_catalog")]
    [Description("Searches the catalog for products matching the provided search query and displays them as an interactive grid.")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://catalog/search-results"}""")]
    public async Task<CallToolResult> ShowCatalogAsync(SearchCatalogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = "No search query provided." }]
            };
        }

        // query, categoryids, manufacturerids, vendorid, pricemin, pricemax, orderby, pageindex, pagesize

        var products = await _simplifiedProductService.SearchProductsAsync(request);

        var overviewModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);

        var summary = $"Found {overviewModels.Count()} product(s) matching \"{request.Query}\".";

        var structured = new
        {
            query = request.Query,
            products = overviewModels.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.ProductPrice?.Price ?? string.Empty,
                imageUrl = p.PictureModels.FirstOrDefault()?.ImageUrl ?? string.Empty,
                sku = p.Sku
            })
        };

        return new CallToolResult
        {
            Content = new List<ContentBlock> { new TextContentBlock { Text = summary } },
            StructuredContent = JsonSerializer.SerializeToElement(structured)
        };
    }
}