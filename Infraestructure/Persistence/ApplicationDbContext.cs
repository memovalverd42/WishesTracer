using Microsoft.EntityFrameworkCore;
using WishesTracer.Domain.Entities;

namespace WishesTracer.Infraestructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired();
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 2); // Importante para dinero
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever();
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            
            // Relación
            entity.HasOne<Product>()
                .WithMany(p => p.PriceHistory)
                .HasForeignKey(ph => ph.ProductId);
        });
        
        base.OnModelCreating(modelBuilder);
    }
}
