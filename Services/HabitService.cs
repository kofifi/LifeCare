using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            // ustaw porządek na koniec listy
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

        // ------------------------------
        //         NOWE METODY
        // ------------------------------

        public async Task<IReadOnlyList<HabitEntry>> GetHabitEntriesAsync(int habitId, string userId, DateTime from, DateTime to)
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

        public async Task<(double OverallPercent, int CurrentStreak, int BestStreak, int Total, int Completed, int Skipped)>
            GetHabitStatsAsync(int habitId, string userId)
        {
            var habit = await _context.Habits
                .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

            if (habit == null)
            {
                return (0, 0, 0, 0, 0, 0);
            }

            // Pobierz wszystkie wpisy danego nawyku (dla użytkownika)
            var entries = await _context.HabitEntries
                .Where(e => e.HabitId == habitId && e.Habit.UserId == userId)
                .ToListAsync();

            if (!entries.Any())
            {
                return (0, 0, 0, 0, 0, 0);
            }

            // Zakres od pierwszego wpisu do dziś
            var start = entries.Min(e => e.Date.Date);
            var end = DateTime.Today;
            if (start > end) start = end;

            // Zbuduj słownik wpisów po dacie
            var byDate = entries
                .GroupBy(e => e.Date.Date)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First()); // last entry of the day

            bool IsCompletedForDay(DateTime day)
            {
                if (!byDate.TryGetValue(day, out var e)) return false;

                if (habit.Type == HabitType.Quantity)
                {
                    var target = habit.TargetQuantity ?? 0;
                    var q = e.Quantity ?? 0;
                    if (target > 0) return q >= target;
                    // gdy target == 0 traktuj dowolną ilość > 0 jako wykonane
                    return q > 0;
                }
                else
                {
                    return e.Completed ?? false;
                }
            }

            int totalDays = (int)(end - start).TotalDays + 1;
            int completedDays = 0;

            // serie
            int currentStreak = 0;
            int bestStreak = 0;

            // liczenie completed skipped i streaków
            int running = 0;
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var done = IsCompletedForDay(d);
                if (done)
                {
                    completedDays++;
                    running++;
                    if (running > bestStreak) bestStreak = running;
                }
                else
                {
                    running = 0;
                }
            }

            // aktualna seria – liczymy wstecz od dziś
            var cur = 0;
            for (var d = end; d >= start; d = d.AddDays(-1))
            {
                if (IsCompletedForDay(d)) cur++;
                else break;
            }
            currentStreak = cur;

            var skipped = totalDays - completedDays;
            var overallPercent = totalDays > 0 ? (double)completedDays / totalDays * 100.0 : 0.0;

            return (overallPercent, currentStreak, bestStreak, totalDays, completedDays, skipped);
        }
    }
}
