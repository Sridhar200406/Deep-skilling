import sqlite3

# ── Setup ──────────────────────────────────────────────────────────────────────
conn = sqlite3.connect(":memory:")
cur = conn.cursor()

cur.execute("""
    CREATE TABLE Employees (
        EmployeeID   INTEGER PRIMARY KEY,
        EmployeeName TEXT,
        Department   TEXT,
        Salary       REAL
    )
""")

cur.executemany(
    "INSERT INTO Employees VALUES (?, ?, ?, ?)",
    [
        (1,  "Alice",   "Engineering", 95000),
        (2,  "Bob",     "Engineering", 85000),
        (3,  "Carol",   "Engineering", 85000),
        (4,  "Dave",    "Marketing",   70000),
        (5,  "Eve",     "Marketing",   75000),
        (6,  "Frank",   "Marketing",   70000),
        (7,  "Grace",   "HR",          60000),
        (8,  "Hank",    "HR",          65000),
        (9,  "Ivy",     "HR",          60000),
        (10, "Jack",    "Engineering", 90000),
    ],
)
conn.commit()


def run(title, sql):
    print(f"\n{'='*60}")
    print(f" {title}")
    print('='*60)
    cur.execute(sql)
    cols = [d[0] for d in cur.description]
    rows = cur.fetchall()
    # header
    col_widths = [max(len(c), max((len(str(r[i])) for r in rows), default=0)) for i, c in enumerate(cols)]
    header = "  ".join(c.ljust(w) for c, w in zip(cols, col_widths))
    print(header)
    print("  ".join("-" * w for w in col_widths))
    for row in rows:
        print("  ".join(str(v).ljust(w) for v, w in zip(row, col_widths)))


# ── Ex 1: Ranking and Window Functions (Global) ────────────────────────────────
run("Ex 1: Ranking and Window Functions (Global)", """
SELECT
    EmployeeID,
    EmployeeName,
    Department,
    Salary,
    ROW_NUMBER() OVER (ORDER BY Salary DESC)  AS RowNum,
    RANK()       OVER (ORDER BY Salary DESC)  AS RankValue,
    DENSE_RANK() OVER (ORDER BY Salary DESC)  AS DenseRankValue
FROM Employees
""")

# ── Ex 2: Ranking Within Each Department (PARTITION BY) ───────────────────────
run("Ex 2: Ranking Within Each Department (PARTITION BY)", """
SELECT
    EmployeeID,
    EmployeeName,
    Department,
    Salary,
    RANK()       OVER (PARTITION BY Department ORDER BY Salary DESC) AS DeptRank,
    DENSE_RANK() OVER (PARTITION BY Department ORDER BY Salary DESC) AS DeptDenseRank
FROM Employees
""")

# ── Ex 3: Running Total and Moving Average ─────────────────────────────────────
run("Ex 3: Running Total and Moving Average", """
SELECT
    EmployeeID,
    EmployeeName,
    Department,
    Salary,
    SUM(Salary) OVER (
        ORDER BY Salary DESC
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) AS RunningTotal,
    ROUND(AVG(Salary) OVER (
        ORDER BY Salary DESC
        ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
    ), 2) AS MovingAvg3
FROM Employees
""")

# ── Ex 4: CTE - Top Earner Per Department ─────────────────────────────────────
run("Ex 4: CTE - Top Earner Per Department", """
WITH DeptRanked AS (
    SELECT
        EmployeeID,
        EmployeeName,
        Department,
        Salary,
        RANK() OVER (PARTITION BY Department ORDER BY Salary DESC) AS rnk
    FROM Employees
)
SELECT EmployeeID, EmployeeName, Department, Salary
FROM DeptRanked
WHERE rnk = 1
""")

# ── Ex 5: Salary Difference from Department Average ───────────────────────────
run("Ex 5: Salary Difference from Department Average", """
SELECT
    EmployeeID,
    EmployeeName,
    Department,
    Salary,
    ROUND(AVG(Salary) OVER (PARTITION BY Department), 2) AS DeptAvgSalary,
    ROUND(Salary - AVG(Salary) OVER (PARTITION BY Department), 2) AS DiffFromAvg
FROM Employees
ORDER BY Department, Salary DESC
""")

conn.close()
print("\nDone.")
