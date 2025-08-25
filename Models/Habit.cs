using LifeCare.Models;
using System.ComponentModel.DataAnnotations;

public class Habit
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string? Description { get; set; }

    public string Color { get; set; } // np. "#FF5733"
    public string Icon { get; set; }  // np. "fa-book" albo "book.svg"
    
    [Required]
    public HabitType Type { get; set; }

    public string? Unit { get; set; } // np. "strony", "minuty" (tylko dla typu Quantity)
    public int? TargetQuantity { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }
    
    public int Order { get; set; }

    public ICollection<HabitEntry> Entries { get; set; }
}