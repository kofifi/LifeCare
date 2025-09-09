using AutoMapper;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using LifeCare.ViewModels.Habits;
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


        public record HabitEntryDto(DateTime Date, bool Completed, float? Quantity);

        public HabitsController(IHabitService habitService, UserManager<User> userManager, ITagService tagService,
            IMapper mapper)
        {
            _habitService = habitService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index([FromQuery] List<int> tagIds)
        {
            var userId = _userManager.GetUserId(User);
            var habits = await _habitService.GetAllHabitsAsync(userId!, tagIds);
            ViewBag.AvailableTags = await _habitService.GetUserTagsAsync(userId!);
            ViewBag.SelectedTagIds = tagIds ?? new List<int>();
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

            return View(habit);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var vm = new HabitVM
            {
                Type = HabitType.Checkbox,
                Color = "#3b82f6",
                Icon = "fa-dumbbell",
                AvailableTags = await _habitService.GetUserTagsAsync(userId!)
            };
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HabitVM habitVM)
        {
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                habitVM.AvailableTags = await _habitService.GetUserTagsAsync(userId!);
                return View(habitVM);
            }

            await _habitService.CreateHabitAsync(habitVM, userId!);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vm = await _habitService.GetHabitByIdAsync(id, userId!);
            vm.AvailableTags = await _habitService.GetUserTagsAsync(userId!);
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HabitVM habitVM)
        {
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                habitVM.AvailableTags = await _habitService.GetUserTagsAsync(userId!);
                return View(habitVM);
            }

            var existing = await _habitService.GetHabitByIdAsync(habitVM.Id, userId!);
            if (existing == null) return NotFound();

            await _habitService.UpdateHabitAsync(habitVM, userId!);
            TempData["Toast.Success"] = "Zaktualizowano nawyk.";
            return RedirectToAction(nameof(Details), new { id = habitVM.Id });
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

        [HttpGet]
        public async Task<IActionResult> HabitStats(int habitId)
        {
            var userId = _userManager.GetUserId(User);
            var stats = await _habitService.GetHabitStatsAsync(habitId, userId);

            var dto = new HabitStatsDto(
                stats.OverallPercent,
                stats.CurrentStreak,
                stats.BestStreak,
                stats.Total,
                stats.Completed,
                stats.Skipped,
                stats.Partial,
                stats.StartDateUtc
            );

            return Json(dto);
        }

        [HttpGet]
        public async Task<IActionResult> HabitEntries(int habitId, DateTime from, DateTime to)
        {
            var userId = _userManager.GetUserId(User);

            var entries = await _habitService.GetHabitEntriesAsync(habitId, userId, from, to);

            var dto = entries
                .OrderBy(e => e.Date)
                .Select(e => new HabitEntryDto(e.Date, e.Completed ?? false, e.Quantity))
                .ToList();

            return Json(dto);
        }

        [HttpGet]
        public async Task<IActionResult> HabitMonth(int habitId, int year, int month)
        {
            var userId = _userManager.GetUserId(User);

            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var habit = await _habitService.GetHabitByIdAsync(habitId, userId);
            if (habit == null) return NotFound();

            var entries = await _habitService.GetHabitEntriesAsync(habitId, userId, from, to);
            var allEntries =
                await _habitService.GetHabitEntriesAsync(habitId, userId, DateTime.MinValue, DateTime.MaxValue);

            var hasAny = allEntries.Any();
            var startDate = hasAny ? allEntries.Min(e => e.Date.Date) : DateTime.UtcNow.Date;
            var today = DateTime.UtcNow.Date;

            var isQuantity = habit.Type == HabitType.Quantity;
            var target = habit.TargetQuantity ?? 0;

            var map = new Dictionary<string, string>();
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                string status;
                if (d < startDate)
                {
                    status = "off";
                }
                else if (d > today)
                {
                    status = "future";
                }
                else
                {
                    var e = entries.FirstOrDefault(x => x.Date.Date == d);
                    if (isQuantity)
                    {
                        var q = (int?)(e?.Quantity) ?? 0;
                        if (target > 0)
                            status = q >= target ? "full" : (q > 0 ? "partial" : "none");
                        else
                            status = q > 0 ? "full" : "none";
                    }
                    else
                    {
                        status = (e?.Completed ?? false) ? "full" : "none";
                    }
                }

                map[d.ToString("yyyy-MM-dd")] = status;
            }

            return Json(map);
        }
    }
}