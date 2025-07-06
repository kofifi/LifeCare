using LifeCare.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LifeCare.Data;

public class LifeCareDbContext : IdentityDbContext<User>
{
    public LifeCareDbContext(DbContextOptions<LifeCareDbContext> options)
        : base(options) {}

    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitEntry> HabitEntries { get; set; }
    public DbSet<Routine> Routines { get; set; }
    public DbSet<RoutineEntry> RoutineEntries { get; set; }
    public DbSet<NutritionPlan> NutritionPlans { get; set; }
    public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
    public DbSet<DailyStats> DailyStats { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
}