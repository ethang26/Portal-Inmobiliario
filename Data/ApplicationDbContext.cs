using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Models;

namespace PortalInmobiliario.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Inmueble> Inmuebles => Set<Inmueble>();
        public DbSet<Visita>   Visitas    => Set<Visita>();
        public DbSet<Reserva>  Reservas   => Set<Reserva>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Codigo único
            b.Entity<Inmueble>()
             .HasIndex(i => i.Codigo)
             .IsUnique();

            // Relaciones
            b.Entity<Visita>()
             .HasOne(v => v.Inmueble)
             .WithMany(i => i.Visitas)
             .HasForeignKey(v => v.InmuebleId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Reserva>()
             .HasOne(r => r.Inmueble)
             .WithMany(i => i.Reservas)
             .HasForeignKey(r => r.InmuebleId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}