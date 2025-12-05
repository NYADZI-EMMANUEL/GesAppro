using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging; // Ajoutez ce using
using Models;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GesAppro.Controllers
{
    public class ApprovisionnementController : Controller
    {
        private readonly IApprovisionnementService _service;
        private readonly ILogger<ApprovisionnementController> _logger; // Ajoutez cette ligne
        
        public ApprovisionnementController(IApprovisionnementService service, ILogger<ApprovisionnementController> logger) // Modifiez le constructeur
        {
            _service = service;
            _logger = logger; // Ajoutez cette ligne
        }
        
        // GET: Approvisionnement
        public async Task<IActionResult> Index(string? dateDebut = null, string? dateFin = null, int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Chargement de la liste des approvisionnements");
                
                DateTime? debut = null;
                DateTime? fin = null;
                
                if (!string.IsNullOrEmpty(dateDebut) && DateTime.TryParse(dateDebut, out DateTime dDebut))
                {
                    debut = dDebut;
                    ViewBag.DateDebut = dDebut.ToString("yyyy-MM-dd");
                }
                else
                {
                    ViewBag.DateDebut = "";
                }
                
                if (!string.IsNullOrEmpty(dateFin) && DateTime.TryParse(dateFin, out DateTime dFin))
                {
                    fin = dFin;
                    ViewBag.DateFin = dFin.ToString("yyyy-MM-dd");
                }
                else
                {
                    ViewBag.DateFin = "";
                }
                
                var approvisionnements = await _service.SearchAsync(debut, fin);
                
                // Pagination
                var totalItems = approvisionnements.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                var pagedItems = approvisionnements
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                    
                ViewBag.CurrentPage = page;
                ViewBag.PageCount = totalPages;
                ViewBag.PageSize = pageSize;
                
                _logger.LogInformation($"Chargement réussi: {approvisionnements.Count} approvisionnements trouvés");
                
                return View(pagedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des approvisionnements");
                TempData["ErrorMessage"] = "Erreur lors du chargement des données.";
                return View(new List<Approvisionnement>());
            }
        }
        
        // GET: Approvisionnement/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Chargement du formulaire de création");
                
                var fournisseurs = await _service.GetFournisseursAsync();
                var articles = await _service.GetArticlesAsync();
                
                ViewBag.Fournisseurs = new SelectList(fournisseurs, "Id", "Name");
                ViewBag.Articles = new SelectList(articles, "Id", "Libelle");
                
                var model = new Approvisionnement
                {
                    DateApprovisionnement = DateTime.Now,
                    ApprovisionnementArticles = new List<ApprovisionnementArticle>
                    {
                        new ApprovisionnementArticle { Quantite = 1, PrixUnitaire = 0, Montant = 0, ArticleId = 0 }
                    }
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du formulaire de création");
                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // POST: Approvisionnement/Create
        // Dans ApprovisionnementController.cs - méthode Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Approvisionnement approvisionnement, string? action)
        {
            _logger.LogInformation("Tentative de création d'un approvisionnement. Action: {Action}", action);
            
            // Gestion du bouton "Ajouter un article"
            if (action == "addArticle")
            {
                _logger.LogDebug("Ajout d'un nouvel article au formulaire");
                
                approvisionnement.ApprovisionnementArticles ??= new List<ApprovisionnementArticle>();
                approvisionnement.ApprovisionnementArticles.ToList().Add(new ApprovisionnementArticle { 
                    Quantite = 1, 
                    PrixUnitaire = 0, 
                    Montant = 0,
                    ArticleId = 0
                });
                ModelState.Clear();
                await PopulateViewBags();
                return View(approvisionnement);
            }
            
            // Gestion du bouton "Supprimer un article"
            if (!string.IsNullOrEmpty(action) && action.StartsWith("removeArticle_"))
            {
                int index = int.Parse(action.Substring("removeArticle_".Length));
                _logger.LogDebug("Suppression de l'article à l'index {Index}", index);
                
                if (approvisionnement.ApprovisionnementArticles != null)
                {
                    var list = approvisionnement.ApprovisionnementArticles.ToList();
                    if (index < list.Count)
                    {
                        list.RemoveAt(index);
                        approvisionnement.ApprovisionnementArticles = list;
                    }
                }
                ModelState.Clear();
                await PopulateViewBags();
                return View(approvisionnement);
            }
            
            // Gestion du bouton "Enregistrer"
            if (action == "save")
            {
                _logger.LogInformation("Enregistrement de l'approvisionnement");
                
                // IMPORTANT: Supprimer l'erreur de validation pour le champ Reference
                // car il est généré automatiquement par le service
                ModelState.Remove("Reference");
                
                // Réévaluer la validité du modèle après avoir supprimé l'erreur de Reference
                bool isModelValid = ModelState.IsValid;
                
                if (!isModelValid)
                {
                    _logger.LogWarning("Modèle invalide. Erreurs: {@Errors}", 
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    
                    ViewBag.ErrorMessage = "Veuillez corriger les erreurs dans le formulaire.";
                    await PopulateViewBags();
                    return View(approvisionnement);
                }
                
                // Vérifications supplémentaires
                if (approvisionnement.FournisseurId <= 0)
                {
                    _logger.LogWarning("Fournisseur non sélectionné");
                    ModelState.AddModelError("FournisseurId", "Veuillez sélectionner un fournisseur.");
                    ViewBag.ErrorMessage = "Veuillez sélectionner un fournisseur.";
                    await PopulateViewBags();
                    return View(approvisionnement);
                }
                
                // Vérifier qu'il y a au moins un article valide
                bool hasValidArticles = false;
                if (approvisionnement.ApprovisionnementArticles != null)
                {
                    hasValidArticles = approvisionnement.ApprovisionnementArticles
                        .Any(a => a.ArticleId > 0 && a.Quantite > 0);
                }
                
                if (!hasValidArticles)
                {
                    _logger.LogWarning("Aucun article valide dans l'approvisionnement");
                    ModelState.AddModelError("", "Veuillez ajouter au moins un article valide.");
                    ViewBag.ErrorMessage = "Veuillez ajouter au moins un article valide.";
                    await PopulateViewBags();
                    return View(approvisionnement);
                }
                
                try
                {
                    _logger.LogDebug("Appel du service CreateAsync");
                    
                    // S'assurer que la date est définie
                    if (approvisionnement.DateApprovisionnement == default)
                    {
                        approvisionnement.DateApprovisionnement = DateTime.Now;
                    }
                    
                    // S'assurer que le statut est défini
                    if (string.IsNullOrEmpty(approvisionnement.Statut))
                    {
                        approvisionnement.Statut = "En attente";
                    }
                    
                    // Nettoyer le champ Reference (le service le regénérera)
                    approvisionnement.Reference = string.Empty;
                    
                    // Filtrer les articles invalides
                    if (approvisionnement.ApprovisionnementArticles != null)
                    {
                        var validArticles = approvisionnement.ApprovisionnementArticles
                            .Where(a => a.ArticleId > 0 && a.Quantite > 0)
                            .ToList();
                        
                        approvisionnement.ApprovisionnementArticles = validArticles;
                        _logger.LogDebug("{Count} articles valides après filtrage", validArticles.Count);
                    }
                    
                    // Appeler le service
                    var result = await _service.CreateAsync(approvisionnement);
                    
                    _logger.LogInformation("Approvisionnement créé avec succès. ID: {Id}, Référence: {Reference}", 
                        result.Id, result.Reference);
                    
                    TempData["SuccessMessage"] = $"Approvisionnement {result.Reference} créé avec succès!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Messages d'erreur utilisateur
                    string errorMessage = "";
                    
                    if (ex.Message.Contains("foreign key"))
                    {
                        errorMessage = "Erreur de référence. Vérifiez que le fournisseur et les articles existent.";
                    }
                    else if (ex.Message.Contains("cannot be null"))
                    {
                        errorMessage = "Une valeur requise est manquante.";
                    }
                    else if (ex.Message.Contains("duplicate"))
                    {
                        errorMessage = "Cet enregistrement existe déjà.";
                    }
                    
                    _logger.LogError(ex, "Erreur lors de la création de l'approvisionnement. Message: {Message}", errorMessage);
                    
                    
                    return View(approvisionnement);
                }
            }
            
            await PopulateViewBags();
            return View(approvisionnement);
        }
        
        // POST: Approvisionnement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Tentative de suppression de l'approvisionnement ID: {Id}", id);
            
            try
            {
                await _service.DeleteAsync(id);
                _logger.LogInformation("Approvisionnement ID: {Id} supprimé avec succès", id);
                TempData["SuccessMessage"] = "Approvisionnement supprimé avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'approvisionnement ID: {Id}", id);
                TempData["ErrorMessage"] = "Erreur lors de la suppression: " + ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        private async Task PopulateViewBags()
        {
            try
            {
                var fournisseurs = await _service.GetFournisseursAsync();
                var articles = await _service.GetArticlesAsync();
                
                ViewBag.Fournisseurs = new SelectList(fournisseurs, "Id", "Name");
                ViewBag.Articles = new SelectList(articles, "Id", "Libelle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des données pour les ViewBags");
                ViewBag.Fournisseurs = new SelectList(new List<Fournisseur>(), "Id", "Name");
                ViewBag.Articles = new SelectList(new List<Article>(), "Id", "Libelle");
            }
        }
    }
}