using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Kilavuz.Web.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kilavuz.Web.Areas.Panel.Models;

public class UserViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Kullanıcı Adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir E-Posta adresi giriniz.")]
    [Display(Name = "E-Posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Durum (Aktif/Pasif)")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Lütfen en az bir rol seçiniz.")]
    [Display(Name = "Kullanıcı Rolü")]
    public List<int> SelectedRoleIds { get; set; } = new List<int>();

    public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kullanıcı Adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir E-Posta adresi giriniz.")]
    [Display(Name = "E-Posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre (Değiştirmek istemiyorsanız boş bırakın)")]
    public string? Password { get; set; }

    [Display(Name = "Durum (Aktif/Pasif)")]
    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Lütfen en az bir rol seçiniz.")]
    [Display(Name = "Kullanıcı Rolü")]
    public List<int> SelectedRoleIds { get; set; } = new List<int>();

    public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
}
