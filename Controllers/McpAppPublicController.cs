using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.AI.McpApp.Models;
using Nop.Plugin.AI.McpApp.Services;
using Nop.Services.Customers;
using Nop.Web.Controllers;

namespace Nop.Plugin.AI.McpApp.Controllers;

[AutoValidateAntiforgeryToken]
public class McpAppPublicController : BasePublicController
{
    private readonly IPersonalAccessTokenService _patService;
    private readonly ICustomerService _customerService;
    private readonly IWorkContext _workContext;

    public McpAppPublicController(IPersonalAccessTokenService patService,
        ICustomerService customerService,
        IWorkContext workContext)
    {
        _patService = patService;
        _customerService = customerService;
        _workContext = workContext;
    }

    [HttpGet]
    public async Task<IActionResult> CustomerTokens()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || await _customerService.IsGuestAsync(customer))
            return RedirectToRoute("CustomerLogin");

        var model = new PersonalAccessTokenModel();
        var tokens = await _patService.GetTokensByCustomerAsync(customer.CustomerGuid);
        var orderedTokens = tokens
            .OrderByDescending(t => t.CreatedOnUtc)
            .ThenByDescending(t => t.Id)
            .ToList();

        foreach (var token in orderedTokens)
        {
            var visibleHash = token.TokenHash != null && token.TokenHash.Length >= 6
                ? token.TokenHash.Substring(0, 6)
                : token.TokenHash ?? string.Empty;

            model.Tokens.Add(new PersonalAccessTokenListItemModel
            {
                Id = token.Id,
                Name = token.Name ?? string.Empty,
                CreatedOn = token.CreatedOnUtc.ToString("g"),
                Revoked = token.RevokedOnUtc != null,
                Masked = $"{token.TokenPrefix}...{visibleHash}",
                CanRevoke = token.RevokedOnUtc == null && token.Id == orderedTokens.FirstOrDefault(t => t.RevokedOnUtc == null)?.Id
            });
        }

        if (model.Tokens.Any())
        {
            model.HasToken = true;
            model.Masked = model.Tokens.First().Masked;
        }

        return View("~/Plugins/AI.McpApp/Views/CustomerTokens.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateToken(string name)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null)
            return Json(new { success = false, message = "Customer not found" });

        var tokenName = string.IsNullOrWhiteSpace(name) ? "Web UI" : name.Trim();
        var (tokenEntity, raw) = await _patService.CreateTokenAsync(customer.CustomerGuid, tokenName, null, null);
        if (tokenEntity == null)
            return Json(new { success = false, message = "Failed to create token" });

        var visibleHash = tokenEntity.TokenHash != null && tokenEntity.TokenHash.Length >= 6
            ? tokenEntity.TokenHash.Substring(0, 6)
            : tokenEntity.TokenHash ?? string.Empty;

        var masked = $"{tokenEntity.TokenPrefix}...{visibleHash}";

        return Json(new { success = true, token = raw, masked, tokenName = tokenEntity.Name });
    }

    [HttpPost]
    public async Task<IActionResult> RevokeToken(int tokenId)
    {
        if (tokenId <= 0)
            return Json(new { success = false, message = "Invalid token" });

        await _patService.RevokeAsync(tokenId);
        return Json(new { success = true });
    }
}
