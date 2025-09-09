using LifeCare.ViewModels;

public interface IHabitService
{
    Task<List<HabitVM>> GetAllHabitsAsync(string userId, List<int>? tagIds = null);
    Task<HabitVM> GetHabitByIdAsync(int habitId, string userId);
    Task CreateHabitAsync(HabitVM habitVm, string userId);
    Task UpdateHabitAsync(HabitVM habitVm, string userId);
    Task DeleteHabitAsync(int habitId, string userId);
    Task UpdateHabitOrderAsync(List<int> orderedHabitIds, string userId);
    Task<List<HabitEntryVM>> GetEntriesForDateAsync(DateTime date, string? userId);
    Task<bool> SaveHabitEntryAsync(HabitEntryVM entryVm, string? userId);
    Task<IReadOnlyList<HabitEntry>> GetHabitEntriesAsync(int habitId, string userId, DateTime from, DateTime to);
    Task<(double OverallPercent, int CurrentStreak, int BestStreak, int Total, int Completed, int Skipped, int Partial, DateTime StartDateUtc)>
        GetHabitStatsAsync(int habitId, string userId);

    Task<List<TagVM>> GetUserTagsAsync(string userId);
}