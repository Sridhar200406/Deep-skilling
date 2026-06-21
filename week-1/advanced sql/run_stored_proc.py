import sqlite3

conn = sqlite3.connect(":memory:")
cur = conn.cursor()

# Create table (mirrors SQL-2_create_stored_procedure.sql)
cur.execute("""
    CREATE TABLE Employees (
        EmployeeID   INTEGER PRIMARY KEY,
        EmployeeName TEXT,
        Department   TEXT,
        Salary       REAL
    )
""")

# Sample data
cur.executemany("INSERT INTO Employees VALUES (?, ?, ?, ?)", [
    (1,  "Alice",  "Engineering", 95000),
    (2,  "Bob",    "Engineering", 85000),
    (3,  "Carol",  "Engineering", 85000),
    (4,  "Dave",   "Marketing",   70000),
    (5,  "Eve",    "Marketing",   75000),
    (6,  "Frank",  "Marketing",   70000),
    (7,  "Grace",  "HR",          60000),
    (8,  "Hank",   "HR",          65000),
    (9,  "Ivy",    "HR",          60000),
    (10, "Jack",   "Engineering", 90000),
])
conn.commit()

# Simulate: CREATE PROCEDURE GetEmployeesByDepartment @DepartmentName VARCHAR(50)
def GetEmployeesByDepartment(department_name):
    print(f"\nEXEC GetEmployeesByDepartment @DepartmentName = '{department_name}'")
    print("-" * 55)
    cur.execute("SELECT * FROM Employees WHERE Department = ?", (department_name,))
    rows = cur.fetchall()
    print(f"  {'EmployeeID':<12} {'EmployeeName':<15} {'Department':<15} {'Salary'}")
    print(f"  {'-'*10:<12} {'-'*12:<15} {'-'*11:<15} {'-'*10}")
    for row in rows:
        print(f"  {str(row[0]):<12} {row[1]:<15} {row[2]:<15} {row[3]}")
    print(f"  ({len(rows)} row(s) returned)")

# Call the stored procedure for each department
GetEmployeesByDepartment("Engineering")
GetEmployeesByDepartment("Marketing")
GetEmployeesByDepartment("HR")

conn.close()
print("\nDone.")
