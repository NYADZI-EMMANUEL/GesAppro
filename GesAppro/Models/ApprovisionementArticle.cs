using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ApprovisionnementArticle
    {
        public int Id { get; set; }
        
        [Required]
        public int Quantite { get; set; } = 1;
        
        [Required]
        public decimal PrixUnitaire { get; set; } = 0;
        
        public decimal Montant { get; set; }
    
        public int ApprovisionnementId { get; set; }
        public Approvisionnement? Approvisionnement { get; set; }
        
        [Required]
        public int ArticleId { get; set; }
        public Article? Article { get; set; }
    }
}