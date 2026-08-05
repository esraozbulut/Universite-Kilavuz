using System;
using Kilavuz.Web.Domain.Enums;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class Page : IEntity, IOrderable, IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContentHtml { get; set; }
    public string? CoverImagePath { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public AccessType AccessType { get; set; } = AccessType.Public;
    
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
}
