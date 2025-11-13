using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels.AdminUsers
{
    public class AdminUserCreateVM
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string DisplayName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public bool IsAdmin { get; set; }
    }
}
