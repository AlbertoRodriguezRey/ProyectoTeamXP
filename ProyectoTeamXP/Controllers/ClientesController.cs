using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ProyectoTeamXP.Extensions;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;

namespace ProyectoTeamXP.Controllers
{
    public class ClientesController : Controller
    {
        private RepositoryClientes repo;
        private IDistributedCache cache;

        public ClientesController(RepositoryClientes repo, IDistributedCache cache)
        {
            this.repo = repo;
            this.cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            List<ClientePerfil> clientes = await this.repo.GetClientesAsync();
            return View(clientes);
        }

        public async Task<IActionResult> Details(int id)
        {
            ClientePerfil cliente = await this.repo.FindClienteConMacrosAsync(id);
            if (cliente == null)
            {
                return RedirectToAction("Index");
            }
            return View(cliente);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClientePerfil cliente)
        {
            if (ModelState.IsValid)
            {
                await this.repo.InsertarClienteAsync(cliente);
                await this.cache.RemoveAsync("clientes_activos");
                ViewData["MENSAJE"] = "Cliente creado correctamente";
                return RedirectToAction("Index");
            }
            return View(cliente);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ClientePerfil cliente = await this.repo.FindClienteAsync(id);
            if (cliente == null)
            {
                return RedirectToAction("Index");
            }
            return View(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ClientePerfil cliente)
        {
            if (ModelState.IsValid)
            {
                await this.repo.ActualizarClienteAsync(cliente);
                await this.cache.RemoveAsync($"cliente_{cliente.Id}");
                await this.cache.RemoveAsync("clientes_activos");
                ViewData["MENSAJE"] = "Cliente actualizado correctamente";
                return RedirectToAction("Index");
            }
            return View(cliente);
        }

        public async Task<IActionResult> Delete(int id)
        {
            ClientePerfil cliente = await this.repo.FindClienteAsync(id);
            if (cliente == null)
            {
                return RedirectToAction("Index");
            }
            return View(cliente);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await this.repo.EliminarClienteAsync(id);
            await this.cache.RemoveAsync($"cliente_{id}");
            await this.cache.RemoveAsync("clientes_activos");
            ViewData["MENSAJE"] = "Cliente eliminado correctamente";
            return RedirectToAction("Index");
        }
    }
}
