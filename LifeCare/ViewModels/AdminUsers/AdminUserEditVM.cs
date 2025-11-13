using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels.AdminUsers
{
    public class AdminUserEditVM
    {
        [Required]
        public string Id { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(2)]
        public string DisplayName { get; set; } = "";

        public bool IsAdmin { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string? ConfirmNewPassword { get; set; }
    }
}