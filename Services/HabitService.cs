using AutoMapper;
using AutoMapper.QueryableExtensions;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Services
{
    public class HabitService : IHabitService
    {
        private readonly LifeCareDbContext _context;
        private readonly IMapper _mapper;

        public HabitService(LifeCareDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<HabitVM>> GetAllHabitsAsync(string userId)
        {
            var habits = await _context.Habits
                .Include(h => h.Category)
                .Where(h => h.UserId == userId)
                .OrderBy(h => h.Order)
                .ToListAsync();

            return _mapper.Map<List<HabitVM>>(habits);
        }

        public async Task<HabitVM> GetHabitByIdAsync(int habitId, string userId)
        {
            var habit = await _context.Habits
                .Include(h => h.Category)
                .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

            return _mapper.Map<HabitVM>(habit);
        }

        public async Task CreateHabitAsync(HabitVM habitVM, string userId)
        {
            var habit = _mapper.Map<Habit>(habitVM);
            habit.UserId = userId;
            
            var maxOrder = await _context.Habits.Where(h => h.UserId == userId).MaxAsync(h => (int?)h.Order) ?? -1;
            habit.Order = maxOrder + 1;

            _context.Habits.Add(habit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateHabitAsync(HabitVM habitVM, string userId)
        {
            var existing = await _context.Habits
                .FirstOrDefaultAsync(h => h.Id == habitVM.Id && h.UserId == userId);
            if (existing == null) return;

            _mapper.Map(habitVM, existing);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteHabitAsync(int habitId, string userId)
        {
            var habit = await _context.Habits
                .Include(h => h.Category)
                .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
            if (habit == null) return;

            _context.Habits.Remove(habit);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Category>> GetUserCategoriesAsync(string userId)
        {
            return await _context.Set<Category>()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task UpdateHabitOrderAsync(List<int> orderedHabitIds, string userId)
        {
            var habits = await _context.Habits
                .Where(h => h.UserId == userId && orderedHabitIds.Contains(h.Id))
                .ToListAsync();

            for (int i = 0; i < orderedHabitIds.Count; i++)
            {
                var habit = habits.FirstOrDefault(h => h.Id == orderedHabitIds[i]);
                if (habit != null)
                {
                    habit.Order = i;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<HabitEntryVM>> GetEntriesForDateAsync(DateTime date, string? userId)
        {
            return await _context.HabitEntries
                .Where(e => e.Habit.UserId == userId && e.Date.Date == date.Date)
                .OrderBy(e => e.HabitId)
                .ProjectTo<HabitEntryVM>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<bool> SaveHabitEntryAsync(HabitEntryVM entryVm, string? userId)
        {
            var habit = await _context.Habits
                .FirstOrDefaultAsync(h => h.UserId == userId && h.Id == entryVm.HabitId);
            if (habit == null)
                return false;

            var entry = await _context.HabitEntries
                .FirstOrDefaultAsync(e => e.Date.Date == entryVm.Date.Date &&
                                          e.HabitId == entryVm.HabitId);

            if (entry == null)
            {
                entry = new HabitEntry
                {
                    HabitId = entryVm.HabitId,
                    Date = entryVm.Date.Date,
                    Completed = entryVm.Completed,
                    Quantity = entryVm.Quantity
                };
                _context.HabitEntries.Add(entry);
            }
            else
            {
                entry.Completed = entryVm.Completed;
                entry.Quantity = entryVm.Quantity;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<HabitEntry>> GetHabitEntriesAsync(int habitId, string userId, DateTime from,
            DateTime to)
        {
            var fromDate = from.Date;
            var toDate = to.Date;

            var entries = await _context.HabitEntries
                .Include(e => e.Habit)
                .Where(e => e.HabitId == habitId &&
                            e.Habit.UserId == userId &&
                            e.Date.Date >= fromDate &&
                            e.Date.Date <= toDate)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return entries;
        }

        public async Task<(double OverallPercent, int CurrentStreak, int BestStreak, int Total, int Completed, int
                Skipped, int Partial, DateTime StartDateUtc)>
            GetHabitStatsAsync(int habitId, string userId)
        {
            var habit = await _context.Habits
                .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

            if (habit == null)
            {
                return (0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow.Date);
            }

            var entries = await _context.HabitEntries
                .Where(e => e.HabitId == habitId && e.Habit.UserId == userId)
                .ToListAsync();

            if (!entries.Any())
            {
                var today = DateTime.UtcNow.Date;
                return (0, 0, 0, 1, 0, 1, 0, today);
            }

            var start = entries.Min(e => e.Date.Date);
            var end = DateTime.UtcNow.Date;
            if (start > end) start = end;

            var byDate = entries
                .GroupBy(e => e.Date.Date)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

            bool IsCompletedForDay(DateTime day)
            {
                if (!byDate.TryGetValue(day, out var e)) return false;

                if (habit.Type == HabitType.Quantity)
                {
                    var target = habit.TargetQuantity ?? 0;
                    var q = (int?)e.Quantity ?? 0;
                    if (target > 0) return q >= target;
                    return q > 0;
                }
                else
                {
                    return e.Completed ?? false;
                }
            }

            bool IsPartialForDay(DateTime day)
            {
                if (habit.Type != HabitType.Quantity) return false;
                if (!byDate.TryGetValue(day, out var e)) return false;

                var target = habit.TargetQuantity ?? 0;
                var q = (int?)e.Quantity ?? 0;

                if (target <= 0) return false;
                return q > 0 && q < target;
            }

            int totalDays = (int)(end - start).TotalDays + 1;
            int completedDays = 0;
            int partialDays = 0;

            int running = 0;
            int bestStreak = 0;

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var completed = IsCompletedForDay(d);
                var partial = IsPartialForDay(d);

                if (completed) completedDays++;
                if (partial) partialDays++;

                if (completed)
                {
                    running++;
                    if (running > bestStreak) bestStreak = running;
                }
                else
                {
                    running = 0;
                }
            }

            var currentStreak = 0;
            for (var d = end; d >= start; d = d.AddDays(-1))
            {
                if (IsCompletedForDay(d)) currentStreak++;
                else break;
            }

            var skipped = totalDays - completedDays - partialDays;
            var overallPercent = totalDays > 0 ? (double)completedDays / totalDays * 100.0 : 0.0;

            return (overallPercent, currentStreak, bestStreak, totalDays, completedDays, skipped, partialDays,
                start.ToUniversalTime());
        }
    }
}