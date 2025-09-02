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
        private readonly IRoutineService _service;
        private readonly UserManager<User> _userManager;

        public record ToggleStepDto(int RoutineId, int StepId, DateOnly Date, bool Completed, string? Note);

        public record MarkAllDto(int RoutineId, DateOnly Date);

        public record ToggleProductDto(int RoutineId, int StepId, int ProductId, string Date, bool Completed);

        public record SetAllDto(int RoutineId, string Date, bool Completed);

        public record CompleteDto(int RoutineId, string Date, bool Completed);

        public RoutinesController(IRoutineService service, UserManager<User> userManager)
        {
            _service = service;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var all = await _service.GetAllRoutinesAsync(userId);
            ViewBag.Categories = await _service.GetUserCategoriesAsync(userId);
            return View(all);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.Categories = await _service.GetUserCategoriesAsync(userId);

            return View(new RoutineVM
            {
                Color = "#3b82f6",
                Icon = "fa-spa",
                RRule = "FREQ=DAILY;INTERVAL=1"
            });
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
                ViewBag.Categories = await _service.GetUserCategoriesAsync(userId);
                return View(vm);
            }

            await _service.CreateRoutineAsync(vm, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vm = await _service.GetRoutineAsync(id, userId);
            if (vm == null) return NotFound();

            ViewBag.Categories = await _service.GetUserCategoriesAsync(userId);

            var stepsJson = System.Text.Json.JsonSerializer.Serialize(
                (vm.Steps ?? new List<RoutineStepVM>())
                .OrderBy(s => s.Order)
                .Select(s => new {
                    id = s.Id,
                    name = s.Name,
                    minutes = s.EstimatedMinutes,
                    desc = s.Description,
                    rotation = new { enabled = s.RotationEnabled, mode = s.RotationMode },
                    products = (s.Products ?? new List<RoutineStepProductVM>())
                        .OrderBy(p => p.Id)
                        .Select(p => new {
                            id = p.Id,
                            name = p.Name,
                            note = p.Note,
                            url = p.Url,
                            imageUrl = p.ImageUrl
                        })
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
                ViewBag.Categories = await _service.GetUserCategoriesAsync(userId);
                ViewBag.StepsJson = BuildStepsJsonForEditor(vm);
                return View(vm);
            }

            await _service.UpdateRoutineAsync(vm, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var routine = await _service.GetRoutineAsync(id, userId);
            if (routine == null) return NotFound();
            return View(routine); // prosty widok potwierdzenia
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            await _service.DeleteRoutineAsync(id, userId);
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

            var list = await _service.GetForDateAsync(d, userId);
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStep([FromBody] ToggleStepDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var ok = await _service.ToggleStepAsync(dto.RoutineId, dto.StepId, dto.Date, dto.Completed, dto.Note,
                userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAll([FromBody] MarkAllDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var ok = await _service.MarkAllStepsAsync(dto.RoutineId, dto.Date, userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProduct([FromBody] ToggleProductDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (!DateOnly.TryParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d))
                return BadRequest();

            var ok = await _service.ToggleStepProductAsync(dto.RoutineId, dto.StepId, dto.ProductId, d, dto.Completed,
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

            var ok = await _service.SetAllStepsAsync(dto.RoutineId, d, dto.Completed, userId);
            return ok ? Ok() : BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> Complete([FromBody] CompleteDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (!DateOnly.TryParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d))
                return BadRequest();

            var ok = await _service.SetRoutineCompletedAsync(dto.RoutineId, d, dto.Completed, userId);
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
    }
}