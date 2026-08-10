using System.ComponentModel.DataAnnotations;

namespace Kilavuz.Web.Areas.Panel.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğrulama kodunu giriniz.")]
    public string CaptchaCode { get; set; } = string.Empty;

    public string CaptchaKey { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
