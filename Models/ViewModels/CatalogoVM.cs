namespace PortalInmobiliario.Models.ViewModels
{
    public class CatalogoVM
    {
        public CatalogoFiltroVM Filtros { get; set; } = new();
        public IEnumerable<Inmueble> Items { get; set; } = new List<Inmueble>();
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}