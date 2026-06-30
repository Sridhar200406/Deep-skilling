using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Week2_Day7_LoadingStrategies.Models
{
    /// <summary>
    /// Employee entity — maps to the Employees table.
    /// Carries all fields from Day 2–6 plus virtual navigation
    /// property for Lazy Loading support (Day 7).
    /// </summary>
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int      EmployeeId   { get; set; }

        [Required]
        [MaxLength(100)]
        public string   Name         { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string   Department   { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal  Salary       { get; set; }

        public DateTime JoinedDate   { get; set; }

        // ── Relationship (from Day 6) ──────────────────────
        public int         DepartmentId { get; set; }               // Foreign Key

        // virtual → required for Lazy Loading via EF Core Proxies.
        // EF Core will replace this with a proxy that fires a SQL
        // query the first time the property is accessed.
        public virtual Department? Dept { get; set; }               // Navigation property
    }
}
