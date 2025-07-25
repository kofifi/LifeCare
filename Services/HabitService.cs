using AutoMapper;
using LifeCare.Data;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Mvc;
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
    
    public async Task<List<HabitCategory>> GetUserCategoriesAsync(string userId)
    {
        return await _context.Set<HabitCategory>()
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }
}