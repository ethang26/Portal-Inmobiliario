using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;

namespace PortalInmobiliario.Models
{
    public class Visita : IValidatableObject
    {
        public int Id { get; set; }

        [Required] public int InmuebleId { get; set; }
        public Inmueble? Inmueble { get; set; }

        [Required] public string UsuarioId { get; set; } = string.Empty;

        [Required] public DateTime FechaInicio { get; set; }
        [Required] public DateTime FechaFin { get; set; }

        [Column("Comentarios")]
[StringLength(300)]
public string Notas { get; set; } = string.Empty;


        public EstadoVisita Estado { get; set; } = EstadoVisita.Solicitada;

        public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
        {
            if (FechaInicio >= FechaFin)
                yield return new ValidationResult("FechaInicio debe ser menor que FechaFin.",
                    new[] { nameof(FechaInicio), nameof(FechaFin) });

            var db = (ApplicationDbContext?)ctx.GetService(typeof(ApplicationDbContext));
            if (db is null) yield break;

            bool solapa = db.Visitas.AsNoTracking()
                .Any(v => v.InmuebleId == InmuebleId &&
                          FechaInicio < v.FechaFin &&
                          v.FechaInicio < FechaFin);
            if (solapa)
                yield return new ValidationResult("No se permiten visitas solapadas para el mismo inmueble.");
        }
    }
}
