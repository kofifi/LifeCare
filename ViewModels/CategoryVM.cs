using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels;

public class CategoryVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa jest wymagana")]
    public string Name { get; set; }
}