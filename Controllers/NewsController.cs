using Microsoft.AspNetCore.Mvc;

namespace LifeCare.Controllers;

public class NewsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Article(int id)
    {
        ViewData["ArticleId"] = id;
        return View();
    }
}

