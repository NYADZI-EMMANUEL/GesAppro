using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Approvisionnement
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        [Display(Name = "Référence")]
        public string Reference { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Date d'approvisionnement")]
        [DataType(DataType.Date)]
        public DateTime DateApprovisionnement { get; set; }
        
        [Required]
        [Display(Name = "Fournisseur")]
        public int FournisseurId { get; set; }
        
        [MaxLength(1000)]
        [Display(Name = "Observations")]
        public string Observations { get; set; } = string.Empty;
        
        [MaxLength(50)]
        [Display(Name = "Statut")]
        public string Statut { get; set; } = "En attente";
        
        [Display(Name = "Montant total")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantTotal { get; set; }
        
        // Propriété pour le binding du formulaire (non mappée en base)
        [NotMapped]
        public List<ApprovisionnementArticle> ApprovisionnementArticles { get; set; } = new List<ApprovisionnementArticle>();
        
        // Navigation properties
        public virtual Fournisseur? Fournisseur { get; set; }
        public virtual ICollection<ApprovisionnementArticle>? ApprovisionnementArticlesNavigation { get; set; }
    }
}