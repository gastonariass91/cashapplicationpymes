using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReconciliationApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResolvedByToReconciliationCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolvedBy",
                table: "reconciliation_cases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "reconciliation_cases");
        }
    }
}
