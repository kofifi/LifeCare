using System.ComponentModel.DataAnnotations;

namespace LifeCare.Models;

public class RoutineStepProduct
{
    public int Id { get; set; }
    public int RoutineStepId { get; set; }
    public RoutineStep RoutineStep { get; set; } = null!;

    [Required, MaxLength(128)]
    public string Name { get; set; } = "";

    public string? Note { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    
    public ICollection<RoutineStepProductEntry> ProductEntries { get; set; } = new List<RoutineStepProductEntry>();
}