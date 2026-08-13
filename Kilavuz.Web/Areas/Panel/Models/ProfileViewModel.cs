using System.ComponentModel.DataAnnotations;

namespace Kilavuz.Web.Areas.Panel.Models;

public class ProfileViewModel
{
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "E-Posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mevcut şifrenizi girmelisiniz.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre belirlemelisiniz.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifrenizi tekrar girmelisiniz.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre (Tekrar)")]
    [Compare("NewPassword", ErrorMessage = "Yeni şifre ile tekrarı uyuşmuyor.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
