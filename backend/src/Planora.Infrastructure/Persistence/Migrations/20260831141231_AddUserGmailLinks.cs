using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGmailLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserGmailLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GmailAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    RefreshTokenCipher = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RefreshTokenNonce = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastSendFailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSendFailureReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGmailLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGmailLinks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGmailLinks_UserId",
                table: "UserGmailLinks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGmailLinks");
        }
    }
}
