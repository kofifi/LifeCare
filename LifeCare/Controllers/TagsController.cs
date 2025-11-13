using LifeCare.Models;
using LifeCare.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Controllers;

[Authorize]
[Route("Tags")]
public class TagsController : Controller
{
    private readonly ITagService _service;
    private readonly UserManager<User> _userManager;

    public TagsController(ITagService service, UserManager<User> userManager)
    {
        _service = service;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var tags = await _service.GetUserTagsAsync(userId);
        return View(tags);
    }

    [HttpGet("List")]
    public async Task<IActionResult> List()
    {
        var userId = _userManager.GetUserId(User);
        var tags = await _service.GetUserTagsAsync(userId);
        return Json(tags.Select(t => new { t.Id, t.Name }));
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Name is required" });

        var userId = _userManager.GetUserId(User);
        try
        {
            var tag = await _service.CreateTagAsync(name.Trim(), userId);
            return Ok(new { id = tag.Id, name = tag.Name });
        }
        catch (DbUpdateException dbex) when (dbex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Conflict(new { error = "Tag o takiej nazwie już istnieje." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] int id, [FromForm] string name)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Invalid payload" });

        var userId = _userManager.GetUserId(User);
        var ok = await _service.UpdateTagAsync(id, name.Trim(), userId);
        if (!ok) return NotFound(new { error = "Tag not found" });

        return Ok(new { id, name = name.Trim() });
    }
    
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] int id)
    {
        if (id <= 0) return BadRequest(new { error = "Invalid id" });

        var userId = _userManager.GetUserId(User);
        var ok = await _service.DeleteTagAsync(id, userId);
        if (!ok) return NotFound(new { error = "Tag not found" });

        return Ok(new { id });
    }
}
