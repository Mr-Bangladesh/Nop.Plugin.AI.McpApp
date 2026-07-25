using System.Text.Json.Serialization;

namespace Nop.Plugin.AI.McpApp.Models;

public class AddToCartRequest
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }
}
