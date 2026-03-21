using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconciliationApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakePaymentNumberUniquePerCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_company_id_payment_number",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "IX_payments_company_id_payment_number",
                table: "payments",
                columns: new[] { "company_id", "payment_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_company_id_payment_number",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "IX_payments_company_id_payment_number",
                table: "payments",
                columns: new[] { "company_id", "payment_number" });
        }
    }
}
