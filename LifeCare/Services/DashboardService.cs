using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly LifeCareDbContext _db;

        public DashboardService(LifeCareDbContext db)
        {
            _db = db;
        }

        public async Task<HomeDashboardVM> GetHomeDashboardAsync(string userId, DateOnly? date = null)
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);

            var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);
            var dayEnd = selectedDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var habits = await _db.Habits
                .AsNoTracking()
                .Include(h => h.Tags)
                .Where(h => h.UserId == userId)
                .ToListAsync();

            var habitIds = habits.Select(h => h.Id).ToList();

            var habitEntries = await _db.HabitEntries
                .AsNoTracking()
                .Where(e => habitIds.Contains(e.HabitId)
                            && e.Date >= dayStart
                            && e.Date < dayEnd)
                .ToListAsync();

            var habitItems = habits.Select(h =>
            {
                var entry = habitEntries.FirstOrDefault(e => e.HabitId == h.Id);

                var isQuantity = h.Type == HabitType.Quantity;
                decimal target = isQuantity ? (decimal)(h.TargetQuantity ?? 0) : 0m;
                decimal done = isQuantity ? (decimal)(entry?.Quantity ?? 0f) : 0m;

                bool isCompleted;
                if (isQuantity)
                {
                    isCompleted = target > 0 && done >= target;
                }
                else
                {
                    isCompleted = entry?.Completed == true;
                }

                return new HabitDashboardItemVM
                {
                    HabitId = h.Id,
                    Name = h.Name,
                    Color = string.IsNullOrWhiteSpace(h.Color) ? "#cccccc" : h.Color,
                    Icon = string.IsNullOrWhiteSpace(h.Icon) ? "fa-circle" : h.Icon,

                    IsQuantityType = isQuantity,
                    TargetQuantity = isQuantity ? (decimal?)target : null,
                    DoneQuantity = isQuantity ? done : 0m,
                    IsCompleted = isCompleted,

                    SelectedTagIds = h.Tags?.Select(t => t.Id).ToList() ?? new List<int>()
                };
            }).ToList();

            var routines = await _db.Routines
                .AsNoTracking()
                .Include(r => r.Tags)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var routineIds = routines.Select(r => r.Id).ToList();

            var routineEntries = await _db.RoutineEntries
                .AsNoTracking()
                .Include(e => e.StepEntries)
                .Where(e => routineIds.Contains(e.RoutineId)
                            && e.Date >= dayStart
                            && e.Date < dayEnd)
                .ToListAsync();

            var routineItems = routines.Select(r =>
            {
                var entry = routineEntries.FirstOrDefault(e => e.RoutineId == r.Id);

                int totalSteps = entry?.StepEntries.Count ?? 0;
                int completedSteps = entry?.StepEntries.Count(se => se.Completed) ?? 0;

                return new RoutineDashboardItemVM
                {
                    RoutineId = r.Id,
                    Name = r.Name,
                    Color = string.IsNullOrWhiteSpace(r.Color) ? "#cccccc" : r.Color,
                    TotalSteps = totalSteps,
                    CompletedSteps = completedSteps,
                    SelectedTagIds = r.Tags?.Select(t => t.Id).ToList() ?? new List<int>()
                };
            }).ToList();

            return new HomeDashboardVM
            {
                SelectedDate = selectedDate,
                Habits = habitItems,
                Routines = routineItems,
                TasksCount = 0
            };
        }
    }
}
