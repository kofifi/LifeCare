using Microsoft.AspNetCore.Identity;

namespace LifeCare.Models;

public class User : IdentityUser
{
    public string DisplayName { get; set; }
    public bool IsAdmin { get; set; }

    public ICollection<Habit> Habits { get; set; }
    public ICollection<Category> HabitCategories { get; set; }
    public UserProfile Profile { get; set; }
    public ICollection<Routine> Routines { get; set; }
    public ICollection<NutritionPlan> NutritionPlans { get; set; }
    public ICollection<WorkoutPlan> WorkoutPlans { get; set; }
}