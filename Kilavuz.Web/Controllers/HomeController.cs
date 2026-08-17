using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Dapper;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Models;
using Kilavuz.Web.Domain.Enums;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;

namespace Kilavuz.Web.Controllers;

public class HomeController : Controller
{
    private readonly IGenericService<AppEntity> _applicationService;
    private readonly IDbConnectionFactory _connectionFactory;

    public HomeController(IGenericService<AppEntity> applicationService, IDbConnectionFactory connectionFactory)
    {
        _applicationService = applicationService;
        _connectionFactory = connectionFactory;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _applicationService.GetAllAsync();
        
        // Güvenlik Kuralı: Kısıtlı içerikler yetkisi olmayanlardan gizlenir
        var userId = GetCurrentUserId();
        List<int> permittedAppIds = new();
        if (userId != null)
        {
            using var connection = _connectionFactory.CreateConnection();
            permittedAppIds = (await Dapper.SqlMapper.QueryAsync<int>(connection, @"
                SELECT ContentId FROM ContentPermissions 
                WHERE ContentType = 'Application' AND UserId = @UserId", 
                new { UserId = userId })).AsList();
        }

        var allApps = result.IsSuccess
            ? result.Data.Where(a => a.IsActive && !a.IsDeleted && a.DepartmentId == null).ToList()
            : new();

        var filteredApps = allApps
            .Where(a => a.AccessType == AccessType.Public || permittedAppIds.Contains(a.Id))
            .ToList();

        var pinnedApps = filteredApps.Where(a => a.IsPinned).OrderBy(a => a.SortOrder).ToList();
        List<AppEntity> finalApps;

        if (pinnedApps.Count >= 10)
        {
            finalApps = pinnedApps;
        }
        else
        {
            var recentApps = filteredApps.Where(a => !a.IsPinned).OrderByDescending(a => a.Id).Take(10 - pinnedApps.Count).ToList();
            finalApps = pinnedApps.Concat(recentApps).ToList();
        }

        using var conn = _connectionFactory.CreateConnection();
        var departments = (await conn.QueryAsync<Kilavuz.Web.Domain.Entities.Department>(
            "SELECT Id, Name, Slug, Description, IsActive, IsDeleted FROM Departments WHERE IsActive = 1 AND IsDeleted = 0")).AsList();

        return View(new HomeViewModel { Applications = finalApps, Departments = departments });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Home/Error/{statusCode?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        ViewBag.StatusCode = statusCode;
        
        // Loglanan exception bilgisini almak için:
        // var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        // Ancak biz loglamayı GlobalExceptionHandler içinde ErrorLogs tablosuna yapıyoruz zaten.
        
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
