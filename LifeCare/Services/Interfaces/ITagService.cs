using LifeCare.ViewModels;

namespace LifeCare.Services.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagVM>> GetUserTagsAsync(string userId);
    Task<TagVM> CreateTagAsync(string name, string userId);
    Task<bool> UpdateTagAsync(int id, string name, string userId);
    Task<bool> DeleteTagAsync(int id, string userId);
    Task<TagVM?> GetByIdAsync(int id, string userId);
}