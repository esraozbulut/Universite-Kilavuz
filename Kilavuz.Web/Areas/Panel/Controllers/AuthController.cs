using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Areas.Panel.Models;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[AllowAnonymous]
public class AuthController : Controller
{
    private readonly IAuthenticationProvider _authProvider;
    private readonly ICaptchaProvider _captchaProvider;
    private readonly IMemoryCache _cache;
    private readonly Serilog.ILogger _logger;

    public AuthController(
        IAuthenticationProvider authProvider, 
        ICaptchaProvider captchaProvider, 
        IMemoryCache cache)
    {
        _authProvider = authProvider;
        _captchaProvider = captchaProvider;
        _cache = cache;
        _logger = Log.ForContext<AuthController>();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl);
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl,
            CaptchaKey = Guid.NewGuid().ToString("N")
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CaptchaKey = Guid.NewGuid().ToString("N");
            return View(model);
        }

        // 1. CAPTCHA Doğrulaması
        if (string.IsNullOrWhiteSpace(model.CaptchaKey) || string.IsNullOrWhiteSpace(model.CaptchaCode))
        {
            ModelState.AddModelError("CaptchaCode", "Lütfen doğrulama kodunu giriniz.");
            model.CaptchaKey = Guid.NewGuid().ToString("N");
            return View(model);
        }

        if (!_cache.TryGetValue(model.CaptchaKey, out string? cachedCaptcha) || 
            !string.Equals(cachedCaptcha, model.CaptchaCode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("Başarısız giriş denemesi: Yanlış veya süresi geçmiş CAPTCHA. Kullanıcı Adı: {Username}", model.Username);
            ModelState.AddModelError("CaptchaCode", "Doğrulama kodu hatalı veya süresi dolmuş.");
            _cache.Remove(model.CaptchaKey);
            model.CaptchaKey = Guid.NewGuid().ToString("N");
            return View(model);
        }

        _cache.Remove(model.CaptchaKey);

        // 2. Kimlik Doğrulama
        var result = await _authProvider.ValidateCredentialsAsync(model.Username, model.Password);

        if (!result.IsSuccess || result.User == null)
        {
            _logger.Warning("Başarısız giriş denemesi: Hatalı kullanıcı adı veya şifre. Kullanıcı Adı: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            model.CaptchaKey = Guid.NewGuid().ToString("N");
            return View(model);
        }

        // 3. Başarılı Giriş -> Claim oluşturma
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
            new Claim(ClaimTypes.Name, result.User.UserName)
        };

        foreach (var role in result.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);

        _logger.Information("Başarılı giriş. Kullanıcı Adı: {Username}", result.User.UserName);

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.Information("Kullanıcı çıkış yaptı. Kullanıcı Adı: {Username}", User.Identity?.Name);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult CaptchaImage(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest();

        var captchaResult = _captchaProvider.GenerateCaptcha();
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };
        
        _cache.Set(key, captchaResult.CaptchaCode, cacheOptions);

        return File(captchaResult.ImageBytes, "image/png");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home", new { area = "Panel" });
        }
    }
}
