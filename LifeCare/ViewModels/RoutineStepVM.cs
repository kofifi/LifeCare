using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels;

public class RoutineStepVM
{
    public int Id { get; set; }
    public int RoutineId { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }
    public string? Action { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int Order { get; set; }
    public string? RRule { get; set; }

    public bool   RotationEnabled { get; set; }
    public string? RotationMode   { get; set; }

    public List<RoutineStepProductVM> Products { get; set; } = new();
}