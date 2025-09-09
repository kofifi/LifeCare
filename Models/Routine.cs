using System.ComponentModel.DataAnnotations;

namespace LifeCare.Models
{
    public class Routine
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }
        public string Color { get; set; } = "";
        public string Icon { get; set; } = "";

        public string UserId { get; set; } = "";
        public User User { get; set; } = null!;

        public int Order { get; set; }

        public DateTime StartDateUtc { get; set; }
        public TimeSpan? TimeOfDay { get; set; }
        public string? RRule { get; set; }

        public bool ReminderEnabled { get; set; }
        public int? ReminderMinutesBefore { get; set; }

        public ICollection<RoutineStep> Steps { get; set; } = new List<RoutineStep>();
        public ICollection<RoutineEntry> Entries { get; set; } = new List<RoutineEntry>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}