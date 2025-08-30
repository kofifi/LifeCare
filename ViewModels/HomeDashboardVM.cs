using System.Collections.Generic;

namespace LifeCare.ViewModels;

public class HomeDashboardVM
{
    public int UsersCount { get; set; }
    public int HabitsCount { get; set; }
    public int CategoriesCount { get; set; }

    // Additional metrics
    public int RoutinesCount { get; set; }
    public int HabitEntriesCount { get; set; }
    public double AvgWaterIntakeLast7Days { get; set; }
    public double AvgStepsLast7Days { get; set; }

    // Data for charts
    public List<string> HabitEntriesLabels { get; set; } = new();
    public List<int> HabitEntriesData { get; set; } = new();
}
