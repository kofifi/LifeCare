using LifeCare.Services.Interfaces;
using LifeCare.ViewModels.AdminUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeCare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IAdminUserService _service;

        public UsersController(IAdminUserService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _service.ListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(string id)
        {
            var vm = await _service.GetAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        public IActionResult Create()
        {
            return View(new AdminUserCreateVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserCreateVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var res = await _service.CreateAsync(vm);
            if (!res.ok)
            {
                ModelState.AddModelError(string.Empty, res.error ?? "Error");
                return View(vm);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var vm = await _service.GetForEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminUserEditVM vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var res = await _service.UpdateAsync(vm);
            if (!res.ok)
            {
                ModelState.AddModelError(string.Empty, res.error ?? "Error");
                return View(vm);
            }
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        public async Task<IActionResult> Delete(string id)
        {
            var vm = await _service.GetAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var res = await _service.DeleteAsync(id);
            if (!res.ok)
            {
                TempData["Toast.Error"] = res.error ?? "Error";
                return RedirectToAction(nameof(Delete), new { id });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
