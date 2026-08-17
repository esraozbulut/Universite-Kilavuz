using System;
using Kilavuz.Web.Domain.Enums;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class Application : IEntity, IOrderable, IAuditable, ISoftDeletable, IDepartmentOwned
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPinned { get; set; } = false;
    public AccessType AccessType { get; set; } = AccessType.Public;
    
    public int? DepartmentId { get; set; }
    
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false;
}
