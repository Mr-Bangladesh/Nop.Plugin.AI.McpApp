using Nop.Core.Domain.Customers;
using Nop.Plugin.AI.McpApp.Domain;

namespace Nop.Plugin.AI.McpApp.Services;

public interface IPersonalAccessTokenService
{
    Task<(PersonalAccessToken Token, Customer Customer)> ValidateTokenAsync(string plainTextToken);
    Task<(PersonalAccessToken Entity, string RawToken)> CreateTokenAsync(
        Guid customerGuid, string name, TimeSpan? lifetime, string[] scopes);
    Task<PersonalAccessToken> GetByHashAsync(string hash);
    Task<IList<PersonalAccessToken>> GetTokensByCustomerAsync(Guid customerGuid);
    Task RevokeAsync(int tokenId);
    Task UpdateLastUsedAsync(int tokenId);
}