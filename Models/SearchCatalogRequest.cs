using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Nop.Plugin.AI.McpApp.Models;

public class SearchCatalogRequest
{
    [JsonPropertyName("query")]
    [Description("The search query to match against product names and descriptions.")]
    public string Query { get; set; }

    [JsonPropertyName("categories")]
    [Description("The categories to filter products by.")]
    public string[] Categories { get; set; }

    [JsonPropertyName("manufacturers")]
    [Description("The manufacturers to filter products by.")]
    public string[] Manufacturers { get; set; }

    [JsonPropertyName("vendor")]
    [Description("The vendor to filter products by.")]
    public string Vendor { get; set; }
    
    [JsonPropertyName("tags")]
    [Description("The tags to filter products by.")]
    public string[] Tags { get; set; }

    [JsonPropertyName("priceMin")]
    [Description("The minimum price to filter products by.")]
    public decimal? PriceMin { get; set; } = decimal.Zero;

    [JsonPropertyName("priceMax")]
    [Description("The maximum price to filter products by.")]
    public decimal? PriceMax { get; set; } = decimal.MaxValue;

    [JsonPropertyName("pageIndex")]
    [Description("The index of the page of results to return.")]
    public int PageIndex { get; set; } = 0;

    [JsonPropertyName("pageSize")]
    [Description("The number of results to return per page.")]
    public int PageSize { get; set; } = 15;
}
