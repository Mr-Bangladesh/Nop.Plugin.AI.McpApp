using System.Security.Cryptography;
using System.Text;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Data;
using Nop.Plugin.AI.McpApp.Domain;

namespace Nop.Plugin.AI.McpApp.Services;

public class PersonalAccessTokenService : IPersonalAccessTokenService
{
    private readonly IRepository<PersonalAccessToken> _tokenRepository;
    private readonly ICustomerService _customerService;
    public PersonalAccessTokenService(IRepository<PersonalAccessToken> tokenRepository,
        ICustomerService customerService)
    {
        _tokenRepository = tokenRepository;
        _customerService = customerService;
    }

    public async Task<(PersonalAccessToken Token, Customer Customer)> ValidateTokenAsync(string plainTextToken)
    {
        if (string.IsNullOrWhiteSpace(plainTextToken) || !PatGenerator.LooksLikePat(plainTextToken))
            return (null, null);

        // Generated display prefix uses PREFIX.Length + 8 characters (e.g. "nop_pat_" + 8 chars)
        var prefixLength = Math.Min(plainTextToken.Length, 16);
        var prefix = plainTextToken.Substring(0, prefixLength);

        var candidates = await _tokenRepository.Table
            .Where(t => t.TokenPrefix == prefix && t.RevokedOnUtc == null)
            .ToListAsync();

        var incomingHash = PatGenerator.ComputeHash(plainTextToken);

        var match = candidates.FirstOrDefault(t =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(t.TokenHash),
                Encoding.UTF8.GetBytes(incomingHash)));

        if (match == null)
            return (null, null);

        if (match.ExpiresOnUtc.HasValue && match.ExpiresOnUtc.Value < DateTime.UtcNow)
            return (null, null);

        var customer = await _customerService.GetCustomerByGuidAsync(match.CustomerGuid);
        if (customer == null || !customer.Active || customer.Deleted)
            return (null, null);

        // fire-and-forget last-used update, don't block the request
        _ = UpdateLastUsedAsync(match.Id);

        return (match, customer);
    }

    public async Task<(PersonalAccessToken, string)> CreateTokenAsync(
        Guid customerGuid, string name, TimeSpan? lifetime, string[] scopes)
    {
        var (raw, hash, prefix) = PatGenerator.Generate();

        var token = new PersonalAccessToken
        {
            TokenHash = hash,
            TokenPrefix = prefix,
            CustomerGuid = customerGuid,
            Name = name,
            Scopes = scopes is { Length: > 0 } ? string.Join(",", scopes) : null,
            CreatedOnUtc = DateTime.UtcNow,
            ExpiresOnUtc = lifetime.HasValue ? DateTime.UtcNow.Add(lifetime.Value) : null
        };

        await _tokenRepository.InsertAsync(token);
        return (token, raw); // raw is shown to the user exactly once
    }

    public async Task<PersonalAccessToken> GetByHashAsync(string hash)
    {
        return await _tokenRepository.Table.FirstOrDefaultAsync(t => t.TokenHash == hash);
    }

    public async Task<IList<PersonalAccessToken>> GetTokensByCustomerAsync(Guid customerGuid)
    {
        return await _tokenRepository.Table
            .Where(t => t.CustomerGuid == customerGuid)
            .OrderByDescending(t => t.CreatedOnUtc)
            .ToListAsync();
    }

    public async Task RevokeAsync(int tokenId)
    {
        var token = await _tokenRepository.GetByIdAsync(tokenId);
        if (token is null)
            return;
        token.RevokedOnUtc = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(token);
    }

    public async Task UpdateLastUsedAsync(int tokenId)
    {
        var token = await _tokenRepository.GetByIdAsync(tokenId);
        if (token is null)
            return;
        token.LastUsedOnUtc = DateTime.UtcNow;
        await _tokenRepository.UpdateAsync(token);
    }
}
