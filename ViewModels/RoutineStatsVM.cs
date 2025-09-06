namespace LifeCare.ViewModels;

public class RoutineStatsVM
{
    public DateTime StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    public int TotalOccurrences { get; set; }
    public int CompletedDays { get; set; }
    public int PartialDays { get; set; }
    public int SkippedDays { get; set; }

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public double OverallPercent { get; set; }

    public List<StepSkipVM> TopSkippedSteps { get; set; } = new();
}

public class StepSkipVM
{
    public int StepId { get; set; }
    public string Name { get; set; } = "";
    public int SkippedCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalEntries { get; set; }
}

public class RoutineDayEntryVM
{
    public string Date { get; set; } = "";
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
}