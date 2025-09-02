using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("[controller]/[action]")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    public FilesController(IWebHostEnvironment env) => _env = env;

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Brak pliku.");

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fn  = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadsDir, fn);

        await using (var fs = System.IO.File.Create(path))
        {
            await file.CopyToAsync(fs);
        }

        var publicUrl = $"/uploads/products/{fn}";
        return Ok(new { url = publicUrl });
    }
}