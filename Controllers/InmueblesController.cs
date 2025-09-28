using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PortalInmobiliario.Data;
using PortalInmobiliario.Models;
using PortalInmobiliario.Models.ViewModels;

namespace PortalInmobiliario.Controllers
{
    public class InmueblesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IDistributedCache _cache;
        private const int PageSize = 6;

        public InmueblesController(ApplicationDbContext db, IDistributedCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // ------- Claves de Sesión -------
        private const string FiltrosKey = "CATALOGO:FILTROS";
        private const string LastItemIdKey = "CATALOGO:LAST_ID";
        private const string LastItemTitleKey = "CATALOGO:LAST_TIT";

        private void SaveFiltersToSession(CatalogoFiltroVM f)
            => HttpContext.Session.SetString(FiltrosKey, JsonSerializer.Serialize(f));

        private CatalogoFiltroVM? ReadFiltersFromSession()
        {
            var s = HttpContext.Session.GetString(FiltrosKey);
            return string.IsNullOrEmpty(s) ? null : JsonSerializer.Deserialize<CatalogoFiltroVM>(s);
        }

        private static string CacheKey(CatalogoFiltroVM f, int page)
        {
            string pm = f.PrecioMin?.ToString(CultureInfo.InvariantCulture) ?? "";
            string pM = f.PrecioMax?.ToString(CultureInfo.InvariantCulture) ?? "";
            string tipo = f.Tipo.HasValue ? ((int)f.Tipo.Value).ToString() : "";
            string dorm = f.DormitoriosMin?.ToString() ?? "";
            return $"catalog:{f.Ciudad}:{tipo}:{pm}:{pM}:{dorm}:p{page}";
        }

        // GET: /Inmuebles/Catalogo
        public async Task<IActionResult> Catalogo([FromQuery(Name = "Filtros")] CatalogoFiltroVM filtros, int page = 1)
        {
            // 1) Limpiar filtros si viene clear=1
            if (Request.Query.TryGetValue("clear", out var clear) && clear == "1")
            {
                HttpContext.Session.Remove(FiltrosKey);
                filtros = new CatalogoFiltroVM();
                ModelState.Clear();
            }

            // 2) Si no hay filtros en query y existe sesión previa, recupérala
            bool filtrosVacios = string.IsNullOrWhiteSpace(filtros.Ciudad)
                                 && !filtros.Tipo.HasValue && !filtros.PrecioMin.HasValue
                                 && !filtros.PrecioMax.HasValue && !filtros.DormitoriosMin.HasValue;

            if (filtrosVacios)
            {
                var last = ReadFiltersFromSession();
                if (last is not null) filtros = last;
            }

            // Query base (aplica filtros)
            var q = _db.Inmuebles.AsNoTracking().Where(i => i.Activo);
            if (!string.IsNullOrWhiteSpace(filtros.Ciudad))
                q = q.Where(i => i.Ciudad.Contains(filtros.Ciudad));
            if (filtros.Tipo.HasValue)
                q = q.Where(i => i.Tipo == filtros.Tipo);
            if (filtros.PrecioMin.HasValue)
                q = q.Where(i => i.Precio >= filtros.PrecioMin.Value);
            if (filtros.PrecioMax.HasValue)
                q = q.Where(i => i.Precio <= filtros.PrecioMax.Value);
            if (filtros.DormitoriosMin.HasValue)
                q = q.Where(i => i.Dormitorios >= filtros.DormitoriosMin.Value);

            // --- Caché 60s por filtros+page ---
            var key = CacheKey(filtros, page);
            CatalogoVM? vm = null;

            var cached = await _cache.GetStringAsync(key);
            if (cached is not null)
                vm = JsonSerializer.Deserialize<CatalogoVM>(cached);

            if (vm is null)
            {
                var total = await q.CountAsync();
                var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
                page = Math.Clamp(page, 1, totalPages);

                var items = await q.OrderBy(i => i.Ciudad)
                                   .ThenBy(i => (double)i.Precio) // SQLite: decimal -> double
                                   .Skip((page - 1) * PageSize)
                                   .Take(PageSize)
                                   .Select(i => new Inmueble
                                   {
                                       Id = i.Id, Codigo = i.Codigo, Titulo = i.Titulo, Imagen = i.Imagen,
                                       Tipo = i.Tipo, Ciudad = i.Ciudad, Direccion = i.Direccion,
                                       Dormitorios = i.Dormitorios, Banos = i.Banos,
                                       MetrosCuadrados = i.MetrosCuadrados, Precio = i.Precio, Activo = i.Activo
                                   })
                                   .ToListAsync();

                vm = new CatalogoVM
                {
                    Filtros = filtros,
                    Items = items,
                    Page = page,
                    TotalPages = totalPages,
                    TotalCount = total
                };

                await _cache.SetStringAsync(
                    key,
                    JsonSerializer.Serialize(vm),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                    });
            }

            // Guarda filtros usados para próximas visitas
            SaveFiltersToSession(filtros);
            return View(vm);
        }

        // GET: /Inmuebles/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var item = await _db.Inmuebles.AsNoTracking()
                          .FirstOrDefaultAsync(i => i.Id == id && i.Activo);
            if (item is null) return NotFound();

            // Guarda "último visitado" para el atajo del layout
            HttpContext.Session.SetInt32(LastItemIdKey, id);
            HttpContext.Session.SetString(LastItemTitleKey, item.Titulo);

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
            if (!ModelState.IsValid)
                return await VolverADetalleConErrores(vm);

            // no solapadas
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
                FechaExpiracion = now.AddHours(48)
            };

            _db.Reservas.Add(reserva);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Reserva creada por 48 horas.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // ------- helper -------
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
