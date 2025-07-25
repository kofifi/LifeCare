using AutoMapper;
using AutoMapper.QueryableExtensions;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Services;

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
                Date = entryVm.Date,
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
}