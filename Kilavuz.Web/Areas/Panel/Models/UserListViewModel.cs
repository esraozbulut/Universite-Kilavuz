namespace Kilavuz.Web.Areas.Panel.Models;

public class UserListViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Roles { get; set; } = string.Empty;
}
