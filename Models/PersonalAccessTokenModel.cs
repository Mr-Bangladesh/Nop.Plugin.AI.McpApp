namespace Nop.Plugin.AI.McpApp.Models;

public class PersonalAccessTokenModel
{
    public PersonalAccessTokenModel()
    {
        Tokens = new List<PersonalAccessTokenListItemModel>();
    }

    public bool HasToken { get; set; }
    public string Masked { get; set; } = string.Empty;
    public string TokenName { get; set; } = string.Empty;
    public IList<PersonalAccessTokenListItemModel> Tokens { get; set; }
}

public class PersonalAccessTokenListItemModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedOn { get; set; } = string.Empty;
    public bool Revoked { get; set; }
    public string Masked { get; set; } = string.Empty;
    public bool CanRevoke { get; set; }
}
