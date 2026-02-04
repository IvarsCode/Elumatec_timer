using Microsoft.EntityFrameworkCore;
using Elumatec.Tijdregistratie.Models;

namespace Elumatec.Tijdregistratie.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Medewerker> Medewerkers { get; set; } = null!;
    public DbSet<Contactpersoon> Contactpersonen { get; set; } = null!;
    public DbSet<Interventie> Interventies { get; set; } = null!;
    public DbSet<AppState> AppState { get; set; } = null!; // om o.a. recent user op te slaan

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medewerker>().ToTable("medewerkers");
        modelBuilder.Entity<Contactpersoon>().ToTable("contactpersonen");
        modelBuilder.Entity<Interventie>().ToTable("interventies");

        modelBuilder.Entity<AppState>()
            .ToTable("app_state")
            .HasKey(x => x.Key);

        modelBuilder.Entity<Interventie>()
            .HasOne(i => i.Contactpersoon)
            .WithMany(c => c.Interventies)
            .HasForeignKey(i => i.ContactpersoonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Interventie>()
            .HasOne(i => i.InterneMedewerker)
            .WithMany()
            .HasForeignKey(i => i.InterneMedewerkerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
