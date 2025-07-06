namespace LifeCare.Models;

public class Routine
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }

    public ICollection<RoutineEntry> Entries { get; set; }
}
