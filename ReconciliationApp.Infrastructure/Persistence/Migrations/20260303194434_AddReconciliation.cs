using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconciliationApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReconciledAt",
                table: "batch_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reconciliation_matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebtRowNumber = table.Column<int>(type: "integer", nullable: false),
                    PaymentRowNumber = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconciliation_matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reconciliation_matches_batch_runs_BatchRunId",
                        column: x => x.BatchRunId,
                        principalTable: "batch_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_matches_BatchRunId_DebtRowNumber",
                table: "reconciliation_matches",
                columns: new[] { "BatchRunId", "DebtRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_matches_BatchRunId_PaymentRowNumber",
                table: "reconciliation_matches",
                columns: new[] { "BatchRunId", "PaymentRowNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reconciliation_matches");

            migrationBuilder.DropColumn(
                name: "ReconciledAt",
                table: "batch_runs");
        }
    }
}
