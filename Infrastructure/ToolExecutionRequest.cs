using System.Text.Json;

namespace Nop.Plugin.AI.McpApp.Infrastructure;

public class ToolExecutionRequest
{
    public string? ToolName { get; set; }

    public object? RequestBodyJson { get; set; }

    public JsonElement? GetJsonElement()
    {
        if (RequestBodyJson is null)
            return null;

        if (RequestBodyJson is JsonElement jsonElement)
            return jsonElement;

        var json = JsonSerializer.Serialize(RequestBodyJson);
        return JsonDocument.Parse(json).RootElement;
    }
}
