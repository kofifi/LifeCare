namespace LifeCare.Models;

public class RoutineStepEntry
{
    public int Id { get; set; }

    public int RoutineEntryId { get; set; }
    public RoutineEntry RoutineEntry { get; set; } = null!;

    public int RoutineStepId { get; set; }
    public RoutineStep RoutineStep { get; set; } = null!;

    public bool Completed { get; set; }
    public bool Skipped { get; set; }
    public string? Note { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public ICollection<RoutineStepProductEntry> ProductEntries { get; set; } =
        new List<RoutineStepProductEntry>();
}