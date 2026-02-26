using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Controllers
{
    public class NutricionController : Controller
    {
        private RepositoryNutricion repo;

        public NutricionController(RepositoryNutricion repo)
        {
            this.repo = repo;
        }

        public async Task<IActionResult> Index(int? clienteId)
        {
            if (clienteId == null)
            {
                return RedirectToAction("Index", "Clientes");
            }

            List<PlanNutricional> plan = await this.repo.GetPlanByClienteAsync(clienteId.Value);
            ViewData["ClienteId"] = clienteId;
            return View(plan);
        }

        public async Task<IActionResult> PorComida(int? clienteId, string comida)
        {
            if (clienteId == null || string.IsNullOrEmpty(comida))
            {
                return RedirectToAction("Index", new { clienteId });
            }

            List<PlanNutricional> plan = await this.repo.GetPlanByComidaAsync(clienteId.Value, comida);
            ViewData["ClienteId"] = clienteId;
            ViewData["Comida"] = comida;
            return View(plan);
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
        public async Task<IActionResult> Create(PlanNutricional plan)
        {
            if (ModelState.IsValid)
            {
                await this.repo.InsertarPlanAsync(plan);
                ViewData["MENSAJE"] = "Plan nutricional creado correctamente";
                return RedirectToAction("Index", new { clienteId = plan.ClienteId });
            }
            return View(plan);
        }

        public async Task<IActionResult> Edit(int id)
        {
            PlanNutricional plan = await this.repo.FindPlanAsync(id);
            if (plan == null)
            {
                return RedirectToAction("Index");
            }
            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PlanNutricional plan)
        {
            if (ModelState.IsValid)
            {
                await this.repo.ActualizarPlanAsync(plan);
                ViewData["MENSAJE"] = "Plan nutricional actualizado correctamente";
                return RedirectToAction("Index", new { clienteId = plan.ClienteId });
            }
            return View(plan);
        }

        public async Task<IActionResult> Delete(int id)
        {
            PlanNutricional plan = await this.repo.FindPlanAsync(id);
            if (plan == null)
            {
                return RedirectToAction("Index");
            }
            return View(plan);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, int clienteId)
        {
            await this.repo.EliminarPlanAsync(id);
            ViewData["MENSAJE"] = "Plan nutricional eliminado correctamente";
            return RedirectToAction("Index", new { clienteId });
        }
    }
}
