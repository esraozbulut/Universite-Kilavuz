using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Data;
using Kilavuz.Web.Domain.Enums;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;
using CategoryEntity = Kilavuz.Web.Domain.Entities.Category;
using PageEntity = Kilavuz.Web.Domain.Entities.Page;
using PageAttachmentEntity = Kilavuz.Web.Domain.Entities.PageAttachment;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Policy = "YetkiliOrAbove")]
public class PageController : Controller
{
    private readonly IPageService _pageService;
    private readonly IGenericService<CategoryEntity> _categoryService;
    private readonly IGenericService<AppEntity> _applicationService;
    private readonly IReorderService<PageEntity> _reorderService;
    private readonly IGenericService<PageAttachmentEntity> _attachmentService;
    private readonly IGenericRepository<PageAttachmentEntity> _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _env;
    private readonly IDbConnectionFactory _connectionFactory;

    public PageController(
        IPageService pageService,
        IGenericService<CategoryEntity> categoryService,
        IGenericService<AppEntity> applicationService,
        IReorderService<PageEntity> reorderService,
        IGenericService<PageAttachmentEntity> attachmentService,
        IGenericRepository<PageAttachmentEntity> attachmentRepository,
        IFileStorageService fileStorageService,
        IWebHostEnvironment env,
        IDbConnectionFactory connectionFactory)
    {
        _pageService = pageService;
        _categoryService = categoryService;
        _applicationService = applicationService;
        _reorderService = reorderService;
        _attachmentService = attachmentService;
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
        _env = env;
        _connectionFactory = connectionFactory;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    /// <summary>
    /// Yetkili için: Kategorinin ait olduğu Uygulama'nın sahibi mi?
    /// SuperAdmin her zaman true.
    /// </summary>
    private async Task<(bool isOwner, int applicationId, int categoryOwnerId)> GetCategoryOwnerInfoAsync(int categoryId)
    {
        var catResult = await _categoryService.GetByIdAsync(categoryId);
        if (!catResult.IsSuccess) return (false, 0, 0);

        var appResult = await _applicationService.GetByIdAsync(catResult.Data.ApplicationId);
        if (!appResult.IsSuccess) return (false, 0, 0);

        var appOwnerId = appResult.Data.CreatedByUserId;
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var isOwner = isSuperAdmin || appOwnerId == GetCurrentUserId();
        return (isOwner, catResult.Data.ApplicationId, appOwnerId);
    }

    // ─── Index ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(int categoryId)
    {
        var catResult = await _categoryService.GetByIdAsync(categoryId);
        if (!catResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Kategori bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        var appResult = await _applicationService.GetByIdAsync(catResult.Data.ApplicationId);
        if (!appResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Uygulama bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        ViewBag.CategoryId = categoryId;
        ViewBag.CategoryName = catResult.Data.Name;
        ViewBag.ApplicationId = catResult.Data.ApplicationId;
        ViewBag.ApplicationName = appResult.Data.Name;
        ViewBag.CategoryOwnerId = appResult.Data.CreatedByUserId; // Uygulama sahibi üzerinden yetki

        var result = await _pageService.GetAllAsync(new { CategoryId = categoryId });
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Enumerable.Empty<PageEntity>());
        }

        var pages = result.Data.OrderBy(p => p.SortOrder).ToList();
        return View(pages);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create(int categoryId)
    {
        var (isOwner, _, _) = await GetCategoryOwnerInfoAsync(categoryId);
        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Bu kategoriye sayfa ekleme yetkiniz yok.";
            return RedirectToAction("Index", "Application");
        }

        var model = new PageEntity { CategoryId = categoryId };
        ViewBag.CategoryId = categoryId;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PageEntity model)
    {
        var (isOwner, _, _) = await GetCategoryOwnerInfoAsync(model.CategoryId);
        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Bu kategoriye sayfa ekleme yetkiniz yok.";
            return RedirectToAction("Index", "Application");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.CategoryId = model.CategoryId;
            return View(model);
        }


        // SortOrder: CategoryId bazlı otomatik sıra
        if (model.SortOrder == 0)
        {
            var repo = HttpContext.RequestServices.GetService(typeof(IGenericRepository<PageEntity>)) as IGenericRepository<PageEntity>;
            if (repo != null)
                model.SortOrder = await repo.GetNextSortOrderAsync(new { CategoryId = model.CategoryId });
        }

        var result = await _pageService.CreateAsync(model, GetCurrentUserId(), GetCurrentUserRole());

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { categoryId = model.CategoryId });
        }

        TempData["ErrorMessage"] = result.Message;
        ViewBag.CategoryId = model.CategoryId;
        return View(model);
    }

    // ─── Edit ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _pageService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "Application");
        }

        var page = result.Data;

        // Sahiplik kontrolü (Yetkili için)
        if (User.IsInRole("Yetkili") && page.CreatedByUserId != GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Bu sayfayı düzenleme yetkiniz yok.";
            return RedirectToAction(nameof(Index), new { categoryId = page.CategoryId });
        }

        // Ek dosyaları getir
        var attachmentsResult = await _attachmentService.GetAllAsync(new { PageId = id });
        ViewBag.Attachments = attachmentsResult.IsSuccess
            ? attachmentsResult.Data.ToList()
            : new System.Collections.Generic.List<PageAttachmentEntity>();

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PageEntity model)
    {
        if (!ModelState.IsValid)
        {
            var attachmentsResult = await _attachmentService.GetAllAsync(new { PageId = model.Id });
            ViewBag.Attachments = attachmentsResult.IsSuccess
                ? attachmentsResult.Data.ToList()
                : new System.Collections.Generic.List<PageAttachmentEntity>();
            return View(model);
        }

        var existingResult = await _pageService.GetByIdAsync(model.Id);
        if (!existingResult.IsSuccess)
        {
            TempData["ErrorMessage"] = existingResult.Message;
            return RedirectToAction("Index", "Application");
        }

        var existing = existingResult.Data;
        existing.Title = model.Title;
        existing.ContentHtml = model.ContentHtml; // PageService.UpdateAsync sanitize eder
        existing.IsActive = model.IsActive;
        existing.AccessType = model.AccessType;


        // GenericService.UpdateAsync → CanModify kontrolü + HtmlSanitize
        var result = await _pageService.UpdateAsync(existing, GetCurrentUserId(), GetCurrentUserRole());

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { categoryId = existing.CategoryId });
        }

        TempData["ErrorMessage"] = result.Message;
        var attachments = await _attachmentService.GetAllAsync(new { PageId = model.Id });
        ViewBag.Attachments = attachments.IsSuccess
            ? attachments.Data.ToList()
            : new System.Collections.Generic.List<PageAttachmentEntity>();
        return View(model);
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var pageResult = await _pageService.GetByIdAsync(id);
        if (!pageResult.IsSuccess)
        {
            TempData["ErrorMessage"] = pageResult.Message;
            return RedirectToAction("Index", "Application");
        }

        var categoryId = pageResult.Data.CategoryId;

        // GenericService.SoftDeleteAsync → CanModify kontrolü
        var result = await _pageService.SoftDeleteAsync(id, GetCurrentUserId(), GetCurrentUserRole());

        if (result.IsSuccess)
            TempData["SuccessMessage"] = result.Message;
        else
            TempData["ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index), new { categoryId });
    }

    // ─── MoveUp / MoveDown ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveUp(int id)
    {
        var existingResult = await _pageService.GetByIdAsync(id);
        if (!existingResult.IsSuccess) return RedirectToAction("Index", "Application");

        var categoryId = existingResult.Data.CategoryId;
        var result = await _pageService.GetAllAsync(new { CategoryId = categoryId });
        var pages = result.Data.OrderBy(p => p.SortOrder).ToList();

        var currentIndex = pages.FindIndex(p => p.Id == id);
        if (currentIndex > 0)
        {
            var prevPage = pages[currentIndex - 1];
            var currentPage = pages[currentIndex];

            var temp = currentPage.SortOrder;
            currentPage.SortOrder = prevPage.SortOrder;
            prevPage.SortOrder = temp;

            var orders = new System.Collections.Generic.Dictionary<int, int>
            {
                { currentPage.Id, currentPage.SortOrder },
                { prevPage.Id, prevPage.SortOrder }
            };

            var reorderResult = await _reorderService.UpdateSortOrdersAsync(orders, GetCurrentUserId(), GetCurrentUserRole());
            if (reorderResult.IsSuccess)
                TempData["SuccessMessage"] = "Sıralama güncellendi.";
            else
                TempData["ErrorMessage"] = reorderResult.Message;
        }

        return RedirectToAction(nameof(Index), new { categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveDown(int id)
    {
        var existingResult = await _pageService.GetByIdAsync(id);
        if (!existingResult.IsSuccess) return RedirectToAction("Index", "Application");

        var categoryId = existingResult.Data.CategoryId;
        var result = await _pageService.GetAllAsync(new { CategoryId = categoryId });
        var pages = result.Data.OrderBy(p => p.SortOrder).ToList();

        var currentIndex = pages.FindIndex(p => p.Id == id);
        if (currentIndex >= 0 && currentIndex < pages.Count - 1)
        {
            var nextPage = pages[currentIndex + 1];
            var currentPage = pages[currentIndex];

            var temp = currentPage.SortOrder;
            currentPage.SortOrder = nextPage.SortOrder;
            nextPage.SortOrder = temp;

            var orders = new System.Collections.Generic.Dictionary<int, int>
            {
                { currentPage.Id, currentPage.SortOrder },
                { nextPage.Id, nextPage.SortOrder }
            };

            var reorderResult = await _reorderService.UpdateSortOrdersAsync(orders, GetCurrentUserId(), GetCurrentUserRole());
            if (reorderResult.IsSuccess)
                TempData["SuccessMessage"] = "Sıralama güncellendi.";
            else
                TempData["ErrorMessage"] = reorderResult.Message;
        }

        return RedirectToAction(nameof(Index), new { categoryId });
    }

    // ─── CKEditor AJAX Görsel Yükleme ──────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            var result = await _fileStorageService.UploadImageAsync(file);
            // CKEditor'un beklediği JSON format
            return Json(new { url = result.RelativePath });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { message = ex.Message } });
        }
    }

    // ─── CKEditor AJAX Sayfa İçi Dosya Yükleme (Custom Widget) ────────────────
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadContentFile(IFormFile file, int? categoryId, int? pageId)
    {
        int catIdToVerify = 0;
        
        if (pageId.HasValue)
        {
            var pageResult = await _pageService.GetByIdAsync(pageId.Value);
            if (!pageResult.IsSuccess) return BadRequest(new { error = new { message = "Sayfa bulunamadı." } });
            catIdToVerify = pageResult.Data.CategoryId;
        }
        else if (categoryId.HasValue)
        {
            catIdToVerify = categoryId.Value;
        }
        else
        {
            return BadRequest(new { error = new { message = "Kategori veya Sayfa ID gerekli." } });
        }

        var (isOwner, _, _) = await GetCategoryOwnerInfoAsync(catIdToVerify);
        if (!isOwner)
        {
            return BadRequest(new { error = new { message = "Dosya yükleme yetkiniz yok." } });
        }

        try
        {
            var result = await _fileStorageService.UploadContentFileAsync(file);
            return Json(new 
            { 
                url = result.RelativePath,
                fileName = result.OriginalFileName,
                fileType = result.ContentType
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { message = ex.Message } });
        }
    }

    // ─── Ek Dosya Yükleme ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(int pageId, IFormFile file)
    {
        var pageResult = await _pageService.GetByIdAsync(pageId);
        if (!pageResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Sayfa bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        var page = pageResult.Data;

        // Sahiplik kontrolü
        var (isOwner, _, _) = await GetCategoryOwnerInfoAsync(page.CategoryId);
        if (!isOwner && page.CreatedByUserId != GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Bu sayfaya dosya yükleme yetkiniz yok.";
            return RedirectToAction(nameof(Edit), new { id = pageId });
        }

        try
        {
            var uploadResult = await _fileStorageService.UploadAttachmentAsync(file);

            var attachment = new PageAttachmentEntity
            {
                PageId = pageId,
                FileName = uploadResult.OriginalFileName,
                StoredFileName = uploadResult.StoredFileName,
                FileSize = uploadResult.FileSize,
                ContentType = uploadResult.ContentType,
                UploadedByUserId = GetCurrentUserId(),
                UploadedAt = DateTime.UtcNow
            };

            await _attachmentRepository.InsertAsync(attachment);
            TempData["SuccessMessage"] = "Dosya başarıyla yüklendi.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = $"Dosya yükleme hatası: {ex.Message}";
        }

        return RedirectToAction(nameof(Edit), new { id = pageId });
    }

    // ─── Ek Dosya Silme ───────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        var attachResult = await _attachmentService.GetByIdAsync(attachmentId);
        if (!attachResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Ek dosya bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        var attachment = attachResult.Data;
        var pageId = attachment.PageId;

        // Sayfa sahibini kontrol et
        var pageResult = await _pageService.GetByIdAsync(pageId);
        if (pageResult.IsSuccess)
        {
            var page = pageResult.Data;
            var (isOwner, _, _) = await GetCategoryOwnerInfoAsync(page.CategoryId);
            if (!isOwner && page.CreatedByUserId != GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Bu dosyayı silme yetkiniz yok.";
                return RedirectToAction(nameof(Edit), new { id = pageId });
            }
        }

        // Fiziksel dosyayı sil
        var physicalPath = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads", "Attachments", attachment.StoredFileName);
        if (System.IO.File.Exists(physicalPath))
        {
            try { System.IO.File.Delete(physicalPath); } catch { /* Devam et */ }
        }

        // DB kaydını fiziksel sil (PageAttachment ISoftDeletable değil)
        await _attachmentRepository.DeleteAsync(attachmentId);

        TempData["SuccessMessage"] = "Dosya silindi.";
        return RedirectToAction(nameof(Edit), new { id = pageId });
    }

    // ─── Ek Dosya İndirme (Panel) ──────────────────────────────────────────────
    // Bu action YetkiliOrAbove ile korunuyor. Ek olarak Restricted sayfa kontrolü
    // de yapılıyor (Faz 7 Adım 4'te ertelenmişti, Faz 8 Adım 5'te tamamlandı).

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var attachResult = await _attachmentService.GetByIdAsync(attachmentId);
        if (!attachResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Ek dosya bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        var attachment = attachResult.Data;

        // Güvenlik: Üst sayfanın Restricted kontrolü (Kural 2.4 — sunucu tarafında doğrulama)
        var pageResult = await _pageService.GetByIdAsync(attachment.PageId);
        if (pageResult.IsSuccess)
        {
            var page = pageResult.Data;
            if (page.AccessType == AccessType.Restricted)
            {
                var currentUserId = GetCurrentUserId();
                using var connection = _connectionFactory.CreateConnection();
                var hasPermission = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(1) FROM ContentPermissions
                    WHERE ContentType = 'Page' AND ContentId = @ContentId AND UserId = @UserId",
                    new { ContentId = page.Id, UserId = currentUserId });

                if (hasPermission == 0 && GetCurrentUserRole() != "SuperAdmin")
                {
                    TempData["ErrorMessage"] = "Bu dosyaya erişim yetkiniz yok.";
                    return RedirectToAction(nameof(Edit), new { id = attachment.PageId });
                }
            }
        }

        var physicalPath = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads", "Attachments", attachment.StoredFileName);

        if (!System.IO.File.Exists(physicalPath))
        {
            TempData["ErrorMessage"] = "Dosya fiziksel konumda bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(fileBytes, attachment.ContentType, attachment.FileName);
    }
}
