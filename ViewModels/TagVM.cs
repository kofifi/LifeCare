using System.ComponentModel.DataAnnotations;

namespace LifeCare.ViewModels
{
    public class TagVM
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = "";
    }
}