namespace LifeCare.Models;

public class RoutineEntry
{
    public int Id { get; set; }

    public int RoutineId { get; set; }
    public Routine Routine { get; set; } = null!;

    public DateTime Date { get; set; }

    public bool Completed { get; set; }
    public bool Skipped { get; set; }
    public string? Note { get; set; }

    public ICollection<RoutineStepEntry> StepEntries { get; set; } = new List<RoutineStepEntry>();
}
