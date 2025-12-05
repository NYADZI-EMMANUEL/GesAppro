using System.Collections.Generic;

namespace Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public ICollection<ApprovisionnementArticle> ApprovisionnementArticles { get; set; } = new List<ApprovisionnementArticle>();
    }
}