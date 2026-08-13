using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Domain.Enums;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;

namespace Kilavuz.Web.Controllers;

/// <summary>
/// Public (UI) dosya indirme — Restricted kontrol dahil.
/// Route: /Attachment/Download/{id}
/// </summary>
public class AttachmentController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWebHostEnvironment _env;

    public AttachmentController(IDbConnectionFactory connectionFactory, IWebHostEnvironment env)
    {
        _connectionFactory = connectionFactory;
        _env = env;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        using var connection = _connectionFactory.CreateConnection();

        // 1. Eki getir
        var attachment = await connection.QuerySingleOrDefaultAsync<PageAttachment>(@"
            SELECT * FROM PageAttachments WHERE Id = @Id",
            new { Id = id });

        if (attachment == null) return NotFound();

        // 2. Üst sayfayı getir
        var page = await connection.QuerySingleOrDefaultAsync<Page>(@"
            SELECT * FROM Pages WHERE Id = @Id AND IsDeleted = 0 AND IsActive = 1",
            new { Id = attachment.PageId });

        if (page == null) return NotFound();

        // 3. Üst uygulamayı getir (kategori üzerinden)
        var app = await connection.QuerySingleOrDefaultAsync<AppEntity>(@"
            SELECT a.* FROM Applications a
            JOIN Categories c ON c.ApplicationId = a.Id
            WHERE c.Id = @CategoryId AND a.IsDeleted = 0",
            new { CategoryId = page.CategoryId });

        if (app == null) return NotFound();

        // 4. Kademeli Restricted kontrolü — önce Application, sonra Page
        //    PRD 6.5: Application izni page izni vermez — bağımsız kontrol

        // 4a. Application Restricted kontrolü
        if (app.AccessType == AccessType.Restricted)
        {
            var accessResult = await CheckPermissionAsync(connection, "Application", app.Id);
            if (accessResult != null) return accessResult;
        }

        // 4b. Page Restricted kontrolü (Application'dan bağımsız)
        if (page.AccessType == AccessType.Restricted)
        {
            var accessResult = await CheckPermissionAsync(connection, "Page", page.Id);
            if (accessResult != null) return accessResult;
        }

        // 5. Dosyayı sun
        var physicalPath = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads", "Attachments", attachment.StoredFileName);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return base.File(fileBytes, attachment.ContentType, attachment.FileName);
    }

    // ─── Ortak İzin Kontrol Yardımcısı ─────────────────────────────────────────
    // Güvenlik (Kural 2.1): Parametreli sorgu — SQL injection'a karşı
    private async Task<IActionResult?> CheckPermissionAsync(
        System.Data.IDbConnection connection, string contentType, int contentId)
    {
        // Giriş yapılmamış mı?
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            var returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
            return RedirectToAction("Login", "Auth", new { area = "Panel", ReturnUrl = returnUrl });
        }

        var userId = GetCurrentUserId();
        if (userId == null) return Forbid();

        var hasPermission = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM ContentPermissions
            WHERE ContentType = @ContentType AND ContentId = @ContentId AND UserId = @UserId",
            new { ContentType = contentType, ContentId = contentId, UserId = userId });

        return hasPermission > 0 ? null : Forbid();
    }
}
