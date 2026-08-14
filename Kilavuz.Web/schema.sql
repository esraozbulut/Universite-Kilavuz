-- schema.sql
-- Kurumsal Kullanım Kılavuzu Yönetim Sistemi Veritabanı Şeması

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(100) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE UserRoles (
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(Id),
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE Applications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    IconPath NVARCHAR(500),
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    IsPinned BIT NOT NULL DEFAULT 0,
    AccessType NVARCHAR(50) NOT NULL DEFAULT 'Public' CHECK (AccessType IN ('Public', 'Restricted')), -- 'Public' or 'Restricted'
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ApplicationId INT NOT NULL FOREIGN KEY REFERENCES Applications(Id),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE Pages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL FOREIGN KEY REFERENCES Categories(Id),
    Title NVARCHAR(255) NOT NULL,
    ContentHtml NVARCHAR(MAX),
    CoverImagePath NVARCHAR(500),
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    AccessType NVARCHAR(50) NOT NULL DEFAULT 'Public' CHECK (AccessType IN ('Public', 'Restricted')), -- 'Public' or 'Restricted'
    CreatedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE PageAttachments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PageId INT NOT NULL FOREIGN KEY REFERENCES Pages(Id),
    FileName NVARCHAR(500) NOT NULL,
    StoredFileName NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    UploadedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE ContentPermissions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ContentType NVARCHAR(50) NOT NULL CHECK (ContentType IN ('Application', 'Page')), -- 'Application' or 'Page'
    ContentId INT NOT NULL,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    GrantedByUserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    GrantedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE ErrorLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TimeStamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Message NVARCHAR(MAX) NOT NULL,
    StackTrace NVARCHAR(MAX),
    RequestPath NVARCHAR(500),
    IPAddress NVARCHAR(50)
);

CREATE TABLE LoginAttempts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserNameAttempted NVARCHAR(255) NOT NULL,
    IPAddress NVARCHAR(50),
    IsSuccess BIT NOT NULL,
    AttemptedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Varsayılan Rollerin Eklenmesi
INSERT INTO Roles (Name) VALUES ('SuperAdmin'), ('Yetkili'), ('Kullanici');
