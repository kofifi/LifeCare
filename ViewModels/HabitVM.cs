using System.ComponentModel.DataAnnotations;
using LifeCare.Models;

namespace LifeCare.ViewModels
{
    public class HabitVM
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
        public int? CategoryId { get; set; }
        public int Order { get; set; }

        public DateTime StartDateUtc { get; set; }
    }
}