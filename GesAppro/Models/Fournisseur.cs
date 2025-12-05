using System.Collections.Generic;

namespace Models
{
    public class Fournisseur
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Approvisionnement> Approvisionnements { get; set; } = new List<Approvisionnement>();
    }
}