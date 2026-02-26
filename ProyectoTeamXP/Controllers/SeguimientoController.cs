using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Extensions;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Controllers
{
    public class SeguimientoController : Controller
    {
        private RepositorySeguimiento repo;

        public SeguimientoController(RepositorySeguimiento repo)
        {
            this.repo = repo;
        }

        public async Task<IActionResult> Index(int? clienteId)
        {
            if (clienteId == null)
            {
                return RedirectToAction("Index", "Clientes");
            }

            List<SeguimientoDiario> seguimientos = await this.repo.GetSeguimientosByClienteAsync(clienteId.Value);
            ViewData["ClienteId"] = clienteId;
            return View(seguimientos);
        }

        public async Task<IActionResult> Details(int id)
        {
            SeguimientoDiario seguimiento = await this.repo.FindSeguimientoAsync(id);
            if (seguimiento == null)
            {
                return RedirectToAction("Index");
            }
            return View(seguimiento);
        }

        public IActionResult Create(int? clienteId)
        {
            if (clienteId == null)
            {
                return RedirectToAction("Index", "Clientes");
            }
            ViewData["ClienteId"] = clienteId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SeguimientoDiario seguimiento)
        {
            if (ModelState.IsValid)
            {
                await this.repo.InsertarSeguimientoAsync(seguimiento);
                ViewData["MENSAJE"] = "Seguimiento creado correctamente";
                return RedirectToAction("Index", new { clienteId = seguimiento.ClienteId });
            }
            return View(seguimiento);
        }

        public async Task<IActionResult> Edit(int id)
        {
            SeguimientoDiario seguimiento = await this.repo.FindSeguimientoAsync(id);
            if (seguimiento == null)
            {
                return RedirectToAction("Index");
            }
            return View(seguimiento);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SeguimientoDiario seguimiento)
        {
            if (ModelState.IsValid)
            {
                await this.repo.ActualizarSeguimientoAsync(seguimiento);
                ViewData["MENSAJE"] = "Seguimiento actualizado correctamente";
                return RedirectToAction("Index", new { clienteId = seguimiento.ClienteId });
            }
            return View(seguimiento);
        }

        public async Task<IActionResult> Estadisticas(int? clienteId)
        {
            if (clienteId == null)
            {
                return RedirectToAction("Index", "Clientes");
            }

            DateTime fechaInicio = DateTime.Now.AddDays(-30);
            DateTime fechaFin = DateTime.Now;

            List<SeguimientoDiario> seguimientos = 
                await this.repo.GetSeguimientosRangoFechasAsync(clienteId.Value, fechaInicio, fechaFin);

            ViewData["ClienteId"] = clienteId;
            return View(seguimientos);
        }
    }
}
