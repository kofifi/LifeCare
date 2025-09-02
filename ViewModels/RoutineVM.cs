using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels;

public class RoutineVM
{
    public int Id { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public string Icon { get; set; } = "fa-dumbbell";

    public DateTime StartDateUtc { get; set; } = DateTime.UtcNow.Date;
    public TimeSpan? TimeOfDay { get; set; }
    public string? RRule { get; set; }

    public bool ReminderEnabled { get; set; }
    public int? ReminderMinutesBefore { get; set; }

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public int Order { get; set; }

    public List<RoutineStepVM> Steps { get; set; } = new();
    
}