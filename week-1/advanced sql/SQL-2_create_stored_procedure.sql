-- Create Employee Table

CREATE TABLE Employees (
    EmployeeID INT PRIMARY KEY,
    EmployeeName VARCHAR(50),
    Department VARCHAR(50),
    Salary DECIMAL(10,2)
);

-- Create Stored Procedure

CREATE PROCEDURE GetEmployeesByDepartment
    @DepartmentName VARCHAR(50)
AS
BEGIN
    SELECT *
    FROM Employees
    WHERE Department = @DepartmentName;
END;
GO