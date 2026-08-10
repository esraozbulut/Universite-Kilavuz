using System.Threading.Tasks;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Application.Interfaces;

public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public System.Collections.Generic.IEnumerable<string> Roles { get; set; } = System.Array.Empty<string>();
}

public interface IAuthenticationProvider
{
    Task<AuthenticationResult> ValidateCredentialsAsync(string username, string password);
    Task<User?> GetUserByUsernameAsync(string username);
}
