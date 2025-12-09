using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WishesTracer.Infraestructure.Persistence;

/// <summary>
/// Factory para crear DbContext en tiempo de diseño (migrations, scaffolding, etc.)
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Connection string para diseño - DEBE coincidir con appsettings.Development.json
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=wishes_tracer;Username=wt_admin;Password=wt_@dm1n");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

