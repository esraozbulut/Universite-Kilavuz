using System.Collections.Generic;
using Kilavuz.Web.Domain.Entities;
using AppEntity = Kilavuz.Web.Domain.Entities.Application;

namespace Kilavuz.Web.Models;

public class ApplicationDetailViewModel
{
    public AppEntity Application { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
}

public class CategoryDetailViewModel
{
    public AppEntity Application { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public List<Page> Pages { get; set; } = new();
}

public class PageDetailViewModel
{
    public AppEntity Application { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Page Page { get; set; } = null!;
    public List<PageAttachment> Attachments { get; set; } = new();
}
