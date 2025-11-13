namespace LifeCare.Models;

public class WorkoutPlan
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Exercises { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }

    public DateTime CreatedDate { get; set; }
}
