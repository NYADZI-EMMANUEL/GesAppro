using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Fournisseur
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Nom")]
        public string? Nom { get; set; }
        
    }
}