using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalInmobiliario.Data;
using PortalInmobiliario.Models;

namespace PortalInmobiliario.Controllers
{
    [Authorize(Roles = "Broker")]
    public class BrokerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BrokerController(ApplicationDbContext db) => _db = db;

        // ---------- CRUD Inmuebles ----------
        // GET: /Broker/Inmuebles
        public async Task<IActionResult> Inmuebles()
        {
            var lista = await _db.Inmuebles
                                 .OrderBy(i => i.Ciudad).ThenBy(i => i.Titulo)
                                 .ToListAsync();
            return View(lista);
        }

        // GET: /Broker/CrearInmueble
        public IActionResult CrearInmueble()
            => View(new Inmueble { Activo = true });

        // POST: /Broker/CrearInmueble
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearInmueble(Inmueble m)
        {
            if (!ModelState.IsValid) return View(m);

            // Ejemplo de unicidad rápida para Codigo
            bool codigoRepetido = await _db.Inmuebles.AnyAsync(x => x.Codigo == m.Codigo);
            if (codigoRepetido)
            {
                ModelState.AddModelError(nameof(m.Codigo), "Código ya existe.");
                return View(m);
            }

            _db.Inmuebles.Add(m);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Inmueble creado.";
            return RedirectToAction(nameof(Inmuebles));
        }

        // GET: /Broker/EditarInmueble/5
        public async Task<IActionResult> EditarInmueble(int id)
        {
            var m = await _db.Inmuebles.FindAsync(id);
            if (m is null) return NotFound();
            return View(m);
        }

        // POST: /Broker/EditarInmueble
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarInmueble(Inmueble m)
        {
            if (!ModelState.IsValid) return View(m);

            bool codigoRepetido = await _db.Inmuebles
                .AnyAsync(x => x.Codigo == m.Codigo && x.Id != m.Id);
            if (codigoRepetido)
            {
                ModelState.AddModelError(nameof(m.Codigo), "Código ya existe.");
                return View(m);
            }

            _db.Update(m);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Inmueble actualizado.";
            return RedirectToAction(nameof(Inmuebles));
        }

        // POST: /Broker/ToggleActivo/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var m = await _db.Inmuebles.FindAsync(id);
            if (m is null) return NotFound();

            m.Activo = !m.Activo;
            await _db.SaveChangesAsync();
            TempData["Ok"] = $"Inmueble {(m.Activo ? "activado" : "desactivado")}.";

            // (Opcional) invalidar tu cache del catálogo si lo estás usando
            return RedirectToAction(nameof(Inmuebles));
        }

        // ---------- Agenda del día (solo lectura) ----------
        // GET: /Broker/AgendaHoy
        public async Task<IActionResult> AgendaHoy()
        {
            var hoy = DateTime.Today;
            var mañana = hoy.AddDays(1);

            var visitas = await _db.Visitas
                .Include(v => v.Inmueble)
                .Where(v => v.FechaInicio >= hoy && v.FechaInicio < mañana)
                .OrderBy(v => v.FechaInicio)
                .ToListAsync();

            return View(visitas);
        }

        // POST: /Broker/ConfirmarVisita/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarVisita(int id)
        {
            var v = await _db.Visitas.FindAsync(id);
            if (v is null) return NotFound();

            v.Estado = EstadoVisita.Confirmada;
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Visita confirmada.";
            return RedirectToAction(nameof(AgendaHoy));
        }

        // POST: /Broker/CancelarVisita/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarVisita(int id)
        {
            var v = await _db.Visitas.FindAsync(id);
            if (v is null) return NotFound();

            v.Estado = EstadoVisita.Cancelada;
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Visita cancelada.";
            return RedirectToAction(nameof(AgendaHoy));
        }

        // ---------- Reservas activas ----------
        // GET: /Broker/ReservasActivas
        public async Task<IActionResult> ReservasActivas()
        {
            var now = DateTime.UtcNow;
            var reservas = await _db.Reservas
                .Include(r => r.Inmueble)
                .Where(r => r.FechaExpiracion > now)
                .OrderBy(r => r.FechaExpiracion)
                .ToListAsync();

            return View(reservas);
        }

        // POST: /Broker/LiberarReserva/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LiberarReserva(int id)
        {
            var r = await _db.Reservas.FindAsync(id);
            if (r is null) return NotFound();

            _db.Reservas.Remove(r); // o r.FechaExpiracion = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Reserva liberada.";
            return RedirectToAction(nameof(ReservasActivas));
        }
    }
}