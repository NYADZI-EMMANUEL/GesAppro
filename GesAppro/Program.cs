using Microsoft.EntityFrameworkCore;
using Data;
using Services;
using Services.Impl;
using Microsoft.Extensions.Logging; 

var builder = WebApplication.CreateBuilder(args);

// Configurez la journalisation
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information); // Définir le niveau minimum

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext avec MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GesApproDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add services
builder.Services.AddScoped<IApprovisionnementService, ApprovisionnementService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialiser la base de données
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>(); // Obtenez un logger
    
    try
    {
        var context = services.GetRequiredService<GesApproDbContext>();
        
        // Appliquer les migrations automatiquement
        context.Database.Migrate();
        
        logger.LogInformation("Migrations appliquées avec succès!");
        
        // Ajouter des données de test si la base est vide
        if (!context.Fournisseurs.Any())
        {
            logger.LogInformation("Ajout des fournisseurs de test...");
            context.Fournisseurs.AddRange(
                new Models.Fournisseur { Name = "Fournisseur A" },
                new Models.Fournisseur { Name = "Fournisseur B" },
                new Models.Fournisseur { Name = "Fournisseur C" }
            );
        }
        
        if (!context.Articles.Any())
        {
            logger.LogInformation("Ajout des articles de test...");
            context.Articles.AddRange(
                new Models.Article { Libelle = "Article 1" },
                new Models.Article { Libelle = "Article 2" },
                new Models.Article { Libelle = "Article 3" },
                new Models.Article { Libelle = "Article 4" },
                new Models.Article { Libelle = "Article 5" }
            );
        }
        
        context.SaveChanges();
        logger.LogInformation("Données de test ajoutées avec succès!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de l'initialisation de la base de données");
    }
}

app.Run();