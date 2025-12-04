using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // AJOUTEZ CE USING
using Models;
using Services;
using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Controllers
{
    public class ApprovisionnementController : Controller
    {
        private readonly IApprovisionnementService _approvisionnementService;
        private readonly GesApproDbContext _context;
        
        public ApprovisionnementController(IApprovisionnementService approvisionnementService, GesApproDbContext context)
        {
            _approvisionnementService = approvisionnementService;
            _context = context;
        }
        
        // GET: Approvisionnement
        public async Task<IActionResult> Index(
            int? page, 
            DateTime? dateDebut, 
            DateTime? dateFin,
            string? search,
            int? fournisseurId,
            int? articleId,
            string? sortOrder)
        {
            int pageSize = 10;
            int currentPage = page ?? 1;
            
            var (items, totalCount, pageCount) = await _approvisionnementService.GetPaginatedAsync(
                currentPage, pageSize, dateDebut, dateFin, search, fournisseurId, articleId, sortOrder);
                
            ViewBag.CurrentPage = currentPage;
            ViewBag.PageCount = pageCount;
            ViewBag.TotalCount = totalCount;
            ViewBag.DateDebut = dateDebut?.ToString("yyyy-MM-dd");
            ViewBag.DateFin = dateFin?.ToString("yyyy-MM-dd");
            
            // Charger les listes pour les filtres - CORRIGEZ CES LIGNES
            ViewBag.Fournisseurs = await _context.Fournisseurs.ToListAsync();
            ViewBag.Articles = await _context.Articles.ToListAsync();
            
            return View(items);
        }
        
        // GET: Approvisionnement/Create
        public IActionResult Create()
        {
            ViewBag.Fournisseurs = new SelectList(_context.Fournisseurs, "Id", "Nom");
            ViewBag.Articles = new SelectList(_context.Articles, "Id", "Nom");
            
            // Initialiser avec un article vide
            var model = new Approvisionnement
            {
                DateApprovisionnement = DateTime.Now,
                ApprovisionnementArticles = new List<ApprovisionnementArticle>
                {
                    new ApprovisionnementArticle()
                }
            };
            
            return View(model);
        }
        
  [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Approvisionnement approvisionnement, string action)
{
    Console.WriteLine($"=== ACTION: {action} ===");
    
    // Initialiser la liste si null
    if (approvisionnement.ApprovisionnementArticles == null)
    {
        approvisionnement.ApprovisionnementArticles = new List<ApprovisionnementArticle>();
    }
    
    // Extraire les articles du formulaire
    var articlesFromForm = new List<ApprovisionnementArticle>();
    
    // Lire les données du formulaire manuellement
    var form = await Request.ReadFormAsync();
    
    // Trouver le nombre maximum d'articles
    int maxIndex = 0;
    foreach (var key in form.Keys)
    {
        if (key.StartsWith("ApprovisionnementArticles["))
        {
            var parts = key.Split('[', ']');
            if (parts.Length > 1 && int.TryParse(parts[1], out int index))
            {
                if (index > maxIndex) maxIndex = index;
            }
        }
    }
    
    // Reconstruire la liste des articles
    for (int i = 0; i <= maxIndex; i++)
    {
        var articleIdStr = form[$"ApprovisionnementArticles[{i}].ArticleId"];
        var quantiteStr = form[$"ApprovisionnementArticles[{i}].Quantite"];
        var prixStr = form[$"ApprovisionnementArticles[{i}].PrixUnitaire"];
        
        if (!string.IsNullOrEmpty(articleIdStr) && 
            !string.IsNullOrEmpty(quantiteStr) && 
            !string.IsNullOrEmpty(prixStr))
        {
            if (int.TryParse(articleIdStr, out int articleId) &&
                int.TryParse(quantiteStr, out int quantite) &&
                decimal.TryParse(prixStr, out decimal prix))
            {
                articlesFromForm.Add(new ApprovisionnementArticle
                {
                    ArticleId = articleId,
                    Quantite = quantite,
                    PrixUnitaire = prix
                });
            }
        }
    }
    
    approvisionnement.ApprovisionnementArticles = articlesFromForm;
    
    // Gérer les actions
    if (action == "addArticle")
    {
        approvisionnement.ApprovisionnementArticles.Add(new ApprovisionnementArticle());
    }
    else if (action?.StartsWith("removeArticle:") == true)
    {
        var indexStr = action.Substring("removeArticle:".Length);
        if (int.TryParse(indexStr, out int index) && index >= 0 && index < approvisionnement.ApprovisionnementArticles.Count)
        {
            approvisionnement.ApprovisionnementArticles.RemoveAt(index);
        }
    }
    else if (action == "save")
    {
        Console.WriteLine("=== TENTATIVE D'ENREGISTREMENT ===");
        Console.WriteLine($"FournisseurId: {approvisionnement.FournisseurId}");
        Console.WriteLine($"Nombre d'articles: {approvisionnement.ApprovisionnementArticles.Count}");
        
        if (ModelState.IsValid)
        {
            var validArticles = approvisionnement.ApprovisionnementArticles
                .Where(a => a.ArticleId > 0 && a.Quantite > 0 && a.PrixUnitaire > 0)
                .ToList();
            
            Console.WriteLine($"Articles valides: {validArticles.Count}");
            
            if (validArticles.Any())
            {
                try
                {
                    var result = await _approvisionnementService.CreateAsync(approvisionnement, validArticles);
                    if (result != null)
                    {
                        TempData["SuccessMessage"] = "Approvisionnement créé avec succès!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERREUR: {ex.Message}");
                    ModelState.AddModelError("", $"Erreur: {ex.Message}");
                }
            }
            else
            {
                ModelState.AddModelError("", "Veuillez ajouter au moins un article valide.");
            }
        }
        else
        {
            Console.WriteLine("MODELSTATE INVALIDE");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"- {error.ErrorMessage}");
            }
        }
    }
    
    // Recharger les données
    ViewBag.Fournisseurs = new SelectList(_context.Fournisseurs.ToList(), "Id", "Nom", approvisionnement.FournisseurId);
    ViewBag.Articles = new SelectList(_context.Articles.ToList(), "Id", "Nom");
    
    return View(approvisionnement);
}
        
        // GET: Approvisionnement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var approvisionnement = await _approvisionnementService.GetByIdAsync(id);
            if (approvisionnement == null)
            {
                return NotFound();
            }
            
            return View(approvisionnement);
        }
    }
}