using System;
using System.Collections.Generic;

namespace LifeCare.Models;

public class CommitInfo
{
    public string Hash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<string> Branches { get; set; } = new();
}
