using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Areas.Panel.Models;
using Kilavuz.Web.Domain.Entities;
using Dapper;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Roles = "SuperAdmin")]
public class UserController : Controller
{
    private readonly IGenericService<User> _userService;
    private readonly IGenericService<Role> _roleService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserController(
        IGenericService<User> userService,
        IGenericService<Role> roleService,
        IDbConnectionFactory connectionFactory)
    {
        _userService = userService;
        _roleService = roleService;
        _connectionFactory = connectionFactory;
        _passwordHasher = new PasswordHasher<User>();
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
        var result = await _userService.GetAllAsync();
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Enumerable.Empty<UserViewModel>());
        }

        using var connection = _connectionFactory.CreateConnection();
        var allUserRoles = await connection.QueryAsync<dynamic>(@"
            SELECT ur.UserId, r.Name 
            FROM UserRoles ur
            INNER JOIN Roles r ON ur.RoleId = r.Id");

        var users = result.Data.OrderByDescending(u => u.CreatedAt).Select(u => new UserViewModel
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            IsActive = u.IsActive,
            Roles = allUserRoles.Where(x => x.UserId == u.Id).Select(x => (string)x.Name).ToList()
        }).ToList();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UserCreateViewModel();
        await PopulateRolesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(model);
            return View(model);
        }

        var allUsers = await _userService.GetAllAsync();
        if (allUsers.IsSuccess && allUsers.Data.Any(u => u.UserName.Equals(model.UserName, StringComparison.OrdinalIgnoreCase) || u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("", "Bu kullanıcı adı veya e-posta adresi zaten kullanımda.");
            await PopulateRolesAsync(model);
            return View(model);
        }

        var newUser = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        // Şifre Hashleme
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

        var result = await _userService.CreateAsync(newUser, GetCurrentUserId(), GetCurrentUserRole());
        if (result.IsSuccess)
        {
            // Rolleri Kaydet
            using var connection = _connectionFactory.CreateConnection();
            foreach (var roleId in model.SelectedRoleIds)
            {
                await connection.ExecuteAsync("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", 
                    new { UserId = result.Data, RoleId = roleId });
            }

            TempData["SuccessMessage"] = "Kullanıcı başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.Message);
        await PopulateRolesAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userResult = await _userService.GetByIdAsync(id);
        if (!userResult.IsSuccess || userResult.Data == null)
        {
            return NotFound();
        }

        using var connection = _connectionFactory.CreateConnection();
        var userRoleIds = (await connection.QueryAsync<int>("SELECT RoleId FROM UserRoles WHERE UserId = @UserId", new { UserId = id })).ToList();

        var model = new UserEditViewModel
        {
            Id = userResult.Data.Id,
            UserName = userResult.Data.UserName,
            Email = userResult.Data.Email,
            IsActive = userResult.Data.IsActive,
            SelectedRoleIds = userRoleIds
        };

        var rolesResult = await _roleService.GetAllAsync();
        if (rolesResult.IsSuccess)
        {
            model.AvailableRoles = rolesResult.Data.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var rolesResult = await _roleService.GetAllAsync();
            if (rolesResult.IsSuccess)
            {
                model.AvailableRoles = rolesResult.Data.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                }).ToList();
            }
            return View(model);
        }

        var userResult = await _userService.GetByIdAsync(model.Id);
        if (!userResult.IsSuccess || userResult.Data == null)
        {
            return NotFound();
        }

        var allUsers = await _userService.GetAllAsync();
        if (allUsers.IsSuccess && allUsers.Data.Any(u => u.Id != model.Id && (u.UserName.Equals(model.UserName, StringComparison.OrdinalIgnoreCase) || u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase))))
        {
            ModelState.AddModelError("", "Bu kullanıcı adı veya e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
            var rolesResult = await _roleService.GetAllAsync();
            if (rolesResult.IsSuccess)
            {
                model.AvailableRoles = rolesResult.Data.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                }).ToList();
            }
            return View(model);
        }

        var user = userResult.Data;
        user.UserName = model.UserName;
        user.Email = model.Email;
        user.IsActive = model.IsActive;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        var updateResult = await _userService.UpdateAsync(user, GetCurrentUserId(), GetCurrentUserRole());
        if (updateResult.IsSuccess)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM UserRoles WHERE UserId = @UserId", new { UserId = user.Id });
            
            foreach (var roleId in model.SelectedRoleIds)
            {
                await connection.ExecuteAsync("INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", 
                    new { UserId = user.Id, RoleId = roleId });
            }

            TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", updateResult.Message);
        var rr = await _roleService.GetAllAsync();
        if (rr.IsSuccess)
        {
            model.AvailableRoles = rr.Data.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var userResult = await _userService.GetByIdAsync(id);
        if (!userResult.IsSuccess || userResult.Data == null)
        {
            return NotFound();
        }

        if (userResult.Data.Id == GetCurrentUserId())
        {
            TempData["ErrorMessage"] = "Kendi hesabınızı pasifleştiremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        var user = userResult.Data;
        user.IsActive = !user.IsActive;
        await _userService.UpdateAsync(user, GetCurrentUserId(), GetCurrentUserRole());

        TempData["SuccessMessage"] = "Kullanıcı durumu güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateRolesAsync(UserCreateViewModel model)
    {
        var rolesResult = await _roleService.GetAllAsync();
        if (rolesResult.IsSuccess)
        {
            model.AvailableRoles = rolesResult.Data.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();
        }
    }
}
