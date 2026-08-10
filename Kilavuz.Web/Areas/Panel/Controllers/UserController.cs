using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Areas.Panel.Models;

namespace Kilavuz.Web.Areas.Panel.Controllers;

[Area("Panel")]
[Authorize(Policy = "SuperAdminOnly")]
public class UserController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IActionResult> Index()
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var query = @"
            SELECT 
                u.Id, 
                u.UserName, 
                u.IsActive,
                ISNULL(STUFF((
                    SELECT ', ' + r.Name
                    FROM UserRoles ur
                    INNER JOIN Roles r ON ur.RoleId = r.Id
                    WHERE ur.UserId = u.Id
                    FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), '') AS Roles
            FROM Users u
            ORDER BY u.UserName";

        var users = await connection.QueryAsync<UserListViewModel>(query);
        
        return View(users);
    }
}
