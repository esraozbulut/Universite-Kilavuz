using System.ComponentModel.DataAnnotations;
using Kilavuz.Web.Domain.Entities;
using System.Collections.Generic;

namespace Kilavuz.Web.Areas.Panel.Models
{
    public class DepartmentCreateViewModel
    {
        [Required(ErrorMessage = "Departman adı zorunludur.")]
        [StringLength(255)]
        [Display(Name = "Departman Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL Slug zorunludur.")]
        [StringLength(255)]
        [Display(Name = "URL Slug")]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug sadece küçük harf, rakam ve tire içerebilir.")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
    }

    public class DepartmentEditViewModel : DepartmentCreateViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; }
    }

    public class DepartmentUsersViewModel
    {
        public Department Department { get; set; } = null!;
        public List<User> AssignedUsers { get; set; } = new();
        public List<User> AvailableUsers { get; set; } = new();
    }
}
