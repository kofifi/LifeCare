using LifeCare.ViewModels;

namespace LifeCare.Services.Interfaces;

public interface IHabitService
{
    Task<List<HabitVM>> GetAllHabitsAsync(string userId);
    Task<HabitVM> GetHabitByIdAsync(int habitId, string userId);
    Task CreateHabitAsync(HabitVM habitVM, string userId);
    Task UpdateHabitAsync(HabitVM habitVM, string userId);
    Task DeleteHabitAsync(int habitId, string userId);
    Task<List<HabitCategory>> GetUserCategoriesAsync(string userId);
}