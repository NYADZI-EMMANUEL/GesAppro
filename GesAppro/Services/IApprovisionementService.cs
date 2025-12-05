using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public interface IApprovisionnementService
    {
        Task<Approvisionnement?> GetByIdAsync(int id);
        Task<List<Approvisionnement>> GetAllAsync();
        Task<Approvisionnement> CreateAsync(Approvisionnement approvisionnement);
        Task<Approvisionnement> UpdateAsync(Approvisionnement approvisionnement);
        Task DeleteAsync(int id);
        Task<List<Approvisionnement>> SearchAsync(DateTime? dateDebut, DateTime? dateFin);
        Task<List<Fournisseur>> GetFournisseursAsync();
        Task<List<Article>> GetArticlesAsync();
    }
}