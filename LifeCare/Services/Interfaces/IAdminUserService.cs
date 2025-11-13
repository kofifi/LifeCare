using LifeCare.ViewModels.AdminUsers;

namespace LifeCare.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task<List<AdminUserListItemVM>> ListAsync();
        Task<AdminUserDetailsVM?> GetAsync(string id);
        Task<AdminUserEditVM?> GetForEditAsync(string id);
        Task<(bool ok, string? error, string? id)> CreateAsync(AdminUserCreateVM vm);
        Task<(bool ok, string? error)> UpdateAsync(AdminUserEditVM vm);
        Task<(bool ok, string? error)> DeleteAsync(string id);
    }
}