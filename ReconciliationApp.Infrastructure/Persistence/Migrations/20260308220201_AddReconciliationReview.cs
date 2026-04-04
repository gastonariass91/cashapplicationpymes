using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconciliationApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reconciliation_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconciliation_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reconciliation_runs_batch_runs_batch_run_id",
                        column: x => x.batch_run_id,
                        principalTable: "batch_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    reconciliation_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    debt_row_number = table.Column<int>(type: "integer", nullable: false),
                    payment_row_number = table.Column<int>(type: "integer", nullable: false),
                    customer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    debt_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    delta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rule = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    confidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    match_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    evidence = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    suggestion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconciliation_cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reconciliation_cases_reconciliation_runs_reconciliation_run~",
                        column: x => x.reconciliation_run_id,
                        principalTable: "reconciliation_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_cases_reconciliation_run_id_case_id",
                table: "reconciliation_cases",
                columns: new[] { "reconciliation_run_id", "case_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_runs_batch_run_id",
                table: "reconciliation_runs",
                column: "batch_run_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reconciliation_cases");

            migrationBuilder.DropTable(
                name: "reconciliation_runs");
        }
    }
}
