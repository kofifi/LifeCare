namespace LifeCare.Models;

public class Habit
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }

    public ICollection<HabitEntry> Entries { get; set; }
}
