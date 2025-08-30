using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LifeCareDbContext _context;

    public HomeController(ILogger<HomeController> logger, LifeCareDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var startDate = DateTime.Today.AddDays(-6);

        // Basic counts
        var usersCount = await _context.Users.CountAsync();
        var habitsCount = await _context.Habits.CountAsync();
        var categoriesCount = await GetCategoriesCountAsync();
        var routinesCount = await _context.Routines.CountAsync();
        var habitEntriesCount = await _context.HabitEntries.CountAsync();

        // Averages for the last 7 days
        var avgWater = await _context.DailyStats
            .Where(ds => ds.Date >= startDate)
            .Select(ds => ds.WaterIntakeLiters)
            .DefaultIfEmpty(0)
            .AverageAsync();

        var avgSteps = await _context.DailyStats
            .Where(ds => ds.Date >= startDate)
            .Select(ds => ds.Steps)
            .DefaultIfEmpty(0)
            .AverageAsync();

        // Habit entries chart data
        var habitEntriesByDate = await _context.HabitEntries
            .Where(e => e.Date >= startDate)
            .GroupBy(e => e.Date.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var labels = new List<string>();
        var data = new List<int>();
        for (var i = 0; i < 7; i++)
        {
            var date = startDate.AddDays(i).Date;
            labels.Add(date.ToString("MMM dd"));
            var entry = habitEntriesByDate.FirstOrDefault(e => e.Date == date);
            data.Add(entry?.Count ?? 0);
        }

        var model = new HomeDashboardVM
        {
            UsersCount = usersCount,
            HabitsCount = habitsCount,
            CategoriesCount = categoriesCount,
            RoutinesCount = routinesCount,
            HabitEntriesCount = habitEntriesCount,
            AvgWaterIntakeLast7Days = Math.Round(avgWater, 2),
            AvgStepsLast7Days = Math.Round(avgSteps, 2),
            HabitEntriesLabels = labels,
            HabitEntriesData = data
        };

        return View(model);
    }

    private async Task<int> GetCategoriesCountAsync()
    {
        try
        {
            return await _context.Categories.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count categories");
            return 0;
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}