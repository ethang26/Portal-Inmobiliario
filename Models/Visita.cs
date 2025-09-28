using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;

namespace PortalInmobiliario.Models
{
    public class Visita : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public int InmuebleId { get; set; }
        public Inmueble? Inmueble { get; set; }

        [Required, StringLength(100)]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        public EstadoVisita Estado { get; set; } = EstadoVisita.Solicitada;

        [StringLength(200)]
        public string? Solicitante { get; set; }

        [StringLength(200)]
        public string? Comentarios { get; set; }

        // Validaciones del enunciado:
        // - FechaInicio < FechaFin
        // - No permitir visitas solapadas para el mismo inmueble
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // 1) FechaInicio < FechaFin (requisito)
            if (FechaInicio >= FechaFin)
            {
                yield return new ValidationResult(
                    "FechaInicio debe ser menor que FechaFin.",
                    new[] { nameof(FechaInicio), nameof(FechaFin) }
                );
                yield break; // evita consultar BD si ya está mal el rango
            }

            // 2) No solapamiento (consultando la BD desde DI)
            var db = (ApplicationDbContext?)validationContext.GetService(typeof(ApplicationDbContext));
            if (db is null)
                yield break; // si no hay contexto, no podemos validar contra BD

            bool haySolape = db.Visitas
                .AsNoTracking()
                .Any(v =>
                    v.InmuebleId == InmuebleId &&
                    v.Id != Id &&
                    v.FechaInicio < FechaFin && // empieza antes de que termine la nueva
                    FechaInicio < v.FechaFin     // y la nueva empieza antes de que termine la existente
                );

            if (haySolape)
            {
                yield return new ValidationResult(
                    "No se permiten visitas solapadas para el mismo inmueble.",
                    new[] { nameof(FechaInicio), nameof(FechaFin) }
                );
            }
        }
    }
}