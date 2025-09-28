using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Models;

namespace PortalInmobiliario.Data
{
    public static class Seed
    {
        public static async Task RunAsync(ApplicationDbContext db)
        {
            await db.Database.MigrateAsync();

            if (await db.Inmuebles.AnyAsync())
                return;

            var inmuebles = new List<Inmueble>
            {
                new Inmueble
                {
                    Codigo = "DEP-001",
                    Titulo = "Departamento céntrico",
                    Imagen = "/img/depto1.jpg",
                    Tipo = TipoInmueble.Departamento,
                    Ciudad = "Santiago",
                    Direccion = "Av. Alameda 123",
                    Dormitorios = 2,
                    Banos = 1,
                    MetrosCuadrados = 55,
                    Precio = 120_000_000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "CAS-002",
                    Titulo = "Casa con patio",
                    Imagen = "/img/casa1.jpg",
                    Tipo = TipoInmueble.Casa,
                    Ciudad = "Valparaíso",
                    Direccion = "Calle Cerro 456",
                    Dormitorios = 3,
                    Banos = 2,
                    MetrosCuadrados = 120,
                    Precio = 220_000_000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "OFI-003",
                    Titulo = "Oficina en centro financiero",
                    Imagen = "/img/ofi1.jpg",
                    Tipo = TipoInmueble.Oficina,
                    Ciudad = "Santiago",
                    Direccion = "Isidora Goyenechea 789",
                    Dormitorios = 0,
                    Banos = 1,
                    MetrosCuadrados = 80,
                    Precio = 350_000_000m,
                    Activo = true
                },
                new Inmueble
                {
                    Codigo = "LOC-004",
                    Titulo = "Local comercial alto flujo",
                    Imagen = "/img/local1.jpg",
                    Tipo = TipoInmueble.Local,
                    Ciudad = "Concepción",
                    Direccion = "Barros Arana 1010",
                    Dormitorios = 0,
                    Banos = 1,
                    MetrosCuadrados = 65,
                    Precio = 180_000_000m,
                    Activo = true
                }
            };

            db.Inmuebles.AddRange(inmuebles);
            await db.SaveChangesAsync();
        }
    }
}
