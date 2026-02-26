using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Controllers
{
    public class FeedbackController : Controller
    {
        private RepositoryFeedback repo;

        public FeedbackController(RepositoryFeedback repo)
        {
            this.repo = repo;
        }

        public async Task<IActionResult> Index(int? clienteId)
        {
            if (clienteId == null)
            {
                return RedirectToAction("Index", "Clientes");
            }

            List<RevisionFeedback> feedbacks = await this.repo.GetFeedbacksByClienteAsync(clienteId.Value);
            ViewData["ClienteId"] = clienteId;
            return View(feedbacks);
        }

        public async Task<IActionResult> Pendientes()
        {
            List<RevisionFeedback> feedbacks = await this.repo.GetFeedbacksPendientesAsync();
            return View(feedbacks);
        }

        public async Task<IActionResult> Details(int id)
        {
            RevisionFeedback feedback = await this.repo.FindFeedbackAsync(id);
            if (feedback == null)
            {
                return RedirectToAction("Pendientes");
            }
            return View(feedback);
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
        public async Task<IActionResult> Create(RevisionFeedback feedback)
        {
            if (ModelState.IsValid)
            {
                await this.repo.InsertarFeedbackAsync(feedback);
                ViewData["MENSAJE"] = "Feedback creado correctamente";
                return RedirectToAction("Index", new { clienteId = feedback.ClienteId });
            }
            return View(feedback);
        }

        public async Task<IActionResult> Edit(int id)
        {
            RevisionFeedback feedback = await this.repo.FindFeedbackAsync(id);
            if (feedback == null)
            {
                return RedirectToAction("Pendientes");
            }
            return View(feedback);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(RevisionFeedback feedback)
        {
            if (ModelState.IsValid)
            {
                await this.repo.ActualizarFeedbackAsync(feedback);
                ViewData["MENSAJE"] = "Feedback actualizado correctamente";
                return RedirectToAction("Index", new { clienteId = feedback.ClienteId });
            }
            return View(feedback);
        }

        public async Task<IActionResult> Completar(int id)
        {
            RevisionFeedback feedback = await this.repo.FindFeedbackAsync(id);
            if (feedback != null)
            {
                feedback.Estado = "Completado";
                feedback.FechaCompletado = DateTime.Now;
                await this.repo.ActualizarFeedbackAsync(feedback);
                ViewData["MENSAJE"] = "Feedback marcado como completado";
            }
            return RedirectToAction("Pendientes");
        }
    }
}
