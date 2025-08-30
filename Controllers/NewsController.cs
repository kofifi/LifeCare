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
                var hash = parts[0];
                var message = parts.Length > 1 ? parts[1] : string.Empty;

                var branchPsi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"branch --contains {hash} --format=\"%(refname:short)\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                using var branchProcess = Process.Start(branchPsi);
                var branchOutput = branchProcess!.StandardOutput.ReadToEnd();
                branchProcess.WaitForExit();

                var branches = branchOutput
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => b.Trim())
                    .ToList();

                return new CommitInfo
                {
                    Hash = hash,
                    Message = message,
                    Branches = branches
                };
            })
            .ToList();

        return View(commits);
    }
}

