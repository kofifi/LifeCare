namespace LifeCare.ViewModels.AdminUsers
{
    public class AdminUserDetailsVM
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsAdmin { get; set; }
    }
}