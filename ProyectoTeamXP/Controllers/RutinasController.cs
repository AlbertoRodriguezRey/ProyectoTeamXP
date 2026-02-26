using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Controllers
{
    public class RutinasController : Controller
    {
        private RepositoryRutinas repo;

        public RutinasController(RepositoryRutinas repo)
        {
            this.repo = repo;
        }

        public async Task<IActionResult> Index(int? clienteId)
        {
            if (clienteId == null)
            {
                return RedirectToAction("Index", "Clientes");
            }

            List<RutinaEjercicios> rutinas = await this.repo.GetRutinasByClienteAsync(clienteId.Value);
            ViewData["ClienteId"] = clienteId;
            return View(rutinas);
        }

        public async Task<IActionResult> PorDia(int? clienteId, string dia)
        {
            if (clienteId == null || string.IsNullOrEmpty(dia))
            {
                return RedirectToAction("Index", new { clienteId });
            }

            List<RutinaEjercicios> rutinas = await this.repo.GetRutinasByDiaAsync(clienteId.Value, dia);
            ViewData["ClienteId"] = clienteId;
            ViewData["DiaRutina"] = dia;
            return View(rutinas);
        }

        public async Task<IActionResult> Details(int id)
        {
            RutinaEjercicios rutina = await this.repo.FindRutinaConProgresionAsync(id);
            if (rutina == null)
            {
                return RedirectToAction("Index");
            }
            return View(rutina);
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
        public async Task<IActionResult> Create(RutinaEjercicios rutina)
        {
            if (ModelState.IsValid)
            {
                await this.repo.InsertarRutinaAsync(rutina);
                ViewData["MENSAJE"] = "Rutina creada correctamente";
                return RedirectToAction("Index", new { clienteId = rutina.ClienteId });
            }
            return View(rutina);
        }

        public async Task<IActionResult> Edit(int id)
        {
            RutinaEjercicios rutina = await this.repo.FindRutinaAsync(id);
            if (rutina == null)
            {
                return RedirectToAction("Index");
            }
            return View(rutina);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RutinaEjercicios rutina)
        {
            if (ModelState.IsValid)
            {
                await this.repo.ActualizarRutinaAsync(rutina);
                ViewData["MENSAJE"] = "Rutina actualizada correctamente";
                return RedirectToAction("Index", new { clienteId = rutina.ClienteId });
            }
            return View(rutina);
        }

        public async Task<IActionResult> Delete(int id)
        {
            RutinaEjercicios rutina = await this.repo.FindRutinaAsync(id);
            if (rutina == null)
            {
                return RedirectToAction("Index");
            }
            return View(rutina);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, int clienteId)
        {
            await this.repo.EliminarRutinaAsync(id);
            ViewData["MENSAJE"] = "Rutina eliminada correctamente";
            return RedirectToAction("Index", new { clienteId });
        }
    }
}
