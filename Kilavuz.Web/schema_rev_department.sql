-- Departman Tablosu
CREATE TABLE Departments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL UNIQUE,
    Slug NVARCHAR(255) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- Departman Kullanıcıları Tablosu
CREATE TABLE DepartmentUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentId INT NOT NULL FOREIGN KEY REFERENCES Departments(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    AssignedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_DepartmentUser UNIQUE (DepartmentId, UserId)
);

-- Uygulamalara DepartmentId Eklenmesi
ALTER TABLE Applications
ADD DepartmentId INT NULL FOREIGN KEY REFERENCES Departments(Id);
