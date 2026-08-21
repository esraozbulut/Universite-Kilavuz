using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Areas.Panel.Models;
using Kilavuz.Web.Data;
using Dapper;
using System.Linq;
using System.Security.Claims;
using System;
using Kilavuz.Web.Application;

namespace Kilavuz.Web.Areas.Panel.Controllers
{
    [Area("Panel")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class DepartmentController : Controller
    {
        private readonly IGenericService<Department> _departmentService;
        private readonly IDbConnectionFactory _db;

        public DepartmentController(IGenericService<Department> departmentService, IDbConnectionFactory db)
        {
            _departmentService = departmentService;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _departmentService.GetAllAsync();
            var departments = result.Data?.Where(d => !d.IsDeleted).ToList() ?? new List<Department>();

            using var connection = _db.CreateConnection();
            var userCounts = await connection.QueryAsync<dynamic>("SELECT DepartmentId, COUNT(UserId) AS UserCount FROM DepartmentUsers GROUP BY DepartmentId");
            
            ViewBag.UserCounts = userCounts.ToDictionary(k => (int)k.DepartmentId, v => (int)v.UserCount);

            return View(departments);
        }

        public IActionResult Create()
        {
            return View(new DepartmentCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var connection = _db.CreateConnection();
            var exists = await connection.QueryFirstOrDefaultAsync<bool>("SELECT CAST(1 AS BIT) FROM Departments WHERE Slug = @Slug", new { Slug = model.Slug });
            if (exists)
            {
                ModelState.AddModelError("Slug", "Bu slug zaten kullanımda.");
                return View(model);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);
            
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            var dept = new Department
            {
                Name = model.Name,
                Slug = model.Slug,
                Description = model.Description,
                IsActive = true
            };

            var result = await _departmentService.CreateAsync(dept, userId, userRole);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Departman başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = result.Message;
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var result = await _departmentService.GetByIdAsync(id);
            if (!result.IsSuccess || result.Data == null || result.Data.IsDeleted)
            {
                TempData["ErrorMessage"] = "Departman bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var dept = result.Data;
            var model = new DepartmentEditViewModel
            {
                Id = dept.Id,
                Name = dept.Name,
                Slug = dept.Slug,
                Description = dept.Description,
                IsActive = dept.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var connection = _db.CreateConnection();
            var exists = await connection.QueryFirstOrDefaultAsync<bool>("SELECT CAST(1 AS BIT) FROM Departments WHERE Slug = @Slug AND Id != @Id", new { Slug = model.Slug, Id = model.Id });
            if (exists)
            {
                ModelState.AddModelError("Slug", "Bu slug zaten kullanımda.");
                return View(model);
            }

            var result = await _departmentService.GetByIdAsync(model.Id);
            if (!result.IsSuccess || result.Data == null || result.Data.IsDeleted)
            {
                TempData["ErrorMessage"] = "Departman bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var dept = result.Data;
            dept.Name = model.Name;
            dept.Slug = model.Slug;
            dept.Description = model.Description;
            dept.IsActive = model.IsActive;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            var updateResult = await _departmentService.UpdateAsync(dept, userId, userRole);
            if (updateResult.IsSuccess)
            {
                TempData["SuccessMessage"] = "Departman güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = updateResult.Message;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int userId);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            var result = await _departmentService.SoftDeleteAsync(id, userId, userRole);
            if (result.IsSuccess)
                TempData["SuccessMessage"] = "Departman silindi (pasife çekildi).";
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Users(int id)
        {
            var result = await _departmentService.GetByIdAsync(id);
            if (!result.IsSuccess || result.Data == null || result.Data.IsDeleted)
            {
                TempData["ErrorMessage"] = "Departman bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            using var connection = _db.CreateConnection();
            
            // Get all active users
            var allUsers = await connection.QueryAsync<User>("SELECT * FROM Users WHERE IsActive = 1");
            
            // Get users assigned to this department
            var assignedUserIds = await connection.QueryAsync<int>("SELECT UserId FROM DepartmentUsers WHERE DepartmentId = @DeptId", new { DeptId = id });

            var assignedUsers = allUsers.Where(u => assignedUserIds.Contains(u.Id)).ToList();
            var availableUsers = allUsers.Where(u => !assignedUserIds.Contains(u.Id)).ToList();

            var model = new DepartmentUsersViewModel
            {
                Department = result.Data,
                AssignedUsers = assignedUsers,
                AvailableUsers = availableUsers
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUser(int departmentId, int[] userIds)
        {
            if (userIds == null || userIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen en az bir kullanıcı seçin.";
                return RedirectToAction(nameof(Users), new { id = departmentId });
            }

            using var connection = _db.CreateConnection();
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(currentUserIdStr, out int currentUserId);

            int addedCount = 0;
            foreach (var userId in userIds)
            {
                var exists = await connection.QueryFirstOrDefaultAsync<bool>("SELECT CAST(1 AS BIT) FROM DepartmentUsers WHERE DepartmentId = @DeptId AND UserId = @UserId", new { DeptId = departmentId, UserId = userId });
                if (!exists)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO DepartmentUsers (DepartmentId, UserId, AssignedByUserId, AssignedAt) 
                        VALUES (@DeptId, @UserId, @AssignedBy, @AssignedAt)", 
                        new { DeptId = departmentId, UserId = userId, AssignedBy = currentUserId, AssignedAt = DateTime.UtcNow });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                TempData["SuccessMessage"] = $"{addedCount} kullanıcı departmana başarıyla atandı.";
            }
            else
            {
                TempData["ErrorMessage"] = "Seçilen kullanıcı(lar) zaten bu departmana atanmış.";
            }
            
            return RedirectToAction(nameof(Users), new { id = departmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUser(int departmentId, int userId)
        {
            using var connection = _db.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM DepartmentUsers WHERE DepartmentId = @DeptId AND UserId = @UserId", 
                new { DeptId = departmentId, UserId = userId });

            TempData["SuccessMessage"] = "Kullanıcı departmandan çıkarıldı.";
            return RedirectToAction(nameof(Users), new { id = departmentId });
        }
    }
}
