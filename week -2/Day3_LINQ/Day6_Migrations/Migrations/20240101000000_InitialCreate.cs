using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Day6_Migrations.Migrations
{
    /// <summary>
    /// Migration: InitialCreate
    /// Creates Departments and Employees tables with seeded data.
    /// Run with: dotnet ef migrations add InitialCreate
    ///           dotnet ef database update
    /// </summary>
    public partial class InitialCreate : Migration
    {
        // ── UP: Apply migration (create tables) ──────────────
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Departments table
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id       = table.Column<int>(type: "INTEGER", nullable: false)
                                    .Annotation("Sqlite:Autoincrement", true),
                    Name     = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            // Create Employees table
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id           = table.Column<int>(type: "INTEGER", nullable: false)
                                        .Annotation("Sqlite:Autoincrement", true),
                    Name         = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Department   = table.Column<string>(type: "TEXT", nullable: false),
                    Salary       = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JoinedDate   = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── SEED DEPARTMENTS ─────────────────────────────
            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Name", "Location" },
                values: new object[,]
                {
                    { 1, "IT",      "New York"    },
                    { 2, "HR",      "Chicago"     },
                    { 3, "Finance", "Los Angeles" }
                });

            // ── SEED EMPLOYEES ───────────────────────────────
            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Name", "Department", "Salary", "JoinedDate", "DepartmentId" },
                values: new object[,]
                {
                    { 1, "John",  "IT",      50000m, new DateTime(2020, 1, 15), 1 },
                    { 2, "David", "HR",      35000m, new DateTime(2019, 3, 10), 2 },
                    { 3, "Mary",  "IT",      60000m, new DateTime(2021, 6, 1),  1 },
                    { 4, "Smith", "Finance", 45000m, new DateTime(2018, 9, 20), 3 },
                    { 5, "James", "HR",      40000m, new DateTime(2022, 2, 5),  2 }
                });

            // Create index on DepartmentId foreign key
            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");
        }

        // ── DOWN: Rollback migration (drop tables) ────────────
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Employees");
            migrationBuilder.DropTable(name: "Departments");
        }
    }
}
