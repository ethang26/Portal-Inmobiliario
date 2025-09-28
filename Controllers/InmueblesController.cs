using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;
using PortalInmobiliario.Models;
using PortalInmobiliario.Models.ViewModels;

namespace PortalInmobiliario.Controllers
{
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const int PageSize = 6;

        public InmueblesController(ApplicationDbContext db) => _db = db;

        // /Inmuebles/Catalogo
        public async Task<IActionResult> Catalogo([FromQuery] CatalogoFiltroVM filtros, int page = 1)
        {
            if (!TryValidateModel(filtros))
                return View(new CatalogoVM { Filtros = filtros });

            var q = _db.Inmuebles.AsNoTracking().Where(i => i.Activo);

            if (!string.IsNullOrWhiteSpace(filtros.Ciudad))
                q = q.Where(i => i.Ciudad.Contains(filtros.Ciudad));
            if (filtros.Tipo.HasValue)
                q = q.Where(i => i.Tipo == filtros.Tipo.Value);
            if (filtros.PrecioMin.HasValue)
                q = q.Where(i => i.Precio >= filtros.PrecioMin.Value);
            if (filtros.PrecioMax.HasValue)
                q = q.Where(i => i.Precio <= filtros.PrecioMax.Value);
            if (filtros.DormitoriosMin.HasValue)
                q = q.Where(i => i.Dormitorios >= filtros.DormitoriosMin.Value);

            var total = await q.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var items = await q.OrderBy(i => i.Ciudad).ThenBy(i => (double)i.Precio)
                               .Skip((page - 1) * PageSize)
                               .Take(PageSize)
                               .ToListAsync();

            return View(new CatalogoVM { Filtros = filtros, Items = items, Page = page, TotalPages = totalPages, TotalCount = total });
        }

        // /Inmuebles/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var item = await _db.Inmuebles.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.Activo);
            if (item == null) return NotFound();
            return View(item);
        }
    }
}