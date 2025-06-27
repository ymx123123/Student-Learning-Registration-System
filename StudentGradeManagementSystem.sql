CREATE DATABASE StudentGradeManagementSystem;
GO

USE StudentGradeManagementSystem;

CREATE TABLE Teachers (
    TeacherID INT PRIMARY KEY NOT NULL,
    TeacherName NVARCHAR(50) NOT NULL
);

CREATE TABLE Students (
    StudentID INT PRIMARY KEY NOT NULL,
    StudentName NVARCHAR(50) NOT NULL,
    Gender NVARCHAR(10) NULL,
    BirthDate DATE NULL,
    Class NVARCHAR(50) NULL
);

CREATE TABLE Courses (
    CourseID INT PRIMARY KEY NOT NULL,
    CourseName NVARCHAR(100) NOT NULL,
    Credit INT NULL,
    TeacherID INT NOT NULL,
    CONSTRAINT FK_Courses_Teachers FOREIGN KEY (TeacherID) REFERENCES Teachers(TeacherID)
);

CREATE TABLE Grades (
    GradeID INT PRIMARY KEY NOT NULL,
    StudentID INT NOT NULL,
    CourseID INT NOT NULL,
    GradeValue DECIMAL(5,2) NULL,
    TeacherID INT NOT NULL,
    CONSTRAINT FK_Grades_Students FOREIGN KEY (StudentID) REFERENCES Students(StudentID),
    CONSTRAINT FK_Grades_Courses FOREIGN KEY (CourseID) REFERENCES Courses(CourseID),
    CONSTRAINT FK_Grades_Teachers FOREIGN KEY (TeacherID) REFERENCES Teachers(TeacherID)
);