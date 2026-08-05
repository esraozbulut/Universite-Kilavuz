using System;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class Category : IEntity, IOrderable, IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
}
