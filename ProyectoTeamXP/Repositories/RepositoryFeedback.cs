using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositoryFeedback
    {
        private TeamXPDbContext context;

        public RepositoryFeedback(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<RevisionFeedback>> GetFeedbacksByClienteAsync(int clienteId)
        {
            var consulta = from datos in this.context.RevisionesFeedback
                           where datos.ClienteId == clienteId
                           orderby datos.FechaRevision descending
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<List<RevisionFeedback>> GetFeedbacksPendientesAsync()
        {
            var consulta = from datos in this.context.RevisionesFeedback.Include(r => r.Cliente)
                           where datos.Estado == "Pendiente"
                           orderby datos.FechaRevision
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<RevisionFeedback> FindFeedbackAsync(int id)
        {
            var consulta = from datos in this.context.RevisionesFeedback
                           where datos.Id == id
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<RevisionFeedback> FindFeedbackByFechaAsync(int clienteId, DateTime fecha)
        {
            var consulta = from datos in this.context.RevisionesFeedback
                           where datos.ClienteId == clienteId && datos.FechaRevision.Date == fecha.Date
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task InsertarFeedbackAsync(RevisionFeedback feedback)
        {
            this.context.RevisionesFeedback.Add(feedback);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarFeedbackAsync(RevisionFeedback feedback)
        {
            this.context.RevisionesFeedback.Update(feedback);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarFeedbackAsync(int id)
        {
            var feedback = await this.FindFeedbackAsync(id);
            if (feedback != null)
            {
                this.context.RevisionesFeedback.Remove(feedback);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
