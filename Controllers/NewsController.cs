using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LifeCare.Models;

namespace LifeCare.Controllers;

public class NewsController : Controller
{
    public IActionResult Index()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "log --pretty=format:%h:::%s",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        using var process = Process.Start(psi);
        var output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var commits = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var parts = line.Split(":::", 2);
                return new CommitInfo { Hash = parts[0], Message = parts.Length > 1 ? parts[1] : string.Empty };
            })
            .ToList();

        return View(commits);
    }
}

