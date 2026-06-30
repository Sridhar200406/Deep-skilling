using Day6_Migrations.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Day6_Migrations.Migrations
{
    /// <summary>
    /// Auto-generated snapshot of the current model state.
    /// EF Core uses this to detect what changed between migrations.
    /// Do NOT edit manually.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Day6_Migrations.Models.Department", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");
                b.Property<string>("Location").IsRequired()
                    .HasMaxLength(200).HasColumnType("TEXT");
                b.Property<string>("Name").IsRequired()
                    .HasMaxLength(100).HasColumnType("TEXT");
                b.HasKey("Id");
                b.ToTable("Departments");
                b.HasData(
                    new { Id = 1, Location = "New York",    Name = "IT"      },
                    new { Id = 2, Location = "Chicago",     Name = "HR"      },
                    new { Id = 3, Location = "Los Angeles", Name = "Finance" });
            });

            modelBuilder.Entity("Day6_Migrations.Models.Employee", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");
                b.Property<string>("Department").IsRequired().HasColumnType("TEXT");
                b.Property<int>("DepartmentId").HasColumnType("INTEGER");
                b.Property<string>("Grade").HasMaxLength(1).HasColumnType("TEXT");
                b.Property<DateTime>("JoinedDate").HasColumnType("TEXT");
                b.Property<string>("Name").IsRequired()
                    .HasMaxLength(100).HasColumnType("TEXT");
                b.Property<decimal>("Salary")
                    .HasColumnType("decimal(18,2)");
                b.HasKey("Id");
                b.HasIndex("DepartmentId");
                b.ToTable("Employees");
                b.HasData(
                    new { Id = 1, Department = "IT",      DepartmentId = 1, JoinedDate = new DateTime(2020,1,15),  Name = "John",  Salary = 50000m },
                    new { Id = 2, Department = "HR",      DepartmentId = 2, JoinedDate = new DateTime(2019,3,10),  Name = "David", Salary = 35000m },
                    new { Id = 3, Department = "IT",      DepartmentId = 1, JoinedDate = new DateTime(2021,6,1),   Name = "Mary",  Salary = 60000m },
                    new { Id = 4, Department = "Finance", DepartmentId = 3, JoinedDate = new DateTime(2018,9,20),  Name = "Smith", Salary = 45000m },
                    new { Id = 5, Department = "HR",      DepartmentId = 2, JoinedDate = new DateTime(2022,2,5),   Name = "James", Salary = 40000m });
            });

            modelBuilder.Entity("Day6_Migrations.Models.Employee", b =>
            {
                b.HasOne("Day6_Migrations.Models.Department", "Dept")
                    .WithMany("Employees")
                    .HasForeignKey("DepartmentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
                b.Navigation("Dept");
            });

            modelBuilder.Entity("Day6_Migrations.Models.Department", b =>
            {
                b.Navigation("Employees");
            });
#pragma warning restore 612, 618
        }
    }
}
