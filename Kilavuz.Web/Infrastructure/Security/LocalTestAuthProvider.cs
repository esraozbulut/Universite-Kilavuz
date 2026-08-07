using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Data;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Infrastructure.Security;

public class LocalTestAuthProvider : IAuthenticationProvider
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly PasswordHasher<User> _passwordHasher;
    private const string GenericErrorMessage = "Kullanıcı adı veya şifre hatalı.";

    public LocalTestAuthProvider(IGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<AuthenticationResult> ValidateCredentialsAsync(string username, string password)
    {
        // 1. Fetch user by username (case-insensitive)
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => string.Equals(u.UserName, username, System.StringComparison.OrdinalIgnoreCase));

        // 2. Check if user exists
        if (user == null)
        {
            return new AuthenticationResult { IsSuccess = false, Message = GenericErrorMessage };
        }

        // 3. Check password
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return new AuthenticationResult { IsSuccess = false, Message = GenericErrorMessage };
        }

        // 4. Check if active
        if (!user.IsActive)
        {
            // Same generic message to prevent enumeration
            return new AuthenticationResult { IsSuccess = false, Message = GenericErrorMessage };
        }

        // 5. Success
        return new AuthenticationResult 
        { 
            IsSuccess = true, 
            Message = "Giriş başarılı.", 
            User = user 
        };
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var users = await _userRepository.GetAllAsync();
        return users.FirstOrDefault(u => string.Equals(u.UserName, username, System.StringComparison.OrdinalIgnoreCase));
    }
}
