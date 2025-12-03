using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly LifeCareDbContext _db;
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<User> _userManager;

        public HomeController(
            LifeCareDbContext db,
            IDashboardService dashboardService,
            UserManager<User> userManager)
        {
            _db = db;
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateOnly? date = null, int[]? tagIds = null)
        {
            var userId = _userManager.GetUserId(User);
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

        [HttpGet]
        public async Task<IActionResult> DailySummary(DateOnly date)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var summary = await _dashboardService.GetDailySummaryAsync(userId, date);

            return Json(new
            {
                overallCompletionPercentage = summary.OverallCompletionPercentage,
                tasksCount = summary.TasksCount,
                currentStreak = summary.CurrentStreak
            });
        }

    }
}
