using Microsoft.EntityFrameworkCore;
using Models;

namespace Data
{
    public class GesApproDbContext : DbContext
    {
        public DbSet<Approvisionnement> Approvisionnements { get; set; } = null!;
        public DbSet<ApprovisionnementArticle> ApprovisionnementArticles { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<Fournisseur> Fournisseurs { get; set; } = null!;
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Table Approvisionnements
            modelBuilder.Entity<Approvisionnement>(entity =>
            {
                entity.ToTable("approvisionnements");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reference)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.DateApprovisionnement)
                    .IsRequired();
                entity.Property(e => e.Statut)
                    .HasMaxLength(50)
                    .HasDefaultValue("En attente");
                entity.Property(e => e.MontantTotal)
                    .HasPrecision(18, 2)
                    .HasDefaultValue(0);
                entity.Property(e => e.Observations)
                    .HasMaxLength(1000);
                
                // Relation avec Fournisseur
                entity.HasOne(a => a.Fournisseur)
                    .WithMany()
                    .HasForeignKey(a => a.FournisseurId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Relation avec ApprovisionnementArticles
                entity.HasMany(a => a.ApprovisionnementArticlesNavigation)
                    .WithOne(aa => aa.Approvisionnement)
                    .HasForeignKey(aa => aa.ApprovisionnementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Table Articles
            modelBuilder.Entity<Article>(entity =>
            {
                entity.ToTable("articles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom)
                    .IsRequired()
                    .HasMaxLength(200);
            });
            
            // Table Fournisseurs
            modelBuilder.Entity<Fournisseur>(entity =>
            {
                entity.ToTable("fournisseurs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom);
            });
            
            // Table ApprovisionnementArticles
            modelBuilder.Entity<ApprovisionnementArticle>(entity =>
            {
                entity.ToTable("approvisionnement_articles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Quantite)
                    .IsRequired();
                entity.Property(e => e.PrixUnitaire)
                    .HasPrecision(18, 2)
                    .IsRequired();
                entity.Property(e => e.MontantTotal)
                    .HasPrecision(18, 2);
                
                // Relation avec Article
                entity.HasOne(aa => aa.Article)
                    .WithMany()
                    .HasForeignKey(aa => aa.ArticleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(
                    "Server=localhost;Port=3306;Database=GesApproDB;User=root;Password=;",
                    new MySqlServerVersion(new Version(8, 0, 40))
                );
                
                // Activez le logging pour débugger
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            }
        }
    }
}