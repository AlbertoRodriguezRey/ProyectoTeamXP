using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositoryUsuarios
    {
        private TeamXPDbContext context;

        public RepositoryUsuarios(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<UsuarioSeguridad>> GetUsuariosAsync()
        {
            var consulta = from datos in this.context.UsuariosSeguridad
                           where !datos.Eliminado
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<UsuarioSeguridad> FindUsuarioAsync(int id)
        {
            var consulta = from datos in this.context.UsuariosSeguridad
                           where datos.Id == id && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<UsuarioSeguridad> FindUsuarioByEmailAsync(string email)
        {
            var consulta = from datos in this.context.UsuariosSeguridad.Include(u => u.Rol)
                           where datos.Email == email && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await this.context.UsuariosSeguridad
                .AnyAsync(u => u.Email == email && !u.Eliminado);
        }

        public async Task<List<UsuarioSeguridad>> GetUsuariosActivosAsync()
        {
            var consulta = from datos in this.context.UsuariosSeguridad.Include(u => u.Rol)
                           where datos.Activo && !datos.Eliminado
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task InsertarUsuarioAsync(UsuarioSeguridad usuario)
        {
            this.context.UsuariosSeguridad.Add(usuario);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarUsuarioAsync(UsuarioSeguridad usuario)
        {
            this.context.UsuariosSeguridad.Update(usuario);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarUsuarioAsync(int id)
        {
            var usuario = await this.FindUsuarioAsync(id);
            if (usuario != null)
            {
                this.context.UsuariosSeguridad.Remove(usuario);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
