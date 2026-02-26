using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositoryClientes
    {
        private TeamXPDbContext context;

        public RepositoryClientes(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<ClientePerfil>> GetClientesAsync()
        {
            var consulta = from datos in this.context.ClientesPerfil.Include(c => c.UsuarioSeguridad)
                           where !datos.Eliminado
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<ClientePerfil> FindClienteAsync(int id)
        {
            var consulta = from datos in this.context.ClientesPerfil.Include(c => c.UsuarioSeguridad)
                           where datos.Id == id && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<ClientePerfil> FindClienteByUsuarioIdAsync(int usuarioId)
        {
            var consulta = from datos in this.context.ClientesPerfil.Include(c => c.UsuarioSeguridad)
                           where datos.UsuarioSeguridadId == usuarioId && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<ClientePerfil> FindClienteConMacrosAsync(int id)
        {
            var consulta = from datos in this.context.ClientesPerfil
                               .Include(c => c.UsuarioSeguridad)
                               .Include(c => c.Macros)
                           where datos.Id == id && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task InsertarClienteAsync(ClientePerfil cliente)
        {
            this.context.ClientesPerfil.Add(cliente);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarClienteAsync(ClientePerfil cliente)
        {
            this.context.ClientesPerfil.Update(cliente);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarClienteAsync(int id)
        {
            var cliente = await this.FindClienteAsync(id);
            if (cliente != null)
            {
                this.context.ClientesPerfil.Remove(cliente);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
