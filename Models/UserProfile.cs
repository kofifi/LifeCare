namespace LifeCare.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } // FK
    public User User { get; set; }

    public int? Age { get; set; }
    public string Gender { get; set; }
    public float? Weight { get; set; }
    public float? Height { get; set; }
    public string Goal { get; set; }
    public float? TargetWeight { get; set; }
    public string ActivityLevel { get; set; }
}