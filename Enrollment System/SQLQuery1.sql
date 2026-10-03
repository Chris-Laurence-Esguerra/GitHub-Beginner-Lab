CREATE DATABASE EnrollmentDB;


USE EnrollmentDB;


CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL,
    Password VARCHAR(50) NOT NULL,
    Role VARCHAR(20) NOT NULL 
);


INSERT INTO Users (Username, Password, Role) VALUES ('Admin', 'Cashier', 'Admin');
INSERT INTO Users (Username, Password, Role) VALUES ('Student', 'Student', 'Student');
USE EnrollmentDB;
SELECT * FROM Users;