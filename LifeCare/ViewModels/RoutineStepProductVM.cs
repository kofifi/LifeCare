using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels;

public class RoutineStepProductVM
{
    public int Id { get; set; }
    public int RoutineStepId { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = "";

    public string? Note { get; set; }
    public string? Url  { get; set; }

    public string? ImageUrl { get; set; }
}