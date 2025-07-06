using LifeCare.Models;

public class DailyStats
{
    public int Id { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }

    public DateTime Date { get; set; }
    public float WaterIntakeLiters { get; set; }
    public int Steps { get; set; }
    public int Calories { get; set; }
}