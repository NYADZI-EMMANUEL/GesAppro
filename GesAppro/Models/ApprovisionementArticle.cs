using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class ApprovisionnementArticle
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Approvisionnement")]
        public int ApprovisionnementId { get; set; }
        
        [Required]
        [Display(Name = "Article")]
        public int ArticleId { get; set; }
        
        [Required]
        [Display(Name = "Quantité")]
        [Range(1, int.MaxValue)]
        public int Quantite { get; set; }
        
        [Required]
        [Display(Name = "Prix unitaire")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
        
        [Display(Name = "Montant total")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantTotal { get; set; }
        
        // Navigation properties
        public virtual Approvisionnement? Approvisionnement { get; set; }
        public virtual Article? Article { get; set; }
    }
}