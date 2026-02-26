using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositorySeguimiento
    {
        private TeamXPDbContext context;

        public RepositorySeguimiento(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<SeguimientoDiario>> GetSeguimientosByClienteAsync(int clienteId)
        {
            var consulta = from datos in this.context.SeguimientoDiario
                           where datos.ClienteId == clienteId
                           orderby datos.Fecha descending
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<SeguimientoDiario> FindSeguimientoAsync(int id)
        {
            var consulta = from datos in this.context.SeguimientoDiario
                           where datos.Id == id
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<SeguimientoDiario> FindSeguimientoByFechaAsync(int clienteId, DateTime fecha)
        {
            var consulta = from datos in this.context.SeguimientoDiario
                           where datos.ClienteId == clienteId && datos.Fecha.Date == fecha.Date
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<List<SeguimientoDiario>> GetSeguimientosRangoFechasAsync(int clienteId, DateTime fechaInicio, DateTime fechaFin)
        {
            var consulta = from datos in this.context.SeguimientoDiario
                           where datos.ClienteId == clienteId 
                              && datos.Fecha >= fechaInicio 
                              && datos.Fecha <= fechaFin
                           orderby datos.Fecha
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task InsertarSeguimientoAsync(SeguimientoDiario seguimiento)
        {
            this.context.SeguimientoDiario.Add(seguimiento);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarSeguimientoAsync(SeguimientoDiario seguimiento)
        {
            this.context.SeguimientoDiario.Update(seguimiento);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarSeguimientoAsync(int id)
        {
            var seguimiento = await this.FindSeguimientoAsync(id);
            if (seguimiento != null)
            {
                this.context.SeguimientoDiario.Remove(seguimiento);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
