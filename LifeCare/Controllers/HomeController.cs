using System.Diagnostics;
using System.Security.Claims;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LifeCareDbContext _db;
        private readonly IDashboardService _dashboardService;

        public HomeController(
            ILogger<HomeController> logger,
            LifeCareDbContext db,
            IDashboardService dashboardService)
        {
            _logger = logger;
            _db = db;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(DateOnly? date = null, int[]? tagIds = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var model = await _dashboardService.GetHomeDashboardAsync(userId, date);

            var tags = await _db.Tags
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .Select(t => new TagVM
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();

            ViewBag.AvailableTags = tags;
            ViewBag.SelectedTagIds = tagIds ?? Array.Empty<int>();

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
}