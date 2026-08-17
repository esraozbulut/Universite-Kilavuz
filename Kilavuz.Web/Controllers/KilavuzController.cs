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

    // ─── Kılavuz Listeleme (Ana Kılavuz) ─────────────────────────────────────
    // Route: /kilavuz
    [HttpGet("kilavuz")]
    public async Task<IActionResult> Index()
    {
        using var connection = _connectionFactory.CreateConnection();
        var userId = GetCurrentUserId();
        
        // Sadece Ana Üniversite Kılavuzu'na ait (DepartmentId IS NULL) uygulamaları çek
        var apps = (await connection.QueryAsync<AppEntity>(@"
            SELECT * FROM Applications 
            WHERE IsDeleted = 0 AND IsActive = 1 AND DepartmentId IS NULL
            ORDER BY SortOrder ASC")).AsList();

        List<int> permittedAppIds = new();
        if (userId != null)
        {
            permittedAppIds = (await connection.QueryAsync<int>(@"
                SELECT ContentId FROM ContentPermissions 
                WHERE ContentType = 'Application' AND UserId = @UserId", 
                new { UserId = userId })).AsList();
        }

        var filteredApps = apps.Where(a => 
            a.AccessType == AccessType.Public || permittedAppIds.Contains(a.Id)
        ).ToList();

        var pinnedApps = filteredApps.Where(a => a.IsPinned).OrderBy(a => a.SortOrder).ToList();
        List<AppEntity> finalApps;
        if (pinnedApps.Count >= 10)
        {
            finalApps = pinnedApps;
        }
        else
        {
            var recentApps = filteredApps.Where(a => !a.IsPinned).OrderByDescending(a => a.Id).Take(10 - pinnedApps.Count).ToList();
            finalApps = pinnedApps.Concat(recentApps).ToList();
        }

        ViewBag.DepartmentName = null; // Ana Kılavuz işareti
        return View(finalApps);
    }

    // ─── Kılavuz Listeleme (Departman Kılavuzu) ──────────────────────────────
    // Route: /kilavuz/{departmentSlug}
    [HttpGet("kilavuz/{departmentSlug}")]
    public async Task<IActionResult> DepartmentIndex(string departmentSlug)
    {
        using var connection = _connectionFactory.CreateConnection();
        var userId = GetCurrentUserId();

        // Departmanı bul (Aktif ve Silinmemiş)
        var department = await connection.QuerySingleOrDefaultAsync<Department>(@"
            SELECT * FROM Departments 
            WHERE Slug = @Slug AND IsDeleted = 0 AND IsActive = 1", 
            new { Slug = departmentSlug });

        if (department == null) return NotFound();

        // İlgili departmana ait uygulamaları çek
        var apps = (await connection.QueryAsync<AppEntity>(@"
            SELECT * FROM Applications 
            WHERE IsDeleted = 0 AND IsActive = 1 AND DepartmentId = @DeptId
            ORDER BY SortOrder ASC", new { DeptId = department.Id })).AsList();

        List<int> permittedAppIds = new();
        if (userId != null)
        {
            permittedAppIds = (await connection.QueryAsync<int>(@"
                SELECT ContentId FROM ContentPermissions 
                WHERE ContentType = 'Application' AND UserId = @UserId", 
                new { UserId = userId })).AsList();
        }

        var filteredApps = apps.Where(a => 
            a.AccessType == AccessType.Public || permittedAppIds.Contains(a.Id)
        ).ToList();

        var pinnedApps = filteredApps.Where(a => a.IsPinned).OrderBy(a => a.SortOrder).ToList();
        List<AppEntity> finalApps;
        if (pinnedApps.Count >= 10)
        {
            finalApps = pinnedApps;
        }
        else
        {
            var recentApps = filteredApps.Where(a => !a.IsPinned).OrderByDescending(a => a.Id).Take(10 - pinnedApps.Count).ToList();
            finalApps = pinnedApps.Concat(recentApps).ToList();
        }

        ViewBag.DepartmentName = department.Name;
        // İstenilen view Kilavuz/Index.cshtml
        return View("Index", finalApps);
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

        // Uygulamanın ilk kategorisini bul
        var firstCategory = await connection.QueryFirstOrDefaultAsync<Category>(@"
            SELECT * FROM Categories WHERE ApplicationId = @AppId AND IsDeleted = 0 AND IsActive = 1
            ORDER BY SortOrder ASC",
            new { AppId = appId });

        if (firstCategory == null)
            return View("NoContent", app); // Kategori yoksa özel bir görünüm veya hata gösterilebilir

        // İlk kategorinin ilk sayfasını bul
        var firstPage = await connection.QueryFirstOrDefaultAsync<Page>(@"
            SELECT * FROM Pages WHERE CategoryId = @CategoryId AND IsDeleted = 0 AND IsActive = 1
            ORDER BY SortOrder ASC",
            new { CategoryId = firstCategory.Id });

        if (firstPage == null)
            return View("NoContent", app);

        return RedirectToAction("Page", new { appId = app.Id, categoryId = firstCategory.Id, pageId = firstPage.Id });
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

        var accessCheck = await CheckAccessAsync(app.AccessType, "Application", app.Id);
        if (accessCheck != null) return accessCheck;

        var category = await connection.QuerySingleOrDefaultAsync<Category>(@"
            SELECT * FROM Categories WHERE Id = @Id AND ApplicationId = @AppId AND IsDeleted = 0 AND IsActive = 1",
            new { Id = categoryId, AppId = appId });

        if (category == null) return NotFound();

        // Kategorinin ilk sayfasını bul
        var firstPage = await connection.QueryFirstOrDefaultAsync<Page>(@"
            SELECT * FROM Pages WHERE CategoryId = @CategoryId AND IsDeleted = 0 AND IsActive = 1
            ORDER BY SortOrder ASC",
            new { CategoryId = category.Id });

        if (firstPage == null)
            return View("NoContent", app);

        return RedirectToAction("Page", new { appId = app.Id, categoryId = category.Id, pageId = firstPage.Id });
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

        // Sol menü için tüm kategori ve sayfaları çek
        var allCategories = (await connection.QueryAsync<Category>(@"
            SELECT * FROM Categories WHERE ApplicationId = @AppId AND IsDeleted = 0 AND IsActive = 1
            ORDER BY SortOrder ASC",
            new { AppId = appId })).AsList();

        var userId = GetCurrentUserId();
        List<int> permittedPageIds = new();
        if (userId != null)
        {
            permittedPageIds = (await connection.QueryAsync<int>(@"
                SELECT ContentId FROM ContentPermissions 
                WHERE ContentType = 'Page' AND UserId = @UserId", 
                new { UserId = userId })).AsList();
        }

        var rawAllPages = (await connection.QueryAsync<Page>(@"
            SELECT p.* FROM Pages p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            WHERE c.ApplicationId = @AppId AND p.IsDeleted = 0 AND p.IsActive = 1
            ORDER BY p.SortOrder ASC",
            new { AppId = appId })).AsList();

        var allPages = rawAllPages
            .Where(p => p.AccessType == AccessType.Public || permittedPageIds.Contains(p.Id))
            .ToList();

        var categoriesWithPages = allCategories.Select(c => new CategoryWithPagesDto
        {
            Category = c,
            Pages = allPages.Where(p => p.CategoryId == c.Id).ToList()
        }).ToList();

        return View(new DocumentationViewModel
        {
            Application = app,
            CategoriesWithPages = categoriesWithPages,
            ActiveCategory = category,
            ActivePage = page,
            ActiveAttachments = attachments
        });
    }
}
