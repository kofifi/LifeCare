using System;
using System.Diagnostics;
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
        var model = new HomeDashboardVM
        {
            UsersCount = await _context.Users.CountAsync(),
            HabitsCount = await _context.Habits.CountAsync(),
            CategoriesCount = await GetCategoriesCountAsync()
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