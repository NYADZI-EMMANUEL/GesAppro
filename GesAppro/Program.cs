

using Models;
using Data;
using Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuration de DbContext sans chaîne de connexion (gérée dans OnConfiguring)
builder.Services.AddDbContext<GesApproDbContext>();

// Enregistrer les services
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
    pattern: "{controller=Approvisionnement}/{action=Index}/{id?}");

// Créer la base de données si elle n'existe pas
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GesApproDbContext>();
    try
    {
        // Vérifier si la base de données existe
        if (dbContext.Database.CanConnect())
        {
            Console.WriteLine("Connexion à la base de données réussie!");
            
            // Vérifier si les tables existent, sinon les créer
            if (!dbContext.Database.GetAppliedMigrations().Any())
            {
                dbContext.Database.EnsureCreated();
                Console.WriteLine("Tables créées avec succès!");
                
                // Insérer des données de test
                InsertTestData(dbContext);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur: {ex.Message}");
        
        // Si la base n'existe pas, essayer de la créer
        try
        {
            dbContext.Database.EnsureCreated();
            Console.WriteLine("Base de données et tables créées!");
            
            // Insérer des données de test
            InsertTestData(dbContext);
        }
        catch (Exception innerEx)
        {
            Console.WriteLine($"Erreur lors de la création: {innerEx.Message}");
        }
    }
}

void InsertTestData(GesApproDbContext context)
{
    // Vérifier si des données existent déjà
    
}

app.Run();