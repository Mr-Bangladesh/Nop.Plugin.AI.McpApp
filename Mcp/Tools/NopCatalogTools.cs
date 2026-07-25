using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.AI.McpApp.Models;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.AI.McpApp.Mcp.Tools;

[McpServerToolType]
public class NopCatalogTools
{
    private readonly IProductService _productService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    private readonly IStoreContext _storeContext;
    private readonly ICustomerService _customerService;

    public NopCatalogTools(IProductService productService,
        IProductModelFactory productModelFactory,
        IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        IShoppingCartModelFactory shoppingCartModelFactory,
        IStoreContext storeContext,
        ICustomerService customerService)
    {
        _productService = productService;
        _productModelFactory = productModelFactory;
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _storeContext = storeContext;
        _customerService = customerService;
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

    [McpServerTool(Name = "add_to_cart")]
    [Description("Adds products to the shopping cart.")]
    public async Task<CallToolResult> AddToCartAsync(AddToCartRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var product = await _productService.GetProductByIdAsync(request.ProductId);
        if (product == null)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = $"Product with ID {request.ProductId} not found." } }
            };
        }

        var warnings = await _shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.ShoppingCart, store.Id);

        return new CallToolResult
        {
            Content = new List<ContentBlock> { new TextContentBlock { Text = warnings.FirstOrDefault() ?? "Product added to cart successfully." } }
        };
    }

    [McpServerTool(Name = "show_cart")]
    [Description("Shows the current customer's shopping cart.")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://cart/show"}""")]
    public async Task<CallToolResult> ShowCartAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart);
        var model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(new ShoppingCartModel(), cart);
        var cartTotalModel = await _shoppingCartModelFactory.PrepareOrderTotalsModelAsync(cart, false);

        var summary = $"Found {model.Items.Count} item(s) matching in cart.";

        var structured = new
        {
            total = cartTotalModel.SubTotal,
            products = model.Items.Select(item => new
            {
                productId = item.ProductId,
                name = item.ProductName,
                subTotal = item.SubTotal,
                imageUrl = item.Picture.ImageUrl,
                sku = item.Sku,
                unitPrice = item.UnitPrice,
                quantity = item.Quantity
            })
        };

        return new CallToolResult
        {
            Content = new List<ContentBlock> { new TextContentBlock { Text = summary } },
            StructuredContent = JsonSerializer.SerializeToElement(structured)
        };
    }
}