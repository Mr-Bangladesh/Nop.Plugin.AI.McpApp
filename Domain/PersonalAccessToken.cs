using Nop.Core;

namespace Nop.Plugin.AI.McpApp.Domain;

public class PersonalAccessToken : BaseEntity
{
    public string Name { get; set; }
    public string TokenHash { get; set; }
    public string TokenPrefix { get; set; }      // shown in UI, e.g. "nop_pat_7f3a9c2e"
    public Guid CustomerGuid { get; set; }
    public string Scopes { get; set; }             // comma-separated, nullable
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ExpiresOnUtc { get; set; }
    public DateTime? RevokedOnUtc { get; set; }
    public DateTime? LastUsedOnUtc { get; set; }
}
