using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Enums;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Policy = "YetkiliOrAbove")]
public class ApplicationController : Controller
{
    private readonly IGenericService<AppEntity> _applicationService;
    private readonly IReorderService<AppEntity> _reorderService;
    private readonly IDbConnectionFactory _connectionFactory;

    public ApplicationController(IGenericService<AppEntity> applicationService, IReorderService<AppEntity> reorderService, IDbConnectionFactory connectionFactory)
    {
        _applicationService = applicationService;
        _reorderService = reorderService;
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

    public async Task<IActionResult> Index()
    {
        var result = await _applicationService.GetAllAsync();
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Enumerable.Empty<AppEntity>());
        }

        var apps = result.Data.OrderBy(a => a.SortOrder).ToList();

        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        // Süper Admin değilse, başkasının oluşturduğu ve yetkisiz olduğu Kısıtlı uygulamaları gizle
        if (currentUserRole != UserRoleType.SuperAdmin.ToString())
        {
            List<int> permittedAppIds = new();
            using var connection = _connectionFactory.CreateConnection();
            permittedAppIds = (await connection.QueryAsync<int>(@"
                SELECT ContentId FROM ContentPermissions 
                WHERE ContentType = 'Application' AND UserId = @UserId", 
                new { UserId = currentUserId })).AsList();

            // Rev.D: Departman yetkilisi filtrelemesi
            var authorizedDeptIds = (await connection.QueryAsync<int>(@"
                SELECT DepartmentId FROM DepartmentUsers WHERE UserId = @UserId",
                new { UserId = currentUserId })).AsList();

            apps = apps.Where(a => 
                (
                    (a.DepartmentId.HasValue && authorizedDeptIds.Contains(a.DepartmentId.Value)) ||
                    (!a.DepartmentId.HasValue && a.CreatedByUserId == currentUserId)
                ) &&
                (a.AccessType == AccessType.Public || 
                 a.CreatedByUserId == currentUserId || 
                 permittedAppIds.Contains(a.Id))
            ).ToList();
        }

        // Tüm departman isimlerini alıp Dictionary olarak View'a gönderelim (Index tablosunda göstermek için)
        using var conn = _connectionFactory.CreateConnection();
        var allDepts = await conn.QueryAsync("SELECT Id, Name FROM Departments");
        ViewBag.DepartmentNames = allDepts.ToDictionary(k => (int)k.Id, v => (string)v.Name);

        return View(apps);
    }

    private async Task PrepareDepartmentDropdownAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var currentUserId = GetCurrentUserId();
        
        IEnumerable<Kilavuz.Web.Domain.Entities.Department> departments;
        
        if (User.IsInRole("SuperAdmin"))
        {
            departments = await connection.QueryAsync<Kilavuz.Web.Domain.Entities.Department>(
                "SELECT Id, Name FROM Departments WHERE IsActive = 1 AND IsDeleted = 0");
        }
        else
        {
            departments = await connection.QueryAsync<Kilavuz.Web.Domain.Entities.Department>(@"
                SELECT d.Id, d.Name 
                FROM Departments d
                INNER JOIN DepartmentUsers du ON d.Id = du.DepartmentId
                WHERE d.IsActive = 1 AND d.IsDeleted = 0 AND du.UserId = @UserId",
                new { UserId = currentUserId });
        }
        
        ViewBag.Departments = departments;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareDepartmentDropdownAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppEntity model)
    {
        if (!ModelState.IsValid)
        {
            await PrepareDepartmentDropdownAsync();
            return View(model);
        }

        if (!User.IsInRole("SuperAdmin"))
        {
            model.IsPinned = false;
        }

        var result = await _applicationService.CreateAsync(model, GetCurrentUserId(), GetCurrentUserRole());
        
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = result.Message;
        await PrepareDepartmentDropdownAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _applicationService.GetByIdAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var app = result.Data;
        if (!User.IsInRole("SuperAdmin"))
        {
            if (!app.DepartmentId.HasValue) 
            {
                if (app.CreatedByUserId != GetCurrentUserId())
                {
                    TempData["ErrorMessage"] = "Başkasının oluşturduğu ana üniversite uygulamalarını düzenleme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                using var connection = _connectionFactory.CreateConnection();
                var hasAccess = await connection.QueryFirstOrDefaultAsync<bool>(
                    "SELECT CAST(1 AS BIT) FROM DepartmentUsers WHERE DepartmentId = @DeptId AND UserId = @UserId",
                    new { DeptId = app.DepartmentId.Value, UserId = GetCurrentUserId() });
                if (!hasAccess)
                {
                     TempData["ErrorMessage"] = "Bu uygulamayı düzenleme yetkiniz yok.";
                     return RedirectToAction(nameof(Index));
                }
            }
        }

        await PrepareDepartmentDropdownAsync();
        return View(app);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AppEntity model)
    {
        if (!ModelState.IsValid)
        {
            await PrepareDepartmentDropdownAsync();
            return View(model);
        }

        var existingResult = await _applicationService.GetByIdAsync(model.Id);
        if (!existingResult.IsSuccess)
        {
            TempData["ErrorMessage"] = existingResult.Message;
            return RedirectToAction(nameof(Index));
        }

        var existing = existingResult.Data;
        
        if (!User.IsInRole("SuperAdmin"))
        {
            if (model.DepartmentId != existing.DepartmentId)
            {
                model.DepartmentId = existing.DepartmentId; // Yetkisiz değiştirme denemesi geri alınır
            }
        }
        else
        {
            existing.DepartmentId = model.DepartmentId;
        }

        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.IconPath = model.IconPath;
        existing.IsActive = model.IsActive;

        if (User.IsInRole("SuperAdmin"))
        {
            existing.IsPinned = model.IsPinned;
        }

        existing.AccessType = model.AccessType;

        // GenericService UpdateAsync içinde _policy.CanModify kontrolü zaten yapılıyor!
        var result = await _applicationService.UpdateAsync(existing, GetCurrentUserId(), GetCurrentUserRole());

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = result.Message;
        await PrepareDepartmentDropdownAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // GenericService SoftDeleteAsync içinde _policy.CanModify kontrolü zaten yapılıyor!
        var result = await _applicationService.SoftDeleteAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveUp(int id)
    {
        var result = await _applicationService.GetAllAsync();
        var apps = result.Data.OrderBy(a => a.SortOrder).ToList();

        var currentIndex = apps.FindIndex(a => a.Id == id);
        if (currentIndex > 0)
        {
            var prevApp = apps[currentIndex - 1];
            var currentApp = apps[currentIndex];

            var temp = currentApp.SortOrder;
            currentApp.SortOrder = prevApp.SortOrder;
            prevApp.SortOrder = temp;

            var orders = new System.Collections.Generic.Dictionary<int, int>
            {
                { currentApp.Id, currentApp.SortOrder },
                { prevApp.Id, prevApp.SortOrder }
            };

            var reorderResult = await _reorderService.UpdateSortOrdersAsync(orders, GetCurrentUserId(), GetCurrentUserRole());
            if (reorderResult.IsSuccess)
                TempData["SuccessMessage"] = "Sıralama güncellendi.";
            else
                TempData["ErrorMessage"] = reorderResult.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveDown(int id)
    {
        var result = await _applicationService.GetAllAsync();
        var apps = result.Data.OrderBy(a => a.SortOrder).ToList();

        var currentIndex = apps.FindIndex(a => a.Id == id);
        if (currentIndex >= 0 && currentIndex < apps.Count - 1)
        {
            var nextApp = apps[currentIndex + 1];
            var currentApp = apps[currentIndex];

            var temp = currentApp.SortOrder;
            currentApp.SortOrder = nextApp.SortOrder;
            nextApp.SortOrder = temp;

            var orders = new System.Collections.Generic.Dictionary<int, int>
            {
                { currentApp.Id, currentApp.SortOrder },
                { nextApp.Id, nextApp.SortOrder }
            };

            var reorderResult = await _reorderService.UpdateSortOrdersAsync(orders, GetCurrentUserId(), GetCurrentUserRole());
            if (reorderResult.IsSuccess)
                TempData["SuccessMessage"] = "Sıralama güncellendi.";
            else
                TempData["ErrorMessage"] = reorderResult.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
