using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.AI.McpApp.Domain;

namespace Nop.Plugin.AI.McpApp.Data.Migrations;

[NopMigration("2026-07-20 00:00:00", "AI.McpApp schema", MigrationProcessType.Installation)]
public class SchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        this.CreateTableIfNotExists<PersonalAccessToken>();
    }
}
