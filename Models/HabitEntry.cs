namespace LifeCare.Models;

public class HabitEntry
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public Habit Habit { get; set; }

    public DateTime Date { get; set; }
    public bool Completed { get; set; }
}
