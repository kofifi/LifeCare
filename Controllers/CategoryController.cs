using AutoMapper;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LifeCare.Controllers;

[Authorize]
public class CategoryController : Controller
{
    private readonly LifeCareDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;

    public CategoryController(LifeCareDbContext context, UserManager<User> userManager, IMapper mapper)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryVM categoryVM)
    {
        if (!ModelState.IsValid)
        {
            return View(categoryVM);
        }

        var userId = _userManager.GetUserId(User);
        var category = _mapper.Map<Category>(categoryVM);
        category.UserId = userId;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category == null) return NotFound();

        var vm = _mapper.Map<CategoryVM>(category);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryVM categoryVM)
    {
        if (!ModelState.IsValid) return View(categoryVM);

        var userId = _userManager.GetUserId(User);
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category == null) return NotFound();

        category.Name = categoryVM.Name;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAjax([FromBody] CategoryVM categoryVM)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _userManager.GetUserId(User);
        var category = _mapper.Map<Category>(categoryVM);
        category.UserId = userId;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Json(new { id = category.Id, name = category.Name });
    }
}