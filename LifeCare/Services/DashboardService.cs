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
        private readonly IRoutineService _routineService;

        public DashboardService(LifeCareDbContext db, IRoutineService routineService)
        {
            _db = db;
            _routineService = routineService;
        }

        public async Task<HomeDashboardVM> GetHomeDashboardAsync(string userId, DateOnly? date = null)
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);

            var (habitItems, routineItems) = await LoadDayDataAsync(userId, selectedDate);

            var tasksCount = habitItems.Count + routineItems.Count;

            var doneHabits = habitItems.Count(h => h.IsCompleted);
            var doneRoutines = routineItems.Count(r => r.IsCompleted);
            var doneTotal = doneHabits + doneRoutines;

            var completion = tasksCount == 0
                ? 0
                : (int)Math.Round(100.0 * doneTotal / tasksCount);

            var currentStreak = await CalculateCurrentStreakAsync(userId, selectedDate);

            return new HomeDashboardVM
            {
                SelectedDate = selectedDate,
                Habits = habitItems,
                Routines = routineItems,
                TasksCount = tasksCount,
                OverallCompletionPercentage = completion,
                CurrentStreak = currentStreak
            };
        }

        public async Task<DailySummaryVM> GetDailySummaryAsync(string userId, DateOnly date)
        {
            var (habitItems, routineItems) = await LoadDayDataAsync(userId, date);

            var tasksCount = habitItems.Count + routineItems.Count;

            var doneHabits = habitItems.Count(h => h.IsCompleted);
            var doneRoutines = routineItems.Count(r => r.IsCompleted);
            var doneTotal = doneHabits + doneRoutines;

            var completion = tasksCount == 0
                ? 0
                : (int)Math.Round(100.0 * doneTotal / tasksCount);

            var currentStreak = await CalculateCurrentStreakAsync(userId, date);

            return new DailySummaryVM
            {
                TasksCount = tasksCount,
                OverallCompletionPercentage = completion,
                CurrentStreak = currentStreak
            };
        }

        private async Task<(List<HabitDashboardItemVM> Habits, List<RoutineDashboardItemVM> Routines)>
            LoadDayDataAsync(string userId, DateOnly date)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

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

                    Unit = h.Unit,                       // ← to jest kluczowa linijka
                    SelectedTagIds = h.Tags?.Select(t => t.Id).ToList() ?? new List<int>()
                };
            }).ToList();

            var routinesForDay = await _routineService.GetForDateAsync(date, userId);

            var routineItems = routinesForDay.Select(r => new RoutineDashboardItemVM
            {
                RoutineId = r.RoutineId,
                Name = r.Name,
                Color = string.IsNullOrWhiteSpace(r.Color) ? "#cccccc" : r.Color,
                TotalSteps = r.TotalSteps,
                CompletedSteps = r.DoneSteps,
                SelectedTagIds = new List<int>()
            }).ToList();

            return (habitItems, routineItems);
        }

        private async Task<int> CalculateCurrentStreakAsync(string userId, DateOnly selectedDate)
        {
            var streak = 0;
            var day = selectedDate;

            async Task<(int Tasks, int Done)> GetDayTotals(DateOnly d)
            {
                var (habitItems, routineItems) = await LoadDayDataAsync(userId, d);

                var tasks = habitItems.Count + routineItems.Count;
                var done = habitItems.Count(h => h.IsCompleted) +
                           routineItems.Count(r => r.IsCompleted);

                return (tasks, done);
            }

            {
                var (tasksToday, doneToday) = await GetDayTotals(day);

                if (tasksToday > 0 && doneToday == tasksToday)
                {
                    streak++;
                    day = day.AddDays(-1);
                }
                else
                {
                    day = day.AddDays(-1);
                }
            }

            while (true)
            {
                var (tasks, done) = await GetDayTotals(day);

                if (tasks == 0)
                    break;

                if (done < tasks)
                    break;

                streak++;
                day = day.AddDays(-1);
            }

            return streak;
        }
    }
}
