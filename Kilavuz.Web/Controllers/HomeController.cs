using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Models;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;

namespace Kilavuz.Web.Controllers;

public class HomeController : Controller
{
    private readonly IGenericService<AppEntity> _applicationService;

    public HomeController(IGenericService<AppEntity> applicationService)
    {
        _applicationService = applicationService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _applicationService.GetAllAsync();
        var apps = result.IsSuccess
            ? result.Data
                .Where(a => a.IsActive && !a.IsDeleted)
                .OrderBy(a => a.SortOrder)
                .ToList()
            : new();

        return View(new HomeViewModel { Applications = apps });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
