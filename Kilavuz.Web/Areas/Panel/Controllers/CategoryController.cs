using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;
using CategoryEntity = Kilavuz.Web.Domain.Entities.Category;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Policy = "YetkiliOrAbove")]
public class CategoryController : Controller
{
    private readonly IGenericService<CategoryEntity> _categoryService;
    private readonly IGenericService<AppEntity> _applicationService;
    private readonly IReorderService<CategoryEntity> _reorderService;

    public CategoryController(
        IGenericService<CategoryEntity> categoryService,
        IGenericService<AppEntity> applicationService,
        IReorderService<CategoryEntity> reorderService)
    {
        _categoryService = categoryService;
        _applicationService = applicationService;
        _reorderService = reorderService;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "";
    }

    private async Task<bool> IsAppOwnerAsync(int applicationId)
    {
        if (User.IsInRole("SuperAdmin")) return true;
        
        var appResult = await _applicationService.GetByIdAsync(applicationId);
        if (!appResult.IsSuccess) return false;
        
        return appResult.Data.CreatedByUserId == GetCurrentUserId();
    }

    public async Task<IActionResult> Index(int applicationId)
    {
        var appResult = await _applicationService.GetByIdAsync(applicationId);
        if (!appResult.IsSuccess)
        {
            TempData["ErrorMessage"] = "Uygulama bulunamadı.";
            return RedirectToAction("Index", "Application");
        }



        ViewBag.ApplicationId = applicationId;
        ViewBag.ApplicationName = appResult.Data.Name;
        ViewBag.ApplicationOwnerId = appResult.Data.CreatedByUserId;

        var result = await _categoryService.GetAllAsync(new { ApplicationId = applicationId });
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Enumerable.Empty<CategoryEntity>());
        }

        var categories = result.Data.OrderBy(c => c.SortOrder).ToList();
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int applicationId)
    {
        if (!await IsAppOwnerAsync(applicationId))
        {
            TempData["ErrorMessage"] = "Bu uygulamaya kategori ekleme yetkiniz yok.";
            return RedirectToAction("Index", "Application");
        }

        var model = new CategoryEntity { ApplicationId = applicationId };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryEntity model)
    {
        if (!await IsAppOwnerAsync(model.ApplicationId))
        {
            TempData["ErrorMessage"] = "Bu uygulamaya kategori ekleme yetkiniz yok.";
            return RedirectToAction("Index", "Application");
        }

        if (!ModelState.IsValid)
            return View(model);

        // SortOrder'i ApplicationId bazli hesaplamak icin service katmanina mudehale etmiyoruz, 
        // Service icinde SortOrder 0 ise GetNextSortOrderAsync'e parametresiz gidiyor.
        // Bu yuzden manuel atayip gonderiyoruz.
        if (model.SortOrder == 0)
        {
            var dataRepo = HttpContext.RequestServices.GetService(typeof(Kilavuz.Web.Data.IGenericRepository<CategoryEntity>)) as Kilavuz.Web.Data.IGenericRepository<CategoryEntity>;
            if (dataRepo != null)
            {
                model.SortOrder = await dataRepo.GetNextSortOrderAsync(new { ApplicationId = model.ApplicationId });
            }
        }

        var result = await _categoryService.CreateAsync(model, GetCurrentUserId(), GetCurrentUserRole());
        
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { applicationId = model.ApplicationId });
        }

        TempData["ErrorMessage"] = result.Message;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction("Index", "Application");
        }

        var category = result.Data;
        if (User.IsInRole("Yetkili") && category.CreatedByUserId != GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Bu kategoriyi düzenleme yetkiniz yok.";
            return RedirectToAction(nameof(Index), new { applicationId = category.ApplicationId });
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryEntity model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var existingResult = await _categoryService.GetByIdAsync(model.Id);
        if (!existingResult.IsSuccess)
        {
            TempData["ErrorMessage"] = existingResult.Message;
            return RedirectToAction("Index", "Application");
        }

        var existing = existingResult.Data;
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.IsActive = model.IsActive;

        // GenericService UpdateAsync içinde _policy.CanModify kontrolü zaten yapılıyor!
        var result = await _categoryService.UpdateAsync(existing, GetCurrentUserId(), GetCurrentUserRole());

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { applicationId = existing.ApplicationId });
        }

        TempData["ErrorMessage"] = result.Message;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var categoryResult = await _categoryService.GetByIdAsync(id);
        if (!categoryResult.IsSuccess)
        {
            TempData["ErrorMessage"] = categoryResult.Message;
            return RedirectToAction("Index", "Application");
        }
        
        var appId = categoryResult.Data.ApplicationId;

        // GenericService SoftDeleteAsync içinde _policy.CanModify kontrolü zaten yapılıyor!
        var result = await _categoryService.SoftDeleteAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new { applicationId = appId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveUp(int id)
    {
        var existingResult = await _categoryService.GetByIdAsync(id);
        if (!existingResult.IsSuccess) return RedirectToAction("Index", "Application");
        
        var appId = existingResult.Data.ApplicationId;

        var result = await _categoryService.GetAllAsync(new { ApplicationId = appId });
        var cats = result.Data.OrderBy(c => c.SortOrder).ToList();
        
        var currentIndex = cats.FindIndex(c => c.Id == id);
        if (currentIndex > 0)
        {
            var prevCat = cats[currentIndex - 1];
            var currentCat = cats[currentIndex];

            var temp = currentCat.SortOrder;
            currentCat.SortOrder = prevCat.SortOrder;
            prevCat.SortOrder = temp;

            var orders = new System.Collections.Generic.Dictionary<int, int>
            {
                { currentCat.Id, currentCat.SortOrder },
                { prevCat.Id, prevCat.SortOrder }
            };

            var reorderResult = await _reorderService.UpdateSortOrdersAsync(orders, GetCurrentUserId(), GetCurrentUserRole());
            if (reorderResult.IsSuccess)
                TempData["SuccessMessage"] = "Sıralama güncellendi.";
            else
                TempData["ErrorMessage"] = reorderResult.Message;
        }

        return RedirectToAction(nameof(Index), new { applicationId = appId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveDown(int id)
    {
        var existingResult = await _categoryService.GetByIdAsync(id);
        if (!existingResult.IsSuccess) return RedirectToAction("Index", "Application");
        
        var appId = existingResult.Data.ApplicationId;

        var result = await _categoryService.GetAllAsync(new { ApplicationId = appId });
        var cats = result.Data.OrderBy(c => c.SortOrder).ToList();

        var currentIndex = cats.FindIndex(c => c.Id == id);
        if (currentIndex >= 0 && currentIndex < cats.Count - 1)
        {
            var nextCat = cats[currentIndex + 1];
            var currentCat = cats[currentIndex];

            var temp = currentCat.SortOrder;
            currentCat.SortOrder = nextCat.SortOrder;
            nextCat.SortOrder = temp;

            var orders = new System.Collections.Generic.Dictionary<int, int>
            {
                { currentCat.Id, currentCat.SortOrder },
                { nextCat.Id, nextCat.SortOrder }
            };

            var reorderResult = await _reorderService.UpdateSortOrdersAsync(orders, GetCurrentUserId(), GetCurrentUserRole());
            if (reorderResult.IsSuccess)
                TempData["SuccessMessage"] = "Sıralama güncellendi.";
            else
                TempData["ErrorMessage"] = reorderResult.Message;
        }

        return RedirectToAction(nameof(Index), new { applicationId = appId });
    }
}
