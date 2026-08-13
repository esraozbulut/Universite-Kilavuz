using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Kilavuz.Web.Application;
using Kilavuz.Web.Application.Interfaces;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;

namespace Kilavuz.Web.ViewComponents;

public class NavbarApplicationsViewComponent : ViewComponent
{
    private readonly IGenericService<AppEntity> _applicationService;

    public NavbarApplicationsViewComponent(IGenericService<AppEntity> applicationService)
    {
        _applicationService = applicationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await _applicationService.GetAllAsync();
        var apps = result.IsSuccess
            ? result.Data.Where(a => a.IsActive && !a.IsDeleted).OrderBy(a => a.SortOrder).ToList()
            : new List<AppEntity>();

        return View(apps);
    }
}
