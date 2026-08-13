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

        var apps = result.IsSuccess
            ? result.Data
                .Where(a => a.IsActive && !a.IsDeleted)
                .Where(a => a.AccessType == AccessType.Public || permittedAppIds.Contains(a.Id))
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
