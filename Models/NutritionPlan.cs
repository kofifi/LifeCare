namespace LifeCare.Models;

public class NutritionPlan
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }

    public int CaloriesTarget { get; set; }
    public float Protein { get; set; }
    public float Carbs { get; set; }
    public float Fat { get; set; }

    public DateTime CreatedDate { get; set; }
}
