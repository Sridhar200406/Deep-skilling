using System;
using System.Linq;

class Program
{
    static void Main()
    {
        using var db = new AppDbContext();

        // Ensure database is created
        db.Database.EnsureCreated();

        // ── INSERT ──────────────────────────────────────────────
        Console.WriteLine("=== INSERT ===");
        db.Students.Add(new Student { Name = "Alice", Age = 20 });
        db.Students.Add(new Student { Name = "Bob",   Age = 22 });
        db.Students.Add(new Student { Name = "Carol", Age = 21 });
        db.SaveChanges();
        Console.WriteLine("3 students inserted.");

        // ── READ (SELECT ALL) ────────────────────────────────────
        Console.WriteLine("\n=== READ ALL ===");
        foreach (var s in db.Students.ToList())
        {
            Console.WriteLine($"  Id={s.Id}  Name={s.Name}  Age={s.Age}");
        }

        // ── UPDATE ───────────────────────────────────────────────
        Console.WriteLine("\n=== UPDATE ===");
        var studentToUpdate = db.Students.FirstOrDefault(s => s.Name == "Alice");
        if (studentToUpdate != null)
        {
            studentToUpdate.Age = 25;
            db.SaveChanges();
            Console.WriteLine($"  Updated Alice's age to {studentToUpdate.Age}");
        }

        // ── DELETE ───────────────────────────────────────────────
        Console.WriteLine("\n=== DELETE ===");
        var studentToDelete = db.Students.FirstOrDefault(s => s.Name == "Bob");
        if (studentToDelete != null)
        {
            db.Students.Remove(studentToDelete);
            db.SaveChanges();
            Console.WriteLine("  Deleted Bob.");
        }

        // ── FINAL READ ───────────────────────────────────────────
        Console.WriteLine("\n=== FINAL LIST ===");
        foreach (var s in db.Students.ToList())
        {
            Console.WriteLine($"  Id={s.Id}  Name={s.Name}  Age={s.Age}");
        }

        Console.WriteLine("\nDone!");
    }
}
