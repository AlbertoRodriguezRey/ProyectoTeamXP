using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositoryRutinas
    {
        private TeamXPDbContext context;

        public RepositoryRutinas(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<RutinaEjercicios>> GetRutinasByClienteAsync(int clienteId)
        {
            var consulta = from datos in this.context.RutinaEjercicios
                           where datos.ClienteId == clienteId && !datos.Eliminado
                           orderby datos.DiaRutina, datos.OrdenEjercicio
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<List<RutinaEjercicios>> GetRutinasByDiaAsync(int clienteId, string diaRutina)
        {
            var consulta = from datos in this.context.RutinaEjercicios
                           where datos.ClienteId == clienteId 
                              && datos.DiaRutina == diaRutina 
                              && !datos.Eliminado
                           orderby datos.OrdenEjercicio
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<RutinaEjercicios> FindRutinaAsync(int id)
        {
            var consulta = from datos in this.context.RutinaEjercicios
                           where datos.Id == id && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<RutinaEjercicios> FindRutinaConProgresionAsync(int id)
        {
            var consulta = from datos in this.context.RutinaEjercicios.Include(r => r.ProgresionSeries)
                           where datos.Id == id && !datos.Eliminado
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task InsertarRutinaAsync(RutinaEjercicios rutina)
        {
            this.context.RutinaEjercicios.Add(rutina);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarRutinaAsync(RutinaEjercicios rutina)
        {
            this.context.RutinaEjercicios.Update(rutina);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarRutinaAsync(int id)
        {
            var rutina = await this.FindRutinaAsync(id);
            if (rutina != null)
            {
                this.context.RutinaEjercicios.Remove(rutina);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
