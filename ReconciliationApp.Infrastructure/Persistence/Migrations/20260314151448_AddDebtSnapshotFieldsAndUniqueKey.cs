using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconciliationApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtSnapshotFieldsAndUniqueKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                table: "debts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closed_reason",
                table: "debts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_debts_company_id_customer_id_invoice_number",
                table: "debts",
                columns: new[] { "company_id", "customer_id", "invoice_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_debts_company_id_customer_id_invoice_number",
                table: "debts");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "debts");

            migrationBuilder.DropColumn(
                name: "closed_reason",
                table: "debts");
        }
    }
}
