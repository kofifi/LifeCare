using AutoMapper;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LifeCare.Controllers;

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