using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.ViewModels;

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

    public IActionResult Index()
    {
        var model = new HomeDashboardVM
        {
            UsersCount = _context.Users.Count(),
            HabitsCount = _context.Habits.Count(),
            CategoriesCount = _context.Categories.Count()
        };

        return View(model);
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