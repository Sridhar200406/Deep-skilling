using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeService.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Departments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Departments", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Employees",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                DepartmentId = table.Column<int>(type: "int", nullable: false),
                ProfilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Employees", x => x.Id);
                table.ForeignKey(
                    name: "FK_Employees_Departments_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "Departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EmployeeDocuments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                BlobUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                BlobName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmployeeDocuments_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Seed Departments
        migrationBuilder.InsertData(
            table: "Departments",
            columns: new[] { "Id", "Name", "Description", "IsActive", "CreatedAt" },
            values: new object[,]
            {
                { 1, "Engineering", "Software Engineering Department", true, new DateTime(2024, 1, 1) },
                { 2, "Human Resources", "HR Department", true, new DateTime(2024, 1, 1) },
                { 3, "Finance", "Finance Department", true, new DateTime(2024, 1, 1) },
                { 4, "Marketing", "Marketing Department", true, new DateTime(2024, 1, 1) }
            });

        // Seed admin user (password: Admin@123, hashed with BCrypt)
        migrationBuilder.InsertData(
            table: "Users",
            columns: new[] { "Id", "Username", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt" },
            values: new object[] { 1, "admin", "admin@company.com",
                "$2a$11$rBtB3gMXRdRE/i9hJjNDEuOGJN7bkFJEaU1FQPqTRoFEMpLiNBn2i",
                "Admin", true, new DateTime(2024, 1, 1) });

        // Indexes
        migrationBuilder.CreateIndex(name: "IX_Departments_Name", table: "Departments", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Employees_Email", table: "Employees", column: "Email", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Employees_DepartmentId", table: "Employees", column: "DepartmentId");
        migrationBuilder.CreateIndex(name: "IX_EmployeeDocuments_EmployeeId", table: "EmployeeDocuments", column: "EmployeeId");
        migrationBuilder.CreateIndex(name: "IX_Users_Username", table: "Users", column: "Username", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmployeeDocuments");
        migrationBuilder.DropTable(name: "Employees");
        migrationBuilder.DropTable(name: "Departments");
        migrationBuilder.DropTable(name: "Users");
    }
}
