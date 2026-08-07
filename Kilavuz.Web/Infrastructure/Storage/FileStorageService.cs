using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Application.DTOs;

namespace Kilavuz.Web.Infrastructure.Storage;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    
    private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly string[] _attachmentExtensions = { ".pdf", ".docx", ".xlsx", ".zip", ".jpg", ".jpeg", ".png", ".gif" };
    
    public FileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    public async Task<FileUploadResult> UploadImageAsync(IFormFile file)
    {
        return await UploadFileInternalAsync(file, _imageExtensions, true);
    }

    public async Task<FileUploadResult> UploadAttachmentAsync(IFormFile file)
    {
        return await UploadFileInternalAsync(file, _attachmentExtensions, false);
    }

    private async Task<FileUploadResult> UploadFileInternalAsync(IFormFile file, string[] allowedExtensions, bool isImage)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Dosya seçilmedi.");

        // 1. Max Size Check
        var maxMb = _config.GetValue<int>("FileStorage:MaxFileSizeMb");
        if (maxMb == 0) maxMb = 10; // Default 10 MB
        var maxBytes = maxMb * 1024 * 1024;
        
        if (file.Length > maxBytes)
            throw new ArgumentException($"Dosya boyutu çok büyük. Maksimum izin verilen: {maxMb} MB.");

        var originalFileName = file.FileName;
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        // 2. Extension Whitelist Check
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException($"Desteklenmeyen dosya uzantısı. İzin verilenler: {string.Join(", ", allowedExtensions)}");

        // 3. MIME Type Check (Basic)
        var mimeType = file.ContentType.ToLowerInvariant();
        if (isImage && !mimeType.StartsWith("image/"))
            throw new ArgumentException("Geçersiz MIME tipi (Görsel bekleniyor).");

        // 4. Magic Number (File Signature) Check
        if (!FileSignatureChecker.IsValidSignature(file, extension))
            throw new ArgumentException("Dosya imzası (magic number) geçersiz. Uzantı ile dosya içeriği uyuşmuyor.");

        // 5. GUID Naming & Save Path
        var storedFileName = Guid.NewGuid().ToString() + extension;
        string targetDirectory;
        string relativePath;

        if (isImage)
        {
            // Images go to wwwroot/uploads/images
            targetDirectory = Path.Combine(_env.WebRootPath, "uploads", "images");
            relativePath = $"/uploads/images/{storedFileName}";
        }
        else
        {
            // Attachments go outside wwwroot for security (e.g., App_Data/Uploads/Attachments)
            // But _env.ContentRootPath is the root of the project.
            targetDirectory = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads", "Attachments");
            relativePath = $"/App_Data/Uploads/Attachments/{storedFileName}"; 
            // The relativePath here is just a logical path for the DB, it won't be statically served.
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var fullPath = Path.Combine(targetDirectory, storedFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return new FileUploadResult(
            OriginalFileName: originalFileName,
            StoredFileName: storedFileName,
            FileSize: file.Length,
            ContentType: file.ContentType,
            RelativePath: relativePath
        );
    }
}
