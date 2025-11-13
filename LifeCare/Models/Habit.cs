using LifeCare.Models;
using System.ComponentModel.DataAnnotations;

public class Habit
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }

    [Required]
    public HabitType Type { get; set; }
    public string? Unit { get; set; }
    public int? TargetQuantity { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }

    public int Order { get; set; }

    public DateTime StartDateUtc { get; set; }

    public ICollection<HabitEntry> Entries { get; set; }
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}