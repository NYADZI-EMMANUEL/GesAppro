using Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IApprovisionnementService
    {
        Task<Approvisionnement> CreateAsync(Approvisionnement approvisionnement, List<ApprovisionnementArticle> articles);
        Task<IEnumerable<Approvisionnement>> GetAllAsync();
        Task<IEnumerable<Approvisionnement>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin);
        Task<Approvisionnement> GetByIdAsync(int id);
        Task<(IEnumerable<Approvisionnement> Items, int TotalCount, int PageCount)> GetPaginatedAsync(
            int page, int pageSize, DateTime? dateDebut, DateTime? dateFin,
            string? search, int? fournisseurId, int? articleId, string? sortOrder);
    }
}