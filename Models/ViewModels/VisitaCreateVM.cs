using System.ComponentModel.DataAnnotations;

namespace PortalInmobiliario.Models.ViewModels
{
    public class VisitaCreateVM : IValidatableObject
    {
        [Required]
        public int InmuebleId { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [StringLength(300)]
        public string? Notas { get; set; }

        // Validación de P3: rango válido y horario laboral 08:00–19:00
        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (FechaInicio >= FechaFin)
                yield return new ValidationResult("FechaInicio debe ser menor que FechaFin.",
                    new[] { nameof(FechaInicio), nameof(FechaFin) });

            if (FechaInicio.Date != FechaFin.Date)
                yield return new ValidationResult("La visita debe ser el mismo día.",
                    new[] { nameof(FechaInicio), nameof(FechaFin) });

            var inicio = FechaInicio.TimeOfDay;
            var fin    = FechaFin.TimeOfDay;
            var min = TimeSpan.FromHours(8);   // 08:00
            var max = TimeSpan.FromHours(19);  // 19:00
            if (inicio < min || fin > max)
                yield return new ValidationResult("Visitas solo en horario 08:00–19:00.",
                    new[] { nameof(FechaInicio), nameof(FechaFin) });
        }
    }
}
