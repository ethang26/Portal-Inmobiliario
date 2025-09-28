using System.ComponentModel.DataAnnotations;
using PortalInmobiliario.Models;

namespace PortalInmobiliario.Models.ViewModels
{
    public class CatalogoFiltroVM : IValidatableObject
    {
        public string? Ciudad { get; set; }
        public TipoInmueble? Tipo { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "PrecioMin no puede ser negativo")]
        public decimal? PrecioMin { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "PrecioMax no puede ser negativo")]
        public decimal? PrecioMax { get; set; }

        [Range(0, 50, ErrorMessage = "Dormitorios debe ser >= 0")]
        public int? DormitoriosMin { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (PrecioMin.HasValue && PrecioMax.HasValue && PrecioMin > PrecioMax)
                yield return new ValidationResult("Rango de precios inválido (min ≤ max).",
                    new[] { nameof(PrecioMin), nameof(PrecioMax) });
        }
    }
}