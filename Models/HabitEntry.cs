public class HabitEntry
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit Habit { get; set; }

    public DateTime Date { get; set; }

    // Obsługa dwóch typów wpisu:
    public bool? Completed { get; set; } // jeśli Habit.Type == Checkbox
    public float? Quantity { get; set; } // jeśli Habit.Type == Quantity
}