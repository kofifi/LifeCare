using System.ComponentModel.DataAnnotations;

namespace LifeCare.Models;

public class RoutineStep
{
    public int Id { get; set; }

    public int RoutineId { get; set; }
    public Routine Routine { get; set; } = null!;

    [Required, MaxLength(128)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public string? Action { get; set; }
    public int? EstimatedMinutes { get; set; }

    public int Order { get; set; }

    public string? RRule { get; set; }
    
    public bool RotationEnabled { get; set; }
    public string? RotationMode { get; set; }

    public ICollection<RoutineStepEntry> StepEntries { get; set; } = new List<RoutineStepEntry>();
    public ICollection<RoutineStepProduct> Products { get; set; } = new List<RoutineStepProduct>();

}