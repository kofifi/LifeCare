using AutoMapper;
using LifeCare.Data;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Controllers;

[Authorize]
public class HabitsController : Controller
{
    private readonly IHabitService _habitService;
    private readonly UserManager<User> _userManager;

    public HabitsController(IHabitService habitService, UserManager<User> userManager)
    {
        _habitService = habitService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var habits = await _habitService.GetAllHabitsAsync(userId);
        
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
        ViewBag.Categories = await _habitService.GetUserCategoriesAsync(_userManager.GetUserId(User));
        return View();
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
}