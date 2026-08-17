using Dapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Models;

namespace Kilavuz.Web.Controllers;

public class SearchController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SearchController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var model = new SearchViewModel { Query = q ?? string.Empty };

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return View(model);

        var searchTerm = $"%{q}%";
        var userId = GetCurrentUserId();
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        using var connection = _connectionFactory.CreateConnection();

        // Güvenlik (Kural 2.1): Parametreli LIKE sorgusu — SQL injection'a karşı
        // PRD 6.5 + GEMINI.md "en kısıtlayıcı yorum": Restricted içerikler tamamen gizlenir.
        // Kullanıcının izni olmadığı Restricted içerik arama sonuçlarında BİLE görünmez.

        var rawResults = (await connection.QueryAsync<dynamic>(@"
            -- Uygulamalar (Public olanlar + Restricted ama kullanıcının izni olanlar)
            SELECT 'Application' AS ResultType,
                   a.Id,
                   a.Name AS Title,
                   COALESCE(a.Description, '') AS Snippet,
                   COALESCE(d.Name, 'Üniversite Kılavuzu') AS ParentName,
                   a.Id AS AppId,
                   NULL AS CategoryId
            FROM Applications a
            LEFT JOIN Departments d ON d.Id = a.DepartmentId
            WHERE a.IsDeleted = 0 AND a.IsActive = 1
              AND (a.DepartmentId IS NULL OR (d.IsDeleted = 0 AND d.IsActive = 1))
              AND (a.Name LIKE @Q OR a.Description LIKE @Q)
              AND (
                    a.AccessType = 'Public'
                    OR (
                        a.AccessType = 'Restricted'
                        AND @UserId IS NOT NULL
                        AND EXISTS (
                            SELECT 1 FROM ContentPermissions cp
                            WHERE cp.ContentType = 'Application'
                              AND cp.ContentId = a.Id
                              AND cp.UserId = @UserId
                        )
                    )
              )

            UNION ALL

            -- Kategoriler (üst uygulaması Public ya da kullanıcının izni var)
            SELECT 'Category' AS ResultType,
                   c.Id,
                   c.Name AS Title,
                   COALESCE(c.Description, '') AS Snippet,
                   a.Name + ' (' + COALESCE(d.Name, 'Üniversite Kılavuzu') + ')' AS ParentName,
                   a.Id AS AppId,
                   NULL AS CategoryId
            FROM Categories c
            JOIN Applications a ON a.Id = c.ApplicationId
            LEFT JOIN Departments d ON d.Id = a.DepartmentId
            WHERE c.IsDeleted = 0 AND c.IsActive = 1
              AND (a.DepartmentId IS NULL OR (d.IsDeleted = 0 AND d.IsActive = 1))
              AND (c.Name LIKE @Q OR c.Description LIKE @Q)
              AND a.IsDeleted = 0 AND a.IsActive = 1
              AND (
                    a.AccessType = 'Public'
                    OR (
                        a.AccessType = 'Restricted'
                        AND @UserId IS NOT NULL
                        AND EXISTS (
                            SELECT 1 FROM ContentPermissions cp
                            WHERE cp.ContentType = 'Application'
                              AND cp.ContentId = a.Id
                              AND cp.UserId = @UserId
                        )
                    )
              )

            UNION ALL

            -- Sayfalar (hem üst uygulama hem de sayfa Restricted kontrol edilir)
            SELECT 'Page' AS ResultType,
                   p.Id,
                   p.Title AS Title,
                   LEFT(COALESCE(p.ContentHtml, ''), 200) AS Snippet,
                   c.Name + ' - ' + a.Name + ' (' + COALESCE(d.Name, 'Üniversite Kılavuzu') + ')' AS ParentName,
                   a.Id AS AppId,
                   c.Id AS CategoryId
            FROM Pages p
            JOIN Categories c ON c.Id = p.CategoryId
            JOIN Applications a ON a.Id = c.ApplicationId
            LEFT JOIN Departments d ON d.Id = a.DepartmentId
            WHERE p.IsDeleted = 0 AND p.IsActive = 1
              AND (a.DepartmentId IS NULL OR (d.IsDeleted = 0 AND d.IsActive = 1))
              AND (p.Title LIKE @Q OR p.ContentHtml LIKE @Q)
              AND a.IsDeleted = 0 AND a.IsActive = 1
              AND c.IsDeleted = 0 AND c.IsActive = 1
              -- Üst uygulama kontrolü
              AND (
                    a.AccessType = 'Public'
                    OR (
                        a.AccessType = 'Restricted'
                        AND @UserId IS NOT NULL
                        AND EXISTS (
                            SELECT 1 FROM ContentPermissions cp
                            WHERE cp.ContentType = 'Application'
                              AND cp.ContentId = a.Id
                              AND cp.UserId = @UserId
                        )
                    )
              )
              -- Sayfa kendi kontrolü (uygulama izninden bağımsız)
              AND (
                    p.AccessType = 'Public'
                    OR (
                        p.AccessType = 'Restricted'
                        AND @UserId IS NOT NULL
                        AND EXISTS (
                            SELECT 1 FROM ContentPermissions cp
                            WHERE cp.ContentType = 'Page'
                              AND cp.ContentId = p.Id
                              AND cp.UserId = @UserId
                        )
                    )
              )

            ORDER BY ResultType, Title",
            new { Q = searchTerm, UserId = (object?)userId ?? DBNull.Value }))
        .ToList();

        model.Results = rawResults.Select(r => new SearchResultItem
        {
            ResultType  = (string)r.ResultType,
            Id          = (int)r.Id,
            Title       = (string)r.Title,
            Snippet     = r.Snippet as string,
            ParentName  = r.ParentName as string,
            AppId       = r.AppId as int?,
            CategoryId  = r.CategoryId as int?
        }).ToList();

        return View(model);
    }
}
