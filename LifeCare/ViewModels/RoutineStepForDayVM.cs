namespace LifeCare.ViewModels;

public class RoutineStepForDayVM
{
    public int StepId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Action { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool Completed { get; set; }
    public bool Skipped { get; set; }

    public bool   RotationEnabled { get; set; }
    public string? RotationMode   { get; set; }

    public List<RoutineStepProductForDayVM> Products { get; set; } = new();
}