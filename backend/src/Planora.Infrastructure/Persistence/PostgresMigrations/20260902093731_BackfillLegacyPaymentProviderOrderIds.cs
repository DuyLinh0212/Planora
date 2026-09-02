using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Planora.Infrastructure.Persistence.PostgresMigrations
{
    /// <inheritdoc />
    public partial class BackfillLegacyPaymentProviderOrderIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "PaymentTransactions"
                SET "ProviderOrderId" = CASE
                    WHEN "Provider" = 'Momo' THEN 'PLN-' || replace("Id"::text, '-', '')
                    WHEN "Provider" = 'BankTransfer' THEN 'PLN' || replace("Id"::text, '-', '')
                    ELSE "ProviderOrderId"
                END
                WHERE "ProviderOrderId" IS NULL
                  AND "Status" = 'Pending'
                  AND "Provider" IN ('Momo', 'BankTransfer');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve provider identifiers on rollback because clearing them could
            // make an already-created external payment impossible to reconcile.
        }
    }
}
