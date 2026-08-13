using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Enums;
using Kilavuz.Web.Models;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Controllers;

/// <summary>
/// UI (Public) — Uygulama detayı: /kilavuz/{appId}
/// </summary>
public class KilavuzController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public KilavuzController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    // ─── Erişim Kontrolü (Ortak) ──────────────────────────────────────────────
    // PRD 6.5: Public → göster. Restricted → login yoksa yönlendir, varsa izin tablosunu kontrol et.
    private async Task<IActionResult?> CheckAccessAsync(AccessType accessType, string contentType, int contentId)
    {
        if (accessType == AccessType.Public)
            return null; // Erişim serbest

        // Restricted — giriş yapılmamış mı?
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            var returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
            return RedirectToAction("Login", "Auth", new { area = "Panel", ReturnUrl = returnUrl });
        }

        var userId = GetCurrentUserId();
        if (userId == null) return Forbid();

        using var connection = _connectionFactory.CreateConnection();
        // Güvenlik: Parametreli sorgu — SQL injection'a karşı (Kural 2.1)
        var hasPermission = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM ContentPermissions
            WHERE ContentType = @ContentType AND ContentId = @ContentId AND UserId = @UserId",
            new { ContentType = contentType, ContentId = contentId, UserId = userId });

        return hasPermission > 0 ? null : Forbid();
    }

    // ─── Uygulama Detayı ─────────────────────────────────────────────────────
    // Route: /kilavuz/{appId}
    [HttpGet("kilavuz/{appId:int}")]
    public async Task<IActionResult> Application(int appId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var app = await connection.QuerySingleOrDefaultAsync<AppEntity>(@"
            SELECT * FROM Applications WHERE Id = @Id AND IsDeleted = 0 AND IsActive = 1",
            new { Id = appId });

        if (app == null) return NotFound();

        // Erişim kontrolü
        var accessCheck = await CheckAccessAsync(app.AccessType, "Application", app.Id);
        if (accessCheck != null) return accessCheck;

        var categories = (await connection.QueryAsync<Category>(@"
            SELECT * FROM Categories WHERE ApplicationId = @AppId AND IsDeleted = 0 AND IsActive = 1
            ORDER BY SortOrder ASC",
            new { AppId = appId })).AsList();

        return View(new ApplicationDetailViewModel { Application = app, Categories = categories });
    }

    // ─── Kategori Detayı ─────────────────────────────────────────────────────
    // Route: /kilavuz/{appId}/{categoryId}
    [HttpGet("kilavuz/{appId:int}/{categoryId:int}")]
    public async Task<IActionResult> Category(int appId, int categoryId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var app = await connection.QuerySingleOrDefaultAsync<AppEntity>(@"
            SELECT * FROM Applications WHERE Id = @Id AND IsDeleted = 0 AND IsActive = 1",
            new { Id = appId });

        if (app == null) return NotFound();

        // Kategori sayfasına erişim üst uygulamanın AccessType'ına göre kontrol edilir
        var accessCheck = await CheckAccessAsync(app.AccessType, "Application", app.Id);
        if (accessCheck != null) return accessCheck;

        var category = await connection.QuerySingleOrDefaultAsync<Category>(@"
            SELECT * FROM Categories WHERE Id = @Id AND ApplicationId = @AppId AND IsDeleted = 0 AND IsActive = 1",
            new { Id = categoryId, AppId = appId });

        if (category == null) return NotFound();

        var pages = (await connection.QueryAsync<Page>(@"
            SELECT * FROM Pages WHERE CategoryId = @CategoryId AND IsDeleted = 0 AND IsActive = 1
            ORDER BY SortOrder ASC",
            new { CategoryId = categoryId })).AsList();

        return View(new CategoryDetailViewModel { Application = app, Category = category, Pages = pages });
    }

    // ─── Sayfa Detayı ────────────────────────────────────────────────────────
    // Route: /kilavuz/{appId}/{categoryId}/{pageId}
    [HttpGet("kilavuz/{appId:int}/{categoryId:int}/{pageId:int}")]
    public async Task<IActionResult> Page(int appId, int categoryId, int pageId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var app = await connection.QuerySingleOrDefaultAsync<AppEntity>(@"
            SELECT * FROM Applications WHERE Id = @Id AND IsDeleted = 0 AND IsActive = 1",
            new { Id = appId });

        if (app == null) return NotFound();

        var category = await connection.QuerySingleOrDefaultAsync<Category>(@"
            SELECT * FROM Categories WHERE Id = @Id AND ApplicationId = @AppId AND IsDeleted = 0 AND IsActive = 1",
            new { Id = categoryId, AppId = appId });

        if (category == null) return NotFound();

        var page = await connection.QuerySingleOrDefaultAsync<Page>(@"
            SELECT * FROM Pages WHERE Id = @Id AND CategoryId = @CategoryId AND IsDeleted = 0 AND IsActive = 1",
            new { Id = pageId, CategoryId = categoryId });

        if (page == null) return NotFound();

        // Kademeli erişim kontrolü: önce Uygulama, sonra Sayfa
        var appAccessCheck = await CheckAccessAsync(app.AccessType, "Application", app.Id);
        if (appAccessCheck != null) return appAccessCheck;

        var pageAccessCheck = await CheckAccessAsync(page.AccessType, "Page", page.Id);
        if (pageAccessCheck != null) return pageAccessCheck;

        var attachments = (await connection.QueryAsync<PageAttachment>(@"
            SELECT * FROM PageAttachments WHERE PageId = @PageId",
            new { PageId = pageId })).AsList();

        return View(new PageDetailViewModel
        {
            Application = app,
            Category = category,
            Page = page,
            Attachments = attachments
        });
    }
}
