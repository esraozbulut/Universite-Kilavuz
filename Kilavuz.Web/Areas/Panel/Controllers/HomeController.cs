using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kilavuz.Web.Areas.Panel.Controllers
{
    [Area("Panel")]
    [Authorize] // PRD gereği tüm Panel controller'ları yetki korumasında olmalı
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
