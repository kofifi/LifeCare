using System.Text.Json;
using System.Globalization;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LifeCare.Models;
using LifeCare.Helpers;


namespace LifeCare.Controllers
{
    [Authorize]
    public class RoutinesController : Controller
    {
        private readonly IRoutineService _routineService;
        private readonly UserManager<User> _userManager;

        public record ToggleStepDto(int RoutineId, int StepId, DateOnly Date, bool Completed, string? Note);

        public record MarkAllDto(int RoutineId, DateOnly Date);

        public record ToggleProductDto(int RoutineId, int StepId, int ProductId, string Date, bool Completed);

        public record SetAllDto(int RoutineId, string Date, bool Completed);

        public record CompleteDto(int RoutineId, string Date, bool Completed);

        public RoutinesController(IRoutineService routineService, UserManager<User> userManager, ITagService tagService)
        {
            _routineService = routineService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index([FromQuery] List<int> tagIds)
        {
            var userId = _userManager.GetUserId(User);
            var routines = await _routineService.GetAllRoutinesAsync(userId!, tagIds);
            ViewBag.AvailableTags = await _routineService.GetUserTagsAsync(userId!);
            ViewBag.SelectedTagIds = tagIds ?? new List<int>();
            return View(routines);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var vm = new RoutineVM
            {
                Color = "#3b82f6",
                Icon = "fa-spa",
                RRule = "FREQ=DAILY;INTERVAL=1",
                AvailableTags = await _routineService.GetUserTagsAsync(userId!)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoutineVM vm, string? stepsJson)
        {
            var userId = _userManager.GetUserId(User);

            vm.Steps = RoutineStepsJsonHelper.Parse(stepsJson);

            if (string.IsNullOrWhiteSpace(vm.RRule))
                vm.RRule = null;

            if (vm.Steps == null || vm.Steps.Count == 0)
                ModelState.AddModelError(nameof(vm.Steps), "Dodaj przynajmniej jeden krok.");

            if (!ModelState.IsValid)
            {
                vm.AvailableTags = await _routineService.GetUserTagsAsync(userId!);
                return View(vm);
            }

            await _routineService.CreateRoutineAsync(vm, userId!);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vm = await _routineService.GetRoutineAsync(id, userId);
            if (vm == null) return NotFound();

            vm.AvailableTags = await _routineService.GetUserTagsAsync(userId!);
            
            var stepsJson = System.Text.Json.JsonSerializer.Serialize(
                (vm.Steps ?? new List<RoutineStepVM>())
                .OrderBy(s => s.Order)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    minutes = s.EstimatedMinutes,
                    desc = s.Description,
                    rotation = new { enabled = s.RotationEnabled, mode = s.RotationMode },
                    products = (s.Products ?? new List<RoutineStepProductVM>())
                        .OrderBy(p => p.Id)
                        .Select(p => new
                            { id = p.Id, name = p.Name, note = p.Note, url = p.Url, imageUrl = p.ImageUrl })
                })
            );

            ViewBag.StepsJson = stepsJson;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoutineVM vm, string? stepsJson)
        {
            var userId = _userManager.GetUserId(User);

            vm.Steps = RoutineStepsJsonHelper.Parse(stepsJson);

            if (vm.Steps == null || vm.Steps.Count == 0)
                ModelState.AddModelError(nameof(vm.Steps), "Rutyna musi mieć co najmniej jeden krok.");

            if (!ModelState.IsValid)
            {
                vm.AvailableTags = await _routineService.GetUserTagsAsync(userId!);
                ViewBag.StepsJson = BuildStepsJsonForEditor(vm);
                return View(vm);
            }

            await _routineService.UpdateRoutineAsync(vm, userId!);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var routine = await _routineService.GetRoutineAsync(id, userId);
            if (routine == null) return NotFound();
            return View(routine); // prosty widok potwierdzenia
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            await _routineService.DeleteRoutineAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ForDate(string? date)
        {
            var userId = _userManager.GetUserId(User);

            if (!DateOnly.TryParseExact(date ?? string.Empty, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                d = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            }

            var list = await _routineService.GetForDateAsync(d, userId);
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStep([FromBody] ToggleStepDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var ok = await _routineService.ToggleStepAsync(dto.RoutineId, dto.StepId, dto.Date, dto.Completed, dto.Note,
                userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAll([FromBody] MarkAllDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var ok = await _routineService.MarkAllStepsAsync(dto.RoutineId, dto.Date, userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProduct([FromBody] ToggleProductDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (!DateOnly.TryParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d))
                return BadRequest();

            var ok = await _routineService.ToggleStepProductAsync(dto.RoutineId, dto.StepId, dto.ProductId, d, dto.Completed,
                userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> SetAll([FromBody] SetAllDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (!DateOnly.TryParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d))
                return BadRequest();

            var ok = await _routineService.SetAllStepsAsync(dto.RoutineId, d, dto.Completed, userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> Complete([FromBody] CompleteDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (!DateOnly.TryParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d))
                return BadRequest();

            var ok = await _routineService.SetRoutineCompletedAsync(dto.RoutineId, d, dto.Completed, userId);
            return ok ? Ok() : BadRequest();
        }

        private static string BuildStepsJsonForEditor(RoutineVM vm)
        {
            var items = (vm.Steps ?? new List<RoutineStepVM>())
                .OrderBy(s => s.Order)
                .Select(s => new
                {
                    name = s.Name,
                    minutes = s.EstimatedMinutes,
                    desc = s.Description,
                    products = (s.Products ?? new List<RoutineStepProductVM>()).Select(p => new
                    {
                        name = p.Name,
                        note = p.Note,
                        url = p.Url,
                        imageUrl = p.ImageUrl
                    }),
                    rotation = new { enabled = s.RotationEnabled, mode = s.RotationMode }
                });

            return JsonSerializer.Serialize(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vm = await _routineService.GetRoutineAsync(id, userId); // już masz
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> RoutineStats(int routineId)
        {
            var userId = _userManager.GetUserId(User);
            var stats = await _routineService.GetRoutineStatsAsync(routineId, userId);
            return Json(stats);
        }

        [HttpGet]
        public async Task<IActionResult> RoutineEntries(int routineId, string from, string to)
        {
            var userId = _userManager.GetUserId(User);
            if (!DateOnly.TryParse(from, out var f) || !DateOnly.TryParse(to, out var t)) return BadRequest();
            var list = await _routineService.GetRoutineEntriesAsync(routineId, f, t, userId);
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> RoutineMonth(int routineId, int year, int month)
        {
            var userId = _userManager.GetUserId(User);
            var map = await _routineService.GetRoutineMonthMapAsync(routineId, year, month, userId);
            return Json(map);
        }
    }
}