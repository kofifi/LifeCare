namespace LifeCare.ViewModels
{
    public class HomeDashboardVM
    {
        public DateOnly SelectedDate { get; set; }

        public List<HabitDashboardItemVM> Habits { get; set; } = new();
        public List<RoutineDashboardItemVM> Routines { get; set; } = new();

        public int TasksCount { get; set; }
        public int OverallCompletionPercentage { get; set; }
        public int CurrentStreak { get; set; }
    }

    public class HabitDashboardItemVM
    {
        public int HabitId { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#cccccc";
        public string Icon { get; set; } = "fa-circle";

        public bool IsQuantityType { get; set; }
        public decimal? TargetQuantity { get; set; }
        public string? Unit { get; set; }
        public decimal DoneQuantity { get; set; }
        public bool IsCompleted { get; set; }

        public List<int> SelectedTagIds { get; set; } = new();
    }

    public class RoutineDashboardItemVM
    {
        public int RoutineId { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#cccccc";

        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public bool IsCompleted => TotalSteps > 0 && CompletedSteps >= TotalSteps;

        public List<int> SelectedTagIds { get; set; } = new();
    }
}