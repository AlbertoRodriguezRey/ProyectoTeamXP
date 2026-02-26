using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Controllers
{
    public class RecursosController : Controller
    {
        private RepositoryRecursos repo;

        public RecursosController(RepositoryRecursos repo)
        {
            this.repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            List<RecursoFAQ> recursos = await this.repo.GetRecursosAsync();
            return View(recursos);
        }

        public async Task<IActionResult> PorCategoria(string categoria)
        {
            if (string.IsNullOrEmpty(categoria))
            {
                return RedirectToAction("Index");
            }

            List<RecursoFAQ> recursos = await this.repo.GetRecursosByCategoriaAsync(categoria);
            ViewData["Categoria"] = categoria;
            return View(recursos);
        }

        public async Task<IActionResult> Details(int id)
        {
            RecursoFAQ recurso = await this.repo.FindRecursoAsync(id);
            if (recurso == null)
            {
                return RedirectToAction("Index");
            }
            return View(recurso);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RecursoFAQ recurso)
        {
            if (ModelState.IsValid)
            {
                await this.repo.InsertarRecursoAsync(recurso);
                ViewData["MENSAJE"] = "Recurso creado correctamente";
                return RedirectToAction("Index");
            }
            return View(recurso);
        }

        public async Task<IActionResult> Edit(int id)
        {
            RecursoFAQ recurso = await this.repo.FindRecursoAsync(id);
            if (recurso == null)
            {
                return RedirectToAction("Index");
            }
            return View(recurso);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RecursoFAQ recurso)
        {
            if (ModelState.IsValid)
            {
                await this.repo.ActualizarRecursoAsync(recurso);
                ViewData["MENSAJE"] = "Recurso actualizado correctamente";
                return RedirectToAction("Index");
            }
            return View(recurso);
        }

        public async Task<IActionResult> Delete(int id)
        {
            RecursoFAQ recurso = await this.repo.FindRecursoAsync(id);
            if (recurso == null)
            {
                return RedirectToAction("Index");
            }
            return View(recurso);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await this.repo.EliminarRecursoAsync(id);
            ViewData["MENSAJE"] = "Recurso eliminado correctamente";
            return RedirectToAction("Index");
        }
    }
}
