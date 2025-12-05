using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Approvisionnement
    {
        public int Id { get; set; }
        
        [Required]
        public string Reference { get; set; } = string.Empty;
        
        [Required]
        public DateTime DateApprovisionnement { get; set; }
        
        public string? Observations { get; set; }
        
        [Required]
        public string Statut { get; set; } = "En attente";
        
        public decimal MontantTotal { get; set; }
        
        [Required]
        public int FournisseurId { get; set; }
        
        public Fournisseur? Fournisseur { get; set; }
        
        public ICollection<ApprovisionnementArticle> ApprovisionnementArticles { get; set; } 
            = new List<ApprovisionnementArticle>();
    }
}