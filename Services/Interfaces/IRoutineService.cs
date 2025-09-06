using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LifeCare.Models;
using LifeCare.ViewModels;

namespace LifeCare.Services.Interfaces
{
    public interface IRoutineService
    {
        Task<List<RoutineVM>> GetAllRoutinesAsync(string userId);
        Task<RoutineVM?> GetRoutineAsync(int id, string userId);
        Task<int> CreateRoutineAsync(RoutineVM vm, string userId);
        Task UpdateRoutineAsync(RoutineVM vm, string userId);
        Task DeleteRoutineAsync(int id, string userId);

        Task UpdateStepOrderAsync(int routineId, List<int> orderedStepIds, string userId);

        Task<List<RoutineForDayVM>> GetForDateAsync(DateOnly date, string userId);

        Task<bool> ToggleStepAsync(int routineId, int stepId, DateOnly date, bool completed, string? note,
            string userId);

        Task<bool> MarkAllStepsAsync(int routineId, DateOnly date, string userId);

        Task<List<Category>> GetUserCategoriesAsync(string userId);

        Task<bool> MarkRoutineCompletedAsync(int routineId, DateOnly date, string userId);
        Task<bool> SetAllStepsAsync(int routineId, DateOnly date, bool completed, string userId);
        Task<bool> SetRoutineCompletedAsync(int routineId, DateOnly date, bool completed, string userId);

        Task<bool> ToggleStepProductAsync(int routineId, int stepId, int productId, DateOnly date, bool completed, string userId);

        Task<RoutineStatsVM> GetRoutineStatsAsync(int routineId, string userId);
        Task<List<RoutineDayEntryVM>> GetRoutineEntriesAsync(int routineId, DateOnly from, DateOnly to, string userId);
        Task<Dictionary<string,string>> GetRoutineMonthMapAsync(int routineId, int year, int month, string userId);

    }
}