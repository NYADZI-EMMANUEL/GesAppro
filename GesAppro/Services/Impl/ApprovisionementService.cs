using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class ApprovisionnementService : IApprovisionnementService
    {
        private readonly GesApproDbContext _context;
        
        public ApprovisionnementService(GesApproDbContext context)
        {
            _context = context;
        }
        
        public async Task<Approvisionnement> CreateAsync(Approvisionnement approvisionnement, List<ApprovisionnementArticle> articles)
        {
            Console.WriteLine("=== DÉBUT CreateAsync ===");
            Console.WriteLine($"FournisseurId: {approvisionnement.FournisseurId}");
            Console.WriteLine($"Date: {approvisionnement.DateApprovisionnement}");
            Console.WriteLine($"Nombre d'articles: {articles?.Count ?? 0}");
            
            // Vérifier que le fournisseur existe
            var fournisseurExists = await _context.Fournisseurs
                .AnyAsync(f => f.Id == approvisionnement.FournisseurId);
            
            if (!fournisseurExists)
            {
                throw new Exception($"Le fournisseur avec l'ID {approvisionnement.FournisseurId} n'existe pas.");
            }
            
            // Vérifier que tous les articles existent
            foreach (var article in articles)
            {
                var articleExists = await _context.Articles
                    .AnyAsync(a => a.Id == article.ArticleId);
                
                if (!articleExists)
                {
                    throw new Exception($"L'article avec l'ID {article.ArticleId} n'existe pas.");
                }
            }
            
            // Générer la référence
            var year = DateTime.Now.Year;
            var count = await _context.Approvisionnements
                .CountAsync(a => a.DateApprovisionnement.Year == year);
                
            approvisionnement.Reference = $"APP-{year}-{(count + 1):D3}";
            approvisionnement.Statut = "En attente";
            
            // Calculer le montant total
            decimal montantTotal = 0;
            foreach (var article in articles)
            {
                article.MontantTotal = article.Quantite * article.PrixUnitaire;
                montantTotal += article.MontantTotal;
            }
            approvisionnement.MontantTotal = montantTotal;
            
            Console.WriteLine($"Référence générée: {approvisionnement.Reference}");
            Console.WriteLine($"Montant total: {approvisionnement.MontantTotal}");
            
            // Créer un nouvel objet pour éviter les problèmes de tracking
            var nouvelApprovisionnement = new Approvisionnement
            {
                Reference = approvisionnement.Reference,
                DateApprovisionnement = approvisionnement.DateApprovisionnement,
                FournisseurId = approvisionnement.FournisseurId,
                Observations = approvisionnement.Observations ?? "",
                Statut = approvisionnement.Statut,
                MontantTotal = approvisionnement.MontantTotal
            };
            
            // Ajouter l'approvisionnement
            _context.Approvisionnements.Add(nouvelApprovisionnement);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"✓ Approvisionnement créé avec ID: {nouvelApprovisionnement.Id}");
            
            // Ajouter les articles
            foreach (var article in articles)
            {
                var nouvelArticle = new ApprovisionnementArticle
                {
                    ApprovisionnementId = nouvelApprovisionnement.Id,
                    ArticleId = article.ArticleId,
                    Quantite = article.Quantite,
                    PrixUnitaire = article.PrixUnitaire,
                    MontantTotal = article.MontantTotal
                };
                
                _context.ApprovisionnementArticles.Add(nouvelArticle);
                Console.WriteLine($"  ✓ Article ajouté: ArticleId={article.ArticleId}, Qté={article.Quantite}");
            }
            
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"✓ {articles.Count} article(s) ajouté(s)");
            Console.WriteLine("=== FIN CreateAsync ===");
            
            return nouvelApprovisionnement;
        }
        
        public async Task<IEnumerable<Approvisionnement>> GetAllAsync()
        {
            return await _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticlesNavigation)
                .OrderByDescending(a => a.DateApprovisionnement)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Approvisionnement>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            return await _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticlesNavigation)
                .Where(a => a.DateApprovisionnement >= dateDebut && a.DateApprovisionnement <= dateFin)
                .OrderByDescending(a => a.DateApprovisionnement)
                .ToListAsync();
        }
        
        public async Task<Approvisionnement> GetByIdAsync(int id)
        {
            var approvisionnement = await _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticlesNavigation)
                .ThenInclude(aa => aa.Article)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (approvisionnement == null)
                throw new InvalidOperationException($"Approvisionnement avec l'Id {id} introuvable.");

            return approvisionnement;
        }
        
        public async Task<(IEnumerable<Approvisionnement> Items, int TotalCount, int PageCount)> GetPaginatedAsync(
            int page, int pageSize, DateTime? dateDebut, DateTime? dateFin,
            string? search, int? fournisseurId, int? articleId, string? sortOrder)
        {
            // Requête de base avec les inclusions
            var query = _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticlesNavigation)
                .AsQueryable();
            
            // Appliquer les filtres de date
            if (dateDebut.HasValue)
            {
                query = query.Where(a => a.DateApprovisionnement >= dateDebut.Value);
            }
            
            if (dateFin.HasValue)
            {
                query = query.Where(a => a.DateApprovisionnement <= dateFin.Value);
            }
            
            // Appliquer le filtre de recherche
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => 
                    a.Reference.Contains(search) || 
                    (a.Observations != null && a.Observations.Contains(search)) ||
                    (a.Fournisseur != null && a.Fournisseur.Nom.Contains(search))
                );
            }
            
            // Appliquer le filtre par fournisseur
            if (fournisseurId.HasValue && fournisseurId.Value > 0)
            {
                query = query.Where(a => a.FournisseurId == fournisseurId.Value);
            }
            
            // Appliquer le filtre par article
            if (articleId.HasValue && articleId.Value > 0)
            {
                query = query.Where(a => a.ApprovisionnementArticlesNavigation != null &&
                    a.ApprovisionnementArticlesNavigation.Any(aa => aa.ArticleId == articleId.Value));
            }
            
            // Appliquer le tri
            switch (sortOrder)
            {
                case "date_asc":
                    query = query.OrderBy(a => a.DateApprovisionnement);
                    break;
                case "montant_desc":
                    query = query.OrderByDescending(a => a.MontantTotal);
                    break;
                case "montant_asc":
                    query = query.OrderBy(a => a.MontantTotal);
                    break;
                default: // "date_desc" ou null
                    query = query.OrderByDescending(a => a.DateApprovisionnement);
                    break;
            }
            
            // Compter le total avant pagination
            var totalCount = await query.CountAsync();
            
            // Calculer le nombre de pages
            var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);
            
            // Paginer les résultats
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (items, totalCount, pageCount);
        }
    }
}