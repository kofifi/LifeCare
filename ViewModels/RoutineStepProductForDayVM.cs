namespace LifeCare.ViewModels;

public class RoutineStepProductForDayVM
{
    public int ProductId { get; set; }

    public string Name { get; set; } = "";
    public string? Note { get; set; }
    public string? Url  { get; set; }
    public string? ImageUrl { get; set; }

    public bool Completed { get; set; }
}