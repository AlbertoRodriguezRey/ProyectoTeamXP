using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositoryRecursos
    {
        private TeamXPDbContext context;

        public RepositoryRecursos(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<RecursoFAQ>> GetRecursosAsync()
        {
            var consulta = from datos in this.context.RecursosFAQ
                           where datos.Activo
                           orderby datos.Categoria, datos.Titulo
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<List<RecursoFAQ>> GetRecursosByCategoriaAsync(string categoria)
        {
            var consulta = from datos in this.context.RecursosFAQ
                           where datos.Categoria == categoria && datos.Activo
                           orderby datos.Titulo
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<RecursoFAQ> FindRecursoAsync(int id)
        {
            var consulta = from datos in this.context.RecursosFAQ
                           where datos.Id == id
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task InsertarRecursoAsync(RecursoFAQ recurso)
        {
            this.context.RecursosFAQ.Add(recurso);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarRecursoAsync(RecursoFAQ recurso)
        {
            this.context.RecursosFAQ.Update(recurso);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarRecursoAsync(int id)
        {
            var recurso = await this.FindRecursoAsync(id);
            if (recurso != null)
            {
                this.context.RecursosFAQ.Remove(recurso);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
