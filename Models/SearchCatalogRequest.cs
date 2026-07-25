using System.Text.Json.Serialization;

namespace Nop.Plugin.AI.McpApp.Models;

public class SearchCatalogRequest
{
    [JsonPropertyName("searchQuery")]
    public string SearchQuery { get; set; }
}
