namespace LifeCare.Models;

public class RoutineStepProductEntry
{
    public int Id { get; set; }

    public int RoutineStepEntryId { get; set; }
    public RoutineStepEntry StepEntry { get; set; }

    public int RoutineStepProductId { get; set; }
    public RoutineStepProduct Product { get; set; }

    public bool Completed { get; set; }
    public DateTime? CompletedAt { get; set; }
}
