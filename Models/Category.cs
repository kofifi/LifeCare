using System.ComponentModel.DataAnnotations;

namespace LifeCare.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
    public string UserId { get; set; } // użytkownik, który utworzył kategorię
    public User User { get; set; }

    public ICollection<Habit> Habits { get; set; }
}