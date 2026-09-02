using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlanoraDbContext))]
[Migration("20260831150000_RequireGmailLinkForEmailNotifications")]
public partial class RequireGmailLinkForEmailNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE [Users]
            SET [EmailTaskNotificationsEnabled] = CAST(0 AS bit)
            WHERE [EmailTaskNotificationsEnabled] = CAST(1 AS bit)
              AND NOT EXISTS (
                  SELECT 1
                  FROM [UserGmailLinks] AS [gmailLink]
                  WHERE [gmailLink].[UserId] = [Users].[Id]);
            """);

        migrationBuilder.AlterColumn<bool>(
            name: "EmailTaskNotificationsEnabled",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "bit",
            oldDefaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<bool>(
            name: "EmailTaskNotificationsEnabled",
            table: "Users",
            type: "bit",
            nullable: false,
            defaultValue: true,
            oldClrType: typeof(bool),
            oldType: "bit",
            oldDefaultValue: false);
    }
}
