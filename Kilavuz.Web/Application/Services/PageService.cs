using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Data;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Application.Services;

public class PageService : GenericService<Page>, IPageService
{
    private readonly IHtmlSanitizerService _sanitizerService;

    public PageService(
        Kilavuz.Web.Data.IGenericRepository<Page> repository, 
        Kilavuz.Web.Application.Interfaces.IResourceOwnershipPolicy<Page> policy,
        IHtmlSanitizerService sanitizerService) 
        : base(repository, policy)
    {
        _sanitizerService = sanitizerService;
    }

    public override async Task<ServiceResult<int>> CreateAsync(Page entity, int currentUserId, string currentUserRole)
    {
        // Sanitize the HTML content before saving
        entity.ContentHtml = _sanitizerService.Sanitize(entity.ContentHtml);
        
        return await base.CreateAsync(entity, currentUserId, currentUserRole);
    }

    public override async Task<ServiceResult<bool>> UpdateAsync(Page entity, int currentUserId, string currentUserRole)
    {
        // Sanitize the HTML content before updating
        entity.ContentHtml = _sanitizerService.Sanitize(entity.ContentHtml);
        
        return await base.UpdateAsync(entity, currentUserId, currentUserRole);
    }
}
