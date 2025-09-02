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
    public DbSet<Category> Categories { get; set; }
    public DbSet<Routine> Routines { get; set; }
    public DbSet<RoutineStep> RoutineSteps { get; set; }
    public DbSet<RoutineEntry> RoutineEntries { get; set; }
    public DbSet<RoutineStepEntry> RoutineStepEntries { get; set; }
    public DbSet<RoutineStepProduct> RoutineStepProducts { get; set; }
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

        // CATEGORY
        modelBuilder.Entity<Habit>()
            .HasOne(h => h.Category)
            .WithMany(c => c.Habits)
            .HasForeignKey(h => h.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
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

        modelBuilder.Entity<RoutineStep>()
            .HasOne(s => s.Routine)
            .WithMany(r => r.Steps)
            .HasForeignKey(s => s.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Routine -> Entries
        modelBuilder.Entity<RoutineEntry>()
            .HasOne(e => e.Routine)
            .WithMany(r => r.Entries)
            .HasForeignKey(e => e.RoutineId)
            .OnDelete(DeleteBehavior.Cascade);

        // RoutineEntry -> StepEntries
        modelBuilder.Entity<RoutineStepEntry>()
            .HasOne(se => se.RoutineEntry)
            .WithMany(e => e.StepEntries)
            .HasForeignKey(se => se.RoutineEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // RoutineStep -> StepEntries
        modelBuilder.Entity<RoutineStepEntry>()
            .HasOne(se => se.RoutineStep)
            .WithMany(s => s.StepEntries)
            .HasForeignKey(se => se.RoutineStepId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<RoutineStepProduct>()
            .HasOne(p => p.RoutineStep)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.RoutineStepId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<RoutineStepEntry>()
            .HasMany(e => e.ProductEntries)
            .WithOne(pe => pe.StepEntry)
            .HasForeignKey(pe => pe.RoutineStepEntryId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<RoutineStepProductEntry>()
            .HasOne(pe => pe.Product)
            .WithMany(p => p.ProductEntries)
            .HasForeignKey(pe => pe.RoutineStepProductId)
            .OnDelete(DeleteBehavior.NoAction);
        
        modelBuilder.Entity<RoutineStepEntry>()
            .HasIndex(e => new { e.RoutineEntryId, e.RoutineStepId })
            .IsUnique();

        modelBuilder.Entity<RoutineStepProductEntry>()
            .HasIndex(e => new { e.RoutineStepEntryId, e.RoutineStepProductId })
            .IsUnique();
    }
}