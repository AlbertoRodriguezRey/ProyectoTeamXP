using Microsoft.EntityFrameworkCore;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Data
{
    public class TeamXPDbContext : DbContext
    {
        public TeamXPDbContext(DbContextOptions<TeamXPDbContext> options) : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<UsuarioSeguridad> UsuariosSeguridad { get; set; }
        public DbSet<ClientePerfil> ClientesPerfil { get; set; }
        public DbSet<ClienteMacros> ClienteMacros { get; set; }
        public DbSet<SeguimientoDiario> SeguimientoDiario { get; set; }
        public DbSet<MedidaCorporal> MedidasCorporales { get; set; }
        public DbSet<NotasSemanalesCoach> NotasSemanalesCoach { get; set; }
        public DbSet<PlanNutricional> PlanNutricional { get; set; }
        public DbSet<RutinaEjercicios> RutinaEjercicios { get; set; }
        public DbSet<ProgresionSeries> ProgresionSeries { get; set; }
        public DbSet<RevisionFeedback> RevisionesFeedback { get; set; }
        public DbSet<ArchivoProgreso> ArchivosProgreso { get; set; }
        public DbSet<RecursoFAQ> RecursosFAQ { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Precisión explícita para evitar truncamiento silencioso (DECIMAL(5,2) / DECIMAL(6,2))
            modelBuilder.Entity<ClienteMacros>(e =>
            {
                e.Property(p => p.Proteina).HasPrecision(5, 2);
                e.Property(p => p.Grasa).HasPrecision(5, 2);
                e.Property(p => p.Carbohidratos).HasPrecision(5, 2);
            });

            modelBuilder.Entity<ClientePerfil>(e =>
            {
                e.Property(p => p.PesoInicial).HasPrecision(5, 2);
            });

            modelBuilder.Entity<ProgresionSeries>(e =>
            {
                e.Property(p => p.CargaKg).HasPrecision(6, 2);
            });

            modelBuilder.Entity<SeguimientoDiario>(e =>
            {
                e.Property(p => p.Peso).HasPrecision(5, 2);
                e.Property(p => p.HorasSueno).HasPrecision(4, 2);
                e.Property(p => p.Proteina).HasPrecision(5, 2);
                e.Property(p => p.Grasa).HasPrecision(5, 2);
                e.Property(p => p.Carbohidratos).HasPrecision(5, 2);
            });

            modelBuilder.Entity<MedidaCorporal>(e =>
            {
                e.Property(p => p.Pecho).HasPrecision(5, 2);
                e.Property(p => p.Brazo).HasPrecision(5, 2);
                e.Property(p => p.CinturaSobreOmbligo).HasPrecision(5, 2);
                e.Property(p => p.CinturaOmbligo).HasPrecision(5, 2);
                e.Property(p => p.CinturaBajoOmbligo).HasPrecision(5, 2);
                e.Property(p => p.Cadera).HasPrecision(5, 2);
                e.Property(p => p.Muslos).HasPrecision(5, 2);
            });
        }
    }
}
