// GEÇİCİ TEST DOSYASI: Bu controller yalnızca Faz 6 kimlik doğrulama testleri için oluşturulmuştur.
// Faz 7'de gerçek Panel modülleri ve Dashboard geldiğinde bu dosya silinecektir.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize] // Korumalı
public class TestController : Controller
{
    public IActionResult Index()
    {
        return Ok($"Hoş geldiniz, {User.Identity?.Name}! Yetkili alanındasınız. Rolünüz: {string.Join(", ", User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value))}");
    }
}
