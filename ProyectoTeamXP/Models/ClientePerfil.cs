using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Perfil de cliente con datos personales y objetivos
/// </summary>
[Table("ClientesPerfil")]
public class ClientePerfil
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("UsuarioSeguridadId")]
    [ForeignKey("UsuarioSeguridad")]
    public int UsuarioSeguridadId { get; set; }

    [Column("NombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Column("Edad")]
    public int? Edad { get; set; }

    [Column("PesoInicial")]
    public decimal? PesoInicial { get; set; }

    [Column("FechaInicioPrograma")]
    public DateTime? FechaInicioPrograma { get; set; }

    [Column("Objetivos")]
    public string? Objetivos { get; set; }

    [Column("ObjetivoPasos")]
    public string? ObjetivoPasos { get; set; }

    [Column("ObjetivoCardio")]
    public string? ObjetivoCardio { get; set; }

    [Column("Suplementacion")]
    public string? Suplementacion { get; set; }

    [Column("TerminosAceptados")]
    public bool TerminosAceptados { get; set; } = false;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("FechaModificacion")]
    public DateTime? FechaModificacion { get; set; }

    [Column("UsuarioModificacion")]
    public string? UsuarioModificacion { get; set; }

    [Column("Eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("FechaEliminacion")]
    public DateTime? FechaEliminacion { get; set; }

    [Column("UsuarioEliminacion")]
    public string? UsuarioEliminacion { get; set; }

    // Navegación
    public UsuarioSeguridad UsuarioSeguridad { get; set; } = null!;
    public ICollection<ClienteMacros> Macros { get; set; } = new List<ClienteMacros>();
    public ICollection<SeguimientoDiario> SeguimientoDiario { get; set; } = new List<SeguimientoDiario>();
    public ICollection<NotasSemanalesCoach> NotasSemanales { get; set; } = new List<NotasSemanalesCoach>();
    public ICollection<PlanNutricional> PlanNutricional { get; set; } = new List<PlanNutricional>();
    public ICollection<RutinaEjercicios> RutinaEjercicios { get; set; } = new List<RutinaEjercicios>();
    public ICollection<RevisionFeedback> Revisiones { get; set; } = new List<RevisionFeedback>();
    public ICollection<ArchivoProgreso> Archivos { get; set; } = new List<ArchivoProgreso>();
}
