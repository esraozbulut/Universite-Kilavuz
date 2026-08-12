using System;
using System.Collections.Generic;
using Kilavuz.Web.Domain.Enums;

namespace Kilavuz.Web.Areas.Panel.Models;

/// <summary>
/// ContentPermissions yönetim ekranı için ViewModel.
/// Hem Application hem Page için aynı view kullanılır.
/// </summary>
public class ManagePermissionsViewModel
{
    public ContentType ContentType { get; set; }
    public int ContentId { get; set; }
    public string ContentName { get; set; } = string.Empty;

    /// <summary>Mevcut izinler — tabloda kayıtlı olan kullanıcılar</summary>
    public List<PermissionRow> ExistingPermissions { get; set; } = new();

    /// <summary>Tüm aktif kullanıcılar — checkbox listesi için</summary>
    public List<UserSelectRow> AllUsers { get; set; } = new();
}

/// <summary>Tabloda kayıtlı bir izin satırı</summary>
public class PermissionRow
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
}

/// <summary>Kullanıcı seçim listesi (checkbox)</summary>
public class UserSelectRow
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;

    /// <summary>Bu kullanıcıya zaten izin verilmiş mi?</summary>
    public bool IsGranted { get; set; }
}
