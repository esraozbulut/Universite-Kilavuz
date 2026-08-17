USE KilavuzDB;
GO

IF NOT EXISTS (SELECT 1 FROM Departments WHERE Slug = 'bilgi-islem')
BEGIN
    INSERT INTO Departments (Name, Slug, Description, IsActive, IsDeleted, CreatedAt, CreatedByUserId)
    VALUES (N'Bilgi İşlem', 'bilgi-islem', N'Bilgi İşlem Departmanı Kılavuzları', 1, 0, GETUTCDATE(), 4); -- SuperAdmin ID usually 1
END

IF NOT EXISTS (SELECT 1 FROM Departments WHERE Slug = 'insan-kaynaklari')
BEGIN
    INSERT INTO Departments (Name, Slug, Description, IsActive, IsDeleted, CreatedAt, CreatedByUserId)
    VALUES (N'İnsan Kaynakları', 'insan-kaynaklari', N'İnsan Kaynakları Departmanı Kılavuzları', 1, 0, GETUTCDATE(), 4);
END
GO
