namespace LifeCare.Models;

public class RoutineEntry
{
    public int Id { get; set; }
    public int RoutineId { get; set; }
    public Routine Routine { get; set; }

    public DateTime ScheduledDate { get; set; }
    public bool Done { get; set; }
}
