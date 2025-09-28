using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PortalInmobiliario.Models
{
    public class Inmueble
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Codigo { get; set; } = string.Empty; // único

        [Required, StringLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Imagen { get; set; }

        [Required]
        public TipoInmueble Tipo { get; set; }   // enum

        [Required, StringLength(60)]
        public string Ciudad { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string Direccion { get; set; } = string.Empty;

        [Range(0, 50)]
        public int Dormitorios { get; set; }

        [Range(0, 50)]
        public int Banos { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "MetrosCuadrados debe ser > 0")]
        public int MetrosCuadrados { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Precio debe ser > 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<Visita> Visitas { get; set; } = new List<Visita>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}