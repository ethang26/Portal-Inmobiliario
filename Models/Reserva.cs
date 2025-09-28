using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;

namespace PortalInmobiliario.Models
{
    public class Reserva : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int InmuebleId { get; set; }
        public Inmueble? Inmueble { get; set; }

        [Required]                             // <- requerido por el enunciado
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime FechaExpiracion { get; set; }

        // Validaciones P1:
        // - FechaExpiracion > FechaCreacion
        // - Máx 1 reserva activa por inmueble (ahora < FechaExpiracion)
        public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
        {
            if (FechaExpiracion <= FechaCreacion)
                yield return new ValidationResult(
                    "FechaExpiracion debe ser posterior a FechaCreacion.",
                    new[] { nameof(FechaExpiracion) });

            var db = (ApplicationDbContext?)ctx.GetService(typeof(ApplicationDbContext));
            if (db is null) yield break;

            var now = DateTime.UtcNow;
            bool existeActiva = db.Reservas.AsNoTracking()
                .Any(r => r.InmuebleId == InmuebleId && r.Id != Id && r.FechaExpiracion > now);

            if (existeActiva)
                yield return new ValidationResult(
                    "Ya existe una reserva activa para este inmueble.",
                    new[] { nameof(InmuebleId) });
        }
    }
}