using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "UserNotifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_DeletedAt_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "UserId", "DeletedAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_DeletedAt_CreatedAt",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserNotifications");
        }
    }
}
