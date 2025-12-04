using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Article
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nom")]
        public required string Nom { get; set; }
        
    }
}