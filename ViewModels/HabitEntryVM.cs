namespace LifeCare.ViewModels;

public class HabitEntryVM
{
    public int HabitId { get; set; }
    public DateTime Date { get; set; }
    public bool Completed { get; set; }
    public int? Quantity { get; set; }
}
