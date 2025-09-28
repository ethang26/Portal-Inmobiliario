namespace PortalInmobiliario.Models.ViewModels
{
    public class DetalleInmuebleVM
    {
        public Inmueble Item { get; set; } = null!;
        public bool PuedeReservar { get; set; }  // no hay reserva activa
        public VisitaCreateVM NuevaVisita { get; set; } = new();
    }
}
