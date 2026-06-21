-- Create Stored Procedure

CREATE PROCEDURE GetTopPaidEmployees
AS
BEGIN
    SELECT TOP 5
        EmployeeID,
        EmployeeName,
        Department,
        Salary
    FROM Employees
    ORDER BY Salary DESC;
END;
GO