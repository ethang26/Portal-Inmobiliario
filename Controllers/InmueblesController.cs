using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
 // GET: /Inmuebles/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var item = await _db.Inmuebles.AsNoTracking()
                          .FirstOrDefaultAsync(i => i.Id == id && i.Activo);
            if (item is null) return NotFound();

            var now = DateTime.UtcNow;
            var puedeReservar = !await _db.Reservas.AsNoTracking()
                .AnyAsync(r => r.InmuebleId == id && r.FechaExpiracion > now);

            // default de visita: hoy 10:00–11:00
            var hoy = DateTime.Today.AddHours(10);
            var vm = new DetalleInmuebleVM
            {
                Item = item,
                PuedeReservar = puedeReservar,
                NuevaVisita = new VisitaCreateVM
                {
                    InmuebleId = id,
                    FechaInicio = hoy,
                    FechaFin = hoy.AddHours(1)
                }
            };
            return View(vm);
        }

        // POST: /Inmuebles/AgendarVisita
        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AgendarVisita(VisitaCreateVM vm)
        {
            // Validaciones de la VM (rango + horario)
            if (!ModelState.IsValid)
                return await VolverADetalleConErrores(vm);

            // Rechazar visitas solapadas (P3)
            bool haySolape = await _db.Visitas.AsNoTracking()
                .AnyAsync(v => v.InmuebleId == vm.InmuebleId &&
                               vm.FechaInicio < v.FechaFin &&
                               v.FechaInicio < vm.FechaFin);
            if (haySolape)
            {
                ModelState.AddModelError(string.Empty,
                    "No se permiten visitas solapadas para el mismo inmueble.");
                return await VolverADetalleConErrores(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var visita = new Visita
            {
                InmuebleId = vm.InmuebleId,
                UsuarioId = userId,
                FechaInicio = vm.FechaInicio,
                FechaFin = vm.FechaFin,
                Estado = EstadoVisita.Solicitada,
                Notas = vm.Notas ?? string.Empty
            };

            _db.Visitas.Add(visita);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Visita solicitada. Te confirmaremos por correo.";
            return RedirectToAction(nameof(Detalle), new { id = vm.InmuebleId });
        }

        // POST: /Inmuebles/Reservar/5
        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int id)
        {
            var now = DateTime.UtcNow;
            bool existeActiva = await _db.Reservas.AsNoTracking()
                .AnyAsync(r => r.InmuebleId == id && r.FechaExpiracion > now);
            if (existeActiva)
            {
                TempData["Error"] = "Este inmueble ya tiene una reserva activa.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reserva = new Reserva
            {
                InmuebleId = id,
                UsuarioId = userId,
                FechaCreacion = now,
                FechaExpiracion = now.AddHours(48) // P3: reserva 48h
            };

            _db.Reservas.Add(reserva);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Reserva creada por 48 horas.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // ------- helpers -------
        private async Task<IActionResult> VolverADetalleConErrores(VisitaCreateVM vm)
        {
            var item = await _db.Inmuebles.AsNoTracking()
                          .FirstOrDefaultAsync(i => i.Id == vm.InmuebleId);
            var now = DateTime.UtcNow;
            var puedeReservar = !await _db.Reservas.AsNoTracking()
                .AnyAsync(r => r.InmuebleId == vm.InmuebleId && r.FechaExpiracion > now);

            return View("Detalle", new DetalleInmuebleVM
            {
                Item = item!,
                PuedeReservar = puedeReservar,
                NuevaVisita = vm
            });
        }
    }
}