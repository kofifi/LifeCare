using LifeCare.Models;
using System.ComponentModel.DataAnnotations;

public class HabitCategory
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string Color { get; set; } // np. "#00BFFF"
    public string Icon { get; set; }  // np. "fa-heart", "heart.svg"

    public string UserId { get; set; } // użytkownik, który utworzył kategorię
    public User User { get; set; }

    public ICollection<Habit> Habits { get; set; }
}