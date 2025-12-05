using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 
using Models;
using Data;

namespace Services.Impl
{
    public class ApprovisionnementService : IApprovisionnementService
    {
        private readonly GesApproDbContext _context;
        
        public ApprovisionnementService(GesApproDbContext context)
        {
            _context = context;
        }
        
        public async Task<Approvisionnement?> GetByIdAsync(int id)
        {
            return await _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticles)
                    .ThenInclude(aa => aa.Article)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        
        public async Task<List<Approvisionnement>> GetAllAsync()
        {
            return await _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticles)
                .OrderByDescending(a => a.DateApprovisionnement)
                .ToListAsync();
        }
        
        // Dans ApprovisionnementService.cs
        public async Task<Approvisionnement> CreateAsync(Approvisionnement approvisionnement)
        {
            // 1. Générer la référence automatiquement
            string nouvelleReference = await GenererReferenceAutomatiqueAsync();
            approvisionnement.Reference = nouvelleReference;
            
            // 2. Calculer le montant total mais NE PAS assigner ApprovisionnementId encore
            if (approvisionnement.ApprovisionnementArticles != null && approvisionnement.ApprovisionnementArticles.Any())
            {
                // Calculer le montant pour chaque article
                foreach (var article in approvisionnement.ApprovisionnementArticles)
                {
                    // Vérifier que l'article est valide
                    if (article.ArticleId <= 0 || article.Quantite <= 0)
                    {
                        continue; // Ignorer les articles invalides
                    }
                    
                    article.Montant = article.Quantite * article.PrixUnitaire;
                    // NE PAS assigner ApprovisionnementId ici ! Il est encore 0
                }
                
                // Calculer le total
                approvisionnement.MontantTotal = approvisionnement.ApprovisionnementArticles
                    .Where(aa => aa.ArticleId > 0 && aa.Quantite > 0)
                    .Sum(aa => aa.Montant);
            }
            else
            {
                approvisionnement.MontantTotal = 0;
            }
            
            // 3. Enregistrer d'abord l'approvisionnement (sans les articles)
            _context.Approvisionnements.Add(approvisionnement);
            await _context.SaveChangesAsync(); // Maintenant approvisionnement.Id a une valeur!
            
            // 4. Maintenant assigner ApprovisionnementId aux articles et les enregistrer
            if (approvisionnement.ApprovisionnementArticles != null)
            {
                foreach (var article in approvisionnement.ApprovisionnementArticles)
                {
                    // Sauter les articles invalides
                    if (article.ArticleId <= 0 || article.Quantite <= 0)
                        continue;
                        
                    // Maintenant approvisionnement.Id est disponible
                    article.ApprovisionnementId = approvisionnement.Id;
                    
                    // Ajouter l'article au contexte
                    _context.ApprovisionnementArticles.Add(article);
                }
                
                // Enregistrer les articles
                await _context.SaveChangesAsync();
            }
            
            return approvisionnement;
        }

        // Méthode pour générer la référence automatiquement
        private async Task<string> GenererReferenceAutomatiqueAsync()
        {
            try
            {
                // Récupérer la dernière référence
                var derniereReference = await _context.Approvisionnements
                    .OrderByDescending(a => a.Id)
                    .Select(a => a.Reference)
                    .FirstOrDefaultAsync();
                
                int prochainNumero = 1;
                
                if (!string.IsNullOrEmpty(derniereReference))
                {
                    // Extraire le numéro de la dernière référence
                    string prefixe = "APP";
                    
                    if (derniereReference.StartsWith(prefixe))
                    {
                        // Essayer d'extraire le numéro après le préfixe
                        string numeroStr = derniereReference.Substring(prefixe.Length);
                        
                        if (int.TryParse(numeroStr, out int dernierNumero))
                        {
                            prochainNumero = dernierNumero + 1;
                        }
                        else
                        {
                            // Si on ne peut pas parser le numéro, chercher le dernier ID
                            var dernierId = await _context.Approvisionnements
                                .OrderByDescending(a => a.Id)
                                .Select(a => a.Id)
                                .FirstOrDefaultAsync();
                            
                            prochainNumero = dernierId + 1;
                        }
                    }
                    else
                    {
                        // Si la référence ne commence pas par APP, utiliser le dernier ID
                        var dernierId = await _context.Approvisionnements
                            .OrderByDescending(a => a.Id)
                            .Select(a => a.Id)
                            .FirstOrDefaultAsync();
                        
                        prochainNumero = dernierId + 1;
                    }
                }
                else
                {
                    // Si aucune référence n'existe, commencer à 1
                    prochainNumero = 1;
                }
                
                // Formater la référence: APP0001, APP0002, etc.
                return $"APP{prochainNumero:D4}";
            }
            catch (Exception ex)
            {
                
                // Fallback: utiliser timestamp pour éviter les doublons
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                return $"APP-{timestamp}";
            }
        }
        
        public async Task<Approvisionnement> UpdateAsync(Approvisionnement approvisionnement)
        {
            // Supprimer les anciens articles
            var existingArticles = await _context.ApprovisionnementArticles
                .Where(aa => aa.ApprovisionnementId == approvisionnement.Id)
                .ToListAsync();
            _context.ApprovisionnementArticles.RemoveRange(existingArticles);
            
            // Recalculer le montant total
            if (approvisionnement.ApprovisionnementArticles != null && approvisionnement.ApprovisionnementArticles.Any())
            {
                approvisionnement.MontantTotal = approvisionnement.ApprovisionnementArticles
                    .Where(aa => aa.Quantite > 0 && aa.PrixUnitaire > 0)
                    .Sum(aa => aa.Quantite * aa.PrixUnitaire);
                    
                // Calculer le montant pour chaque article
                foreach (var article in approvisionnement.ApprovisionnementArticles)
                {
                    article.Montant = article.Quantite * article.PrixUnitaire;
                    article.ApprovisionnementId = approvisionnement.Id;
                }
            }
            
            _context.Approvisionnements.Update(approvisionnement);
            await _context.SaveChangesAsync();
            
            return approvisionnement;
        }
        
        public async Task DeleteAsync(int id)
        {
            var approvisionnement = await _context.Approvisionnements.FindAsync(id);
            if (approvisionnement != null)
            {
                _context.Approvisionnements.Remove(approvisionnement);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<List<Approvisionnement>> SearchAsync(DateTime? dateDebut, DateTime? dateFin)
        {
            var query = _context.Approvisionnements
                .Include(a => a.Fournisseur)
                .Include(a => a.ApprovisionnementArticles)
                .AsQueryable();
                
            if (dateDebut.HasValue)
            {
                query = query.Where(a => a.DateApprovisionnement >= dateDebut.Value);
            }
            
            if (dateFin.HasValue)
            {
                query = query.Where(a => a.DateApprovisionnement <= dateFin.Value);
            }
            
            return await query.OrderByDescending(a => a.DateApprovisionnement).ToListAsync();
        }
        
        public async Task<List<Fournisseur>> GetFournisseursAsync()
        {
            return await _context.Fournisseurs
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
        
        public async Task<List<Article>> GetArticlesAsync()
        {
            return await _context.Articles
                .OrderBy(a => a.Libelle)
                .ToListAsync();
        }
    }
}