using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCheckoutAndAutomaticReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Provider_ProviderTransactionId",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentTransactionId",
                table: "UserSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                table: "PaymentTransactions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderOrderId",
                table: "PaymentTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentTransactionId",
                table: "UserSubscriptions",
                column: "PaymentTransactionId",
                unique: true,
                filter: "[PaymentTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_ProviderOrderId",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "ProviderOrderId" },
                unique: true,
                filter: "[ProviderOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_ProviderTransactionId",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "ProviderTransactionId" },
                unique: true,
                filter: "[ProviderTransactionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_PaymentTransactionId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Provider_ProviderOrderId",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Provider_ProviderTransactionId",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "ProviderOrderId",
                table: "PaymentTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_ProviderTransactionId",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "ProviderTransactionId" });
        }
    }
}
