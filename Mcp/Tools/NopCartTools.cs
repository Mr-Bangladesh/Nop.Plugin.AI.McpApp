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
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.AI.McpApp.Mcp.Tools;

[McpServerToolType]
public class NopCartTools
{
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;

    public NopCartTools(IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        IShoppingCartModelFactory shoppingCartModelFactory,
        IStoreContext storeContext,
        IProductService productService)
    {
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _productService = productService;
        _storeContext = storeContext;
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
}
