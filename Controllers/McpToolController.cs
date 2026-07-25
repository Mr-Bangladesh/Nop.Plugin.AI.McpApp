using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.AI.McpApp.Infrastructure;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.AI.McpApp.Controllers;

[ApiController]
[Route("api/mcp")]
public class McpToolController : BaseController
{
    private readonly McpToolExecutor _toolExecutor;

    public McpToolController()
    {
        _toolExecutor = new McpToolExecutor();
    }

    /// <summary>
    /// Execute a tool by name with provided parameters
    /// </summary>
    /// <param name="request">Tool execution request containing tool name and parameters</param>
    /// <returns>Tool execution result</returns>
    [HttpPost("execute")]
    public async Task<IResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.ToolName))
            return Results.BadRequest(new { success = false, error = "Tool name is required." });

        return await _toolExecutor.ExecuteAsync(request, HttpContext, cancellationToken);
    }
}
