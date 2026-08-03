using System;
using System.Collections.Generic;

namespace Kilavuz.Web.Domain
{
    public class Role : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class User : IEntity
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }

    public class Application : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string AccessType { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class Category : IEntity
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class Page : IEntity
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string ContentHtml { get; set; }
        public string CoverImagePath { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string AccessType { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class PageAttachment : IEntity
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string FileName { get; set; }
        public string StoredFileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class ContentPermission : IEntity
    {
        public int Id { get; set; }
        public string ContentType { get; set; } // 'Application' or 'Page'
        public int ContentId { get; set; }
        public int UserId { get; set; }
        public int GrantedByUserId { get; set; }
        public DateTime GrantedAt { get; set; }
    }

    public class ErrorLog : IEntity
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string RequestPath { get; set; }
        public string IPAddress { get; set; }
    }

    public class LoginAttempt : IEntity
    {
        public int Id { get; set; }
        public string UserNameAttempted { get; set; }
        public string IPAddress { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime AttemptedAt { get; set; }
    }
}
