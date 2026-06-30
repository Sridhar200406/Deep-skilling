using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Day6_Migrations.Migrations
{
    /// <summary>
    /// Migration: AddSalaryGrade
    /// Adds a new column 'Grade' to the Employees table.
    /// Shows how to ADD a column in a new migration after InitialCreate.
    /// Run with: dotnet ef migrations add AddSalaryGrade
    ///           dotnet ef database update
    /// </summary>
    public partial class AddSalaryGrade : Migration
    {
        // ── UP: Add Grade column ──────────────────────────────
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new nullable column 'Grade' to Employees
            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Employees",
                type: "TEXT",
                maxLength: 1,
                nullable: true,
                defaultValue: "C");

            // Update existing rows: set Grade based on Salary
            migrationBuilder.Sql(@"
                UPDATE Employees
                SET Grade = CASE
                    WHEN Salary >= 55000 THEN 'A'
                    WHEN Salary >= 45000 THEN 'B'
                    ELSE 'C'
                END
            ");
        }

        // ── DOWN: Remove Grade column ─────────────────────────
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Employees");
        }
    }
}
