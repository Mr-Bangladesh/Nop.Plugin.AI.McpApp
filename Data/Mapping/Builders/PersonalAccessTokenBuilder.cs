using System;
using System.Collections.Generic;
using System.Text;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.AI.McpApp.Domain;

namespace Nop.Plugin.AI.McpApp.Data.Mapping.Builders;

public class PersonalAccessTokenBuilder : NopEntityBuilder<PersonalAccessToken>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PersonalAccessToken.TokenHash)).AsString(128).NotNullable().Indexed()
            .WithColumn(nameof(PersonalAccessToken.TokenPrefix)).AsString(24).NotNullable()
            .WithColumn(nameof(PersonalAccessToken.CustomerGuid)).AsGuid().NotNullable().Indexed()
            .WithColumn(nameof(PersonalAccessToken.Scopes)).AsString(500).Nullable()
            .WithColumn(nameof(PersonalAccessToken.Name)).AsString(200).Nullable();
    }
}
