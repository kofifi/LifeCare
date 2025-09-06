namespace LifeCare.ViewModels;

public class RoutineForDayVM
{
    public int RoutineId { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#3b82f6";
    public string Icon { get; set; } = "fa-dumbbell";
    public TimeSpan? TimeOfDay { get; set; }

    public int TotalSteps { get; set; }
    public int DoneSteps { get; set; }
    public bool Completed { get; set; }

    public string? Description { get; set; }
    public int? CategoryId { get; set; }

    public List<RoutineStepForDayVM> Steps { get; set; } = new();
}