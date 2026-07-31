using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.AI.McpApp.Models;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Web.Factories;

namespace Nop.Plugin.AI.McpApp.Mcp.Tools;

[McpServerToolType]
public class NopCatalogTools
{
    private readonly IProductService _productService;
    private readonly IProductModelFactory _productModelFactory;

    public NopCatalogTools(IProductService productService,
        IProductModelFactory productModelFactory)
    {
        _productService = productService;
        _productModelFactory = productModelFactory;
    }

    [McpServerTool(Name = "show_catalog")]
    [Description("Searches the catalog for products matching the provided search query and displays them as an interactive grid.")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://catalog/search-results"}""")]
    public async Task<CallToolResult> ShowCatalogAsync(SearchCatalogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.SearchQuery))
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = "No search query provided." }]
            };
        }

        var products = await _productService.SearchProductsAsync(
            keywords: request.SearchQuery,
            pageSize: 20,
            visibleIndividuallyOnly: true,
            showHidden: false,
            overridePublished: true);

        var overviewModels = await _productModelFactory.PrepareProductOverviewModelsAsync(products);

        var summary = $"Found {overviewModels.Count()} product(s) matching \"{request.SearchQuery}\".";

        var structured = new
        {
            query = request.SearchQuery,
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