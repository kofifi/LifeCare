using System.Diagnostics;
using LifeCare.Services.Interfaces;

namespace LifeCare.Services
{
    public class GitInfoService : IGitInfoService
    {
        public string CommitHash { get; }

        public GitInfoService()
        {
            CommitHash = RetrieveCommitHash();
        }

        private static string RetrieveCommitHash()
        {
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    return (process.StandardOutput.ReadLine() ?? string.Empty).Trim();
                }
            }
            catch
            {
                // ignored
            }

            return "unknown";
        }
    }
}
