using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Areas.Panel.Models;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Domain.Enums;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Policy = "YetkiliOrAbove")]
public class PermissionController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private int GetCurrentUserId()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    private bool IsSuperAdmin()
        => User.IsInRole("SuperAdmin");

    // ─── Sahiplik Kontrolü ────────────────────────────────────────────────────

    /// <summary>
    /// Verilen içeriğin sahibi mi? SuperAdmin her zaman true.
    /// ContentType'a göre Applications veya Pages tablosundan CreatedByUserId'yi çeker.
    /// </summary>
    private async Task<(bool isOwner, string contentName)> GetContentOwnerInfoAsync(
        ContentType contentType, int contentId)
    {
        using var connection = _connectionFactory.CreateConnection();

        if (IsSuperAdmin())
        {
            // SuperAdmin için sadece içerik adını döndür
            string name;
            if (contentType == ContentType.Application)
            {
                name = await connection.ExecuteScalarAsync<string>(
                    "SELECT Name FROM Applications WHERE Id = @Id AND IsDeleted = 0",
                    new { Id = contentId }) ?? "Bilinmeyen Uygulama";
            }
            else
            {
                name = await connection.ExecuteScalarAsync<string>(
                    "SELECT Title FROM Pages WHERE Id = @Id AND IsDeleted = 0",
                    new { Id = contentId }) ?? "Bilinmeyen Sayfa";
            }
            return (true, name);
        }

        // Yetkili için sahiplik kontrolü
        if (contentType == ContentType.Application)
        {
            var row = await connection.QuerySingleOrDefaultAsync<(int CreatedByUserId, string Name)>(
                "SELECT CreatedByUserId, Name FROM Applications WHERE Id = @Id AND IsDeleted = 0",
                new { Id = contentId });
            return (row.CreatedByUserId == GetCurrentUserId(), row.Name ?? string.Empty);
        }
        else // ContentType.Page
        {
            var row = await connection.QuerySingleOrDefaultAsync<(int CreatedByUserId, string Title)>(
                "SELECT CreatedByUserId, Title FROM Pages WHERE Id = @Id AND IsDeleted = 0",
                new { Id = contentId });
            return (row.CreatedByUserId == GetCurrentUserId(), row.Title ?? string.Empty);
        }
    }

    // ─── Manage ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Manage(ContentType contentType, int contentId)
    {
        var (isOwner, contentName) = await GetContentOwnerInfoAsync(contentType, contentId);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Bu içeriğin izinlerini yönetme yetkiniz yok.";
            return contentType == ContentType.Application
                ? RedirectToAction("Index", "Application")
                : RedirectToAction("Index", "Application");
        }

        using var connection = _connectionFactory.CreateConnection();

        // Mevcut izinleri getir
        var existingPermissions = (await connection.QueryAsync<PermissionRow>(@"
            SELECT cp.Id, cp.UserId, u.UserName, 
                   ISNULL(STUFF((
                       SELECT ', ' + r.Name
                       FROM UserRoles ur2
                       INNER JOIN Roles r ON ur2.RoleId = r.Id
                       WHERE ur2.UserId = cp.UserId
                       FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), '') AS Roles,
                   cp.GrantedAt
            FROM ContentPermissions cp
            INNER JOIN Users u ON cp.UserId = u.Id
            WHERE cp.ContentType = @ContentType AND cp.ContentId = @ContentId
            ORDER BY u.UserName",
            new { ContentType = contentType.ToString(), ContentId = contentId })).ToList();

        var grantedUserIds = existingPermissions.Select(p => p.UserId).ToHashSet();

        // Tüm aktif kullanıcıları getir
        var allUsers = (await connection.QueryAsync<UserSelectRow>(@"
            SELECT u.Id, u.UserName,
                   ISNULL(STUFF((
                       SELECT ', ' + r.Name
                       FROM UserRoles ur
                       INNER JOIN Roles r ON ur.RoleId = r.Id
                       WHERE ur.UserId = u.Id
                       FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), '') AS Roles
            FROM Users u
            WHERE u.IsActive = 1
            ORDER BY u.UserName")).ToList();

        // IsGranted işaretle
        foreach (var user in allUsers)
        {
            user.IsGranted = grantedUserIds.Contains(user.Id);
        }

        var model = new ManagePermissionsViewModel
        {
            ContentType = contentType,
            ContentId = contentId,
            ContentName = contentName,
            ExistingPermissions = existingPermissions,
            AllUsers = allUsers
        };

        return View(model);
    }

    // ─── Grant (toplu izin atama) ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grant(ContentType contentType, int contentId, int[]? userIds)
    {
        var (isOwner, _) = await GetContentOwnerInfoAsync(contentType, contentId);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Bu içeriğe izin atama yetkiniz yok.";
            return contentType == ContentType.Application
                ? RedirectToAction("Index", "Application")
                : RedirectToAction("Index", "Application");
        }

        using var connection = _connectionFactory.CreateConnection();

        // 1. Mevcut tüm izinleri temizle
        await connection.ExecuteAsync(
            "DELETE FROM ContentPermissions WHERE ContentType = @ContentType AND ContentId = @ContentId",
            new { ContentType = contentType.ToString(), ContentId = contentId });

        // 2. Seçilen kullanıcılara yeni izin kayıtları ekle
        if (userIds != null && userIds.Length > 0)
        {
            // Kullanıcı ID'lerinin gerçekten Users tablosunda olduğunu doğrula (güvenlik)
            var validUserIds = (await connection.QueryAsync<int>(
                "SELECT Id FROM Users WHERE Id IN @Ids AND IsActive = 1",
                new { Ids = userIds })).ToHashSet();

            foreach (var userId in userIds.Where(id => validUserIds.Contains(id)))
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO ContentPermissions (ContentType, ContentId, UserId, GrantedByUserId, GrantedAt)
                    VALUES (@ContentType, @ContentId, @UserId, @GrantedByUserId, @GrantedAt)",
                    new
                    {
                        ContentType = contentType.ToString(),
                        ContentId = contentId,
                        UserId = userId,
                        GrantedByUserId = GetCurrentUserId(),
                        GrantedAt = DateTime.UtcNow
                    });
            }

            TempData["SuccessMessage"] = $"{validUserIds.Count} kullanıcıya erişim izni verildi.";
        }
        else
        {
            TempData["SuccessMessage"] = "Tüm izinler kaldırıldı.";
        }

        return RedirectToAction(nameof(Manage), new { contentType, contentId });
    }

    // ─── Revoke (tek izin kaldırma) ───────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(int permissionId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // İzni getir
        var permission = await connection.QuerySingleOrDefaultAsync<ContentPermission>(
            "SELECT * FROM ContentPermissions WHERE Id = @Id",
            new { Id = permissionId });

        if (permission == null)
        {
            TempData["ErrorMessage"] = "İzin kaydı bulunamadı.";
            return RedirectToAction("Index", "Application");
        }

        // Sahiplik kontrolü
        var (isOwner, _) = await GetContentOwnerInfoAsync(permission.ContentType, permission.ContentId);

        if (!isOwner)
        {
            TempData["ErrorMessage"] = "Bu izni kaldırma yetkiniz yok.";
            return RedirectToAction(nameof(Manage),
                new { contentType = permission.ContentType, contentId = permission.ContentId });
        }

        // Fiziksel sil
        await connection.ExecuteAsync(
            "DELETE FROM ContentPermissions WHERE Id = @Id",
            new { Id = permissionId });

        TempData["SuccessMessage"] = "Erişim izni kaldırıldı.";
        return RedirectToAction(nameof(Manage),
            new { contentType = permission.ContentType, contentId = permission.ContentId });
    }
}
