using System;
using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class PageAttachment : IEntity
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
