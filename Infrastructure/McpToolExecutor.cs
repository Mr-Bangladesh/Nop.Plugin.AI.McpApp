using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Nop.Plugin.AI.McpApp.Mcp.Tools;

namespace Nop.Plugin.AI.McpApp.Infrastructure;

public class McpToolExecutor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyList<MethodInfo> ToolMethods = typeof(NopCatalogTools).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && Attribute.IsDefined(type, typeof(McpServerToolTypeAttribute)))
        .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => Attribute.IsDefined(method, typeof(McpServerToolAttribute))))
        .ToList();

    public async Task<IResult> ExecuteAsync(ToolExecutionRequest request, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ToolName))
            return Results.BadRequest(new { success = false, error = "Tool name is required." });

        var toolMethod = ToolMethods.FirstOrDefault(method =>
            string.Equals(method.Name, request.ToolName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(method.GetCustomAttribute<McpServerToolAttribute>()?.Name, request.ToolName, StringComparison.OrdinalIgnoreCase));

        if (toolMethod is null)
            return Results.NotFound(new { success = false, error = $"Tool '{request.ToolName}' was not found." });

        try
        {
            var toolInstance = ResolveToolInstance(toolMethod.DeclaringType, httpContext.RequestServices);
            var arguments = BuildArguments(toolMethod, request.GetJsonElement());
            var result = toolMethod.Invoke(toolInstance, arguments);

            if (result is Task task)
            {
                await task.WaitAsync(cancellationToken);
                result = task.GetType().GetProperty("Result")?.GetValue(task);
            }

            return Results.Ok(new { success = true, toolName = request.ToolName, result });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return Results.BadRequest(new { success = false, error = exception.InnerException.Message });
        }
        catch (Exception exception)
        {
            return Results.BadRequest(new { success = false, error = exception.Message });
        }
    }

    private static object ResolveToolInstance(Type? toolType, IServiceProvider serviceProvider)
    {
        if (toolType is null)
            throw new InvalidOperationException("The requested tool does not expose a declaring type.");

        return serviceProvider.GetService(toolType) ?? ActivatorUtilities.CreateInstance(serviceProvider, toolType);
    }

    private static object?[] BuildArguments(MethodInfo method, JsonElement? requestBodyJson)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return Array.Empty<object?>();

        if (parameters.Length == 1)
        {
            var parameter = parameters[0];
            if (requestBodyJson is not null && requestBodyJson.Value.ValueKind != JsonValueKind.Null)
            {
                if (TryExtractSingleArgument(requestBodyJson.Value, parameter, out var value))
                    return new object?[] { value };
            }

            return new object?[] { GetDefaultValue(parameter) };
        }

        if (requestBodyJson is null || requestBodyJson.Value.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Tools with multiple parameters require a JSON object payload.");

        var values = new object?[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (TryGetPropertyValue(requestBodyJson.Value, parameter.Name, parameter.ParameterType, out var value))
            {
                values[index] = value;
                continue;
            }

            values[index] = parameter.HasDefaultValue ? parameter.DefaultValue : GetDefaultValue(parameter);
        }

        return values;
    }

    private static bool TryGetPropertyValue(JsonElement payload, string? parameterName, Type targetType, out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in payload.EnumerateObject())
        {
            if (string.Equals(property.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                value = ConvertJsonValue(property.Value, targetType);
                return true;
            }
        }

        return false;
    }

    private static bool TryExtractSingleArgument(JsonElement requestBodyJson, ParameterInfo parameter, out object? value)
    {
        value = null;

        if (requestBodyJson.ValueKind == JsonValueKind.Object)
        {
            if (parameter.Name is not null && requestBodyJson.TryGetProperty(parameter.Name, out var parameterValue))
            {
                value = ConvertJsonValue(parameterValue, parameter.ParameterType);
                return true;
            }

            if (requestBodyJson.EnumerateObject().Count() == 1)
            {
                var singleProperty = requestBodyJson.EnumerateObject().First();
                value = ConvertJsonValue(singleProperty.Value, parameter.ParameterType);
                return true;
            }

            if (parameter.ParameterType != typeof(string) && parameter.ParameterType != typeof(object))
            {
                value = ConvertJsonValue(requestBodyJson, parameter.ParameterType);
                return true;
            }
        }

        value = ConvertJsonValue(requestBodyJson, parameter.ParameterType);
        return true;
    }

    private static object? ConvertJsonValue(JsonElement jsonValue, Type targetType)
    {
        if (jsonValue.ValueKind == JsonValueKind.Null)
            return null;

        if (targetType == typeof(JsonElement))
            return jsonValue;

        if (targetType == typeof(object))
            return jsonValue;

        if (targetType == typeof(string))
            return jsonValue.ValueKind == JsonValueKind.String ? jsonValue.GetString() : jsonValue.GetRawText();

        if (targetType == typeof(Guid))
            return Guid.Parse(jsonValue.GetString() ?? throw new InvalidOperationException("String value could not be converted to Guid."));

        if (targetType == typeof(DateTime))
            return DateTime.Parse(jsonValue.GetString() ?? jsonValue.GetRawText(), CultureInfo.InvariantCulture);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, jsonValue.GetString() ?? jsonValue.GetRawText(), true);

        if (targetType == typeof(bool))
            return jsonValue.GetBoolean();

        if (targetType == typeof(int))
            return jsonValue.GetInt32();

        if (targetType == typeof(long))
            return jsonValue.GetInt64();

        if (targetType == typeof(short))
            return (short)jsonValue.GetInt32();

        if (targetType == typeof(byte))
            return (byte)jsonValue.GetByte();

        if (targetType == typeof(double))
            return jsonValue.GetDouble();

        if (targetType == typeof(float))
            return jsonValue.GetSingle();

        if (targetType == typeof(decimal))
            return jsonValue.GetDecimal();

        return JsonSerializer.Deserialize(jsonValue.GetRawText(), targetType, SerializerOptions);
    }

    private static object? GetDefaultValue(ParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(string))
            return string.Empty;

        return parameter.HasDefaultValue ? parameter.DefaultValue : (parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null);
    }
}
