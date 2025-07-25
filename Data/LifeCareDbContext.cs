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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // HABITS
        modelBuilder.Entity<Habit>()
            .HasOne(h => h.User)
            .WithMany(u => u.Habits)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // HABIT ENTRIES
        modelBuilder.Entity<HabitEntry>()
            .HasOne(e => e.Habit)
            .WithMany(h => h.Entries)
            .HasForeignKey(e => e.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        // HABIT CATEGORY (jeśli masz klasę HabitCategory)
        modelBuilder.Entity<Habit>()
            .HasOne(h => h.Category)
            .WithMany(c => c.Habits)
            .HasForeignKey(h => h.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // zapobiega błędowi multiple cascade

        modelBuilder.Entity<HabitCategory>()
            .HasOne(c => c.User)
            .WithMany(u => u.HabitCategories)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // USER PROFILE
        modelBuilder.Entity<UserProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

}