namespace LifeCare.ViewModels.Habits;

public record HabitStatsDto(
    double OverallPercent,
    int CurrentStreak,
    int BestStreak,
    int Total,
    int Completed,
    int Skipped,
    int Partial,
    DateTime StartDateUtc
);