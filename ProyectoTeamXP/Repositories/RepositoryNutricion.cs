using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Repositories
{
    public class RepositoryNutricion
    {
        private TeamXPDbContext context;

        public RepositoryNutricion(TeamXPDbContext context)
        {
            this.context = context;
        }

        public async Task<List<PlanNutricional>> GetPlanByClienteAsync(int clienteId)
        {
            var consulta = from datos in this.context.PlanNutricional
                           where datos.ClienteId == clienteId
                           orderby datos.OrdenComida
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<List<PlanNutricional>> GetPlanByComidaAsync(int clienteId, string comida)
        {
            var consulta = from datos in this.context.PlanNutricional
                           where datos.ClienteId == clienteId && datos.Comida == comida
                           orderby datos.OrdenComida
                           select datos;
            return await consulta.ToListAsync();
        }

        public async Task<PlanNutricional> FindPlanAsync(int id)
        {
            var consulta = from datos in this.context.PlanNutricional
                           where datos.Id == id
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task InsertarPlanAsync(PlanNutricional plan)
        {
            this.context.PlanNutricional.Add(plan);
            await this.context.SaveChangesAsync();
        }

        public async Task ActualizarPlanAsync(PlanNutricional plan)
        {
            this.context.PlanNutricional.Update(plan);
            await this.context.SaveChangesAsync();
        }

        public async Task EliminarPlanAsync(int id)
        {
            var plan = await this.FindPlanAsync(id);
            if (plan != null)
            {
                this.context.PlanNutricional.Remove(plan);
                await this.context.SaveChangesAsync();
            }
        }
    }
}
