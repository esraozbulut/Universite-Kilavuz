using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Kilavuz.Web.Application;
using Kilavuz.Web.Areas.Panel.Models;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize]
public class ProfileController : Controller
{
    private readonly IGenericService<User> _userService;
    private readonly PasswordHasher<User> _passwordHasher;

    public ProfileController(IGenericService<User> userService)
    {
        _userService = userService;
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var userResult = await _userService.GetByIdAsync(userId);

        if (!userResult.IsSuccess || userResult.Data == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        var model = new ProfileViewModel
        {
            UserName = userResult.Data.UserName,
            Email = userResult.Data.Email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var userId = GetCurrentUserId();
        var userResult = await _userService.GetByIdAsync(userId);

        if (!userResult.IsSuccess || userResult.Data == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        // Modeli geri döndürürken salt okunur alanları tekrar doldur
        model.UserName = userResult.Data.UserName;
        model.Email = userResult.Data.Email;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = userResult.Data;

        // Mevcut şifreyi doğrula
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("CurrentPassword", "Mevcut şifrenizi yanlış girdiniz.");
            return View(model);
        }

        // Yeni şifreyi hash'le ve kaydet
        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        
        var updateResult = await _userService.UpdateAsync(user, userId, GetCurrentUserRole());
        
        if (updateResult.IsSuccess)
        {
            TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        else
        {
            ModelState.AddModelError("", "Şifre güncellenirken bir hata oluştu: " + updateResult.Message);
            return View(model);
        }
    }
}
