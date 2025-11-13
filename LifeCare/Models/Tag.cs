namespace LifeCare.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;

    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public ICollection<Routine> Routines { get; set; } = new List<Routine>();
}