using System;
using Kilavuz.Web.Domain.Enums;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class ContentPermission : IEntity
{
    public int Id { get; set; }
    public ContentType ContentType { get; set; }
    public int ContentId { get; set; }
    
    public int UserId { get; set; }
    public int GrantedByUserId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
