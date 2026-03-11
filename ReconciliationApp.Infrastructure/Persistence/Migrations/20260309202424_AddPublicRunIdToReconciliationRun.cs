using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconciliationApp.Infrastructure.Persistence.Migrations
{
    public partial class AddPublicRunIdToReconciliationRun : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_run_id",
                table: "reconciliation_runs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE reconciliation_runs
                SET public_run_id = 'legacy-' || ""Id""::text
                WHERE public_run_id IS NULL OR public_run_id = '';
            ");

            migrationBuilder.AlterColumn<string>(
                name: "public_run_id",
                table: "reconciliation_runs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_runs_public_run_id",
                table: "reconciliation_runs",
                column: "public_run_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reconciliation_runs_public_run_id",
                table: "reconciliation_runs");

            migrationBuilder.DropColumn(
                name: "public_run_id",
                table: "reconciliation_runs");
        }
    }
}
