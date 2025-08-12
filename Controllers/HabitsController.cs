using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LifeCare.Controllers
{
    [Authorize]
    public class HabitsController : Controller
    {
        private readonly IHabitService _habitService;
        private readonly UserManager<User> _userManager;

        // DTOs dla akcji AJAX (Details)
        public record HabitStatsDto(double OverallPercent, int CurrentStreak, int BestStreak, int Total, int Completed, int Skipped);
        public record HabitEntryDto(DateTime Date, bool Completed, float? Quantity);

        public HabitsController(IHabitService habitService, UserManager<User> userManager)
        {
            _habitService = habitService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var habits = await _habitService.GetAllHabitsAsync(userId);
            var categories = await _habitService.GetUserCategoriesAsync(userId);

            ViewBag.Categories = categories;
            return View(habits);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            var habit = await _habitService.GetHabitByIdAsync(id, userId);
            if (habit == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _habitService.GetUserCategoriesAsync(userId);
            return View(habit);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.Categories = await _habitService.GetUserCategoriesAsync(userId);

            // Domyślne wartości dla nowego nawyku
            return View(new HabitVM
            {
                Type = HabitType.Checkbox,
                Color = "#3b82f6",
                Icon = "fa-dumbbell"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HabitVM habitVM)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _habitService.GetUserCategoriesAsync(_userManager.GetUserId(User));
                return View(habitVM);
            }

            var userId = _userManager.GetUserId(User);
            await _habitService.CreateHabitAsync(habitVM, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var habit = await _habitService.GetHabitByIdAsync(id, userId);

            if (habit == null) return NotFound();

            ViewBag.Categories = await _habitService.GetUserCategoriesAsync(userId);
            return View(habit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HabitVM habitVM)
        {
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _habitService.GetUserCategoriesAsync(userId);
                return View(habitVM);
            }

            await _habitService.UpdateHabitAsync(habitVM, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var habit = await _habitService.GetHabitByIdAsync(id, userId);
            if (habit == null) return NotFound();

            return View(habit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            await _habitService.DeleteHabitAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] List<int> orderedHabitIds)
        {
            var userId = _userManager.GetUserId(User);
            await _habitService.UpdateHabitOrderAsync(orderedHabitIds, userId);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetEntries(DateTime date)
        {
            var userId = _userManager.GetUserId(User);
            var entries = await _habitService.GetEntriesForDateAsync(date, userId);
            return Json(entries);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEntry([FromBody] HabitEntryVM entryVM)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _habitService.SaveHabitEntryAsync(entryVM, userId);
            return result ? Ok() : BadRequest();
        }

        // -----------------------------
        //      AKCJE POD DETAILS
        // -----------------------------

        // GET /Habits/HabitStats?habitId=1
        [HttpGet]
        public async Task<IActionResult> HabitStats(int habitId)
        {
            var userId = _userManager.GetUserId(User);

            // W serwisie zaimplementuj logikę liczenia statystyk:
            // Task<(double OverallPercent,int CurrentStreak,int BestStreak,int Total,int Completed,int Skipped)>
            //     GetHabitStatsAsync(int habitId, string userId)
            var stats = await _habitService.GetHabitStatsAsync(habitId, userId);

            var dto = new HabitStatsDto(
                stats.OverallPercent,
                stats.CurrentStreak,
                stats.BestStreak,
                stats.Total,
                stats.Completed,
                stats.Skipped
            );

            return Json(dto);
        }

        // GET /Habits/HabitEntries?habitId=1&from=2025-08-01&to=2025-08-07
        [HttpGet]
        public async Task<IActionResult> HabitEntries(int habitId, DateTime from, DateTime to)
        {
            var userId = _userManager.GetUserId(User);

            // W serwisie:
            // Task<IReadOnlyList<HabitEntry>> GetHabitEntriesAsync(int habitId, string userId, DateTime from, DateTime to)
            var entries = await _habitService.GetHabitEntriesAsync(habitId, userId, from, to);

            var dto = entries
                .OrderBy(e => e.Date)
                .Select(e => new HabitEntryDto(e.Date, e.Completed ?? false, e.Quantity))
                .ToList();

            return Json(dto);
        }

        // GET /Habits/HabitMonth?habitId=1&year=2025&month=8
        [HttpGet]
        public async Task<IActionResult> HabitMonth(int habitId, int year, int month)
        {
            var userId = _userManager.GetUserId(User);

            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var habit = await _habitService.GetHabitByIdAsync(habitId, userId);
            if (habit == null) return NotFound();

            var entries = await _habitService.GetHabitEntriesAsync(habitId, userId, from, to);
            var isQuantity = habit.Type == HabitType.Quantity;
            var target = habit.TargetQuantity ?? 0;

            // Map statusów dnia: 'full'|'partial'|'none'
            var map = new Dictionary<string, string>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var e = entries.FirstOrDefault(x => x.Date.Date == d.Date);
                string status;

                if (isQuantity)
                {
                    var q = e?.Quantity ?? 0;
                    if (q <= 0) status = "none";
                    else if (q >= target && target > 0) status = "full";
                    else status = "partial";
                }
                else
                {
                    status = (e?.Completed ?? false) ? "full" : "none";
                }

                map[d.ToString("yyyy-MM-dd")] = status;
            }

            return Json(map);
        }
    }
}
