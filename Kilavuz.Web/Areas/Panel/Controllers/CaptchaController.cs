using Kilavuz.Web.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[AllowAnonymous] // CAPTCHA login ekranında olduğu için herkese açık olmalıdır.
public class CaptchaController : Controller
{
    private readonly ICaptchaProvider _captchaProvider;

    public CaptchaController(ICaptchaProvider captchaProvider)
    {
        _captchaProvider = captchaProvider;
    }

    [HttpGet("Panel/Captcha/GetImage")]
    public IActionResult GetImage()
    {
        // 1. Yeni CAPTCHA üret
        var result = _captchaProvider.GenerateCaptcha();

        // 2. Doğru sonucu Session'a kaydet
        HttpContext.Session.SetString("CaptchaCode", result.CaptchaCode);

        // 3. Resmi client'a gönder (cache-control ekleyerek tarayıcının önbelleğe almasını engelle)
        Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");

        return File(result.ImageBytes, "image/png");
    }
}
