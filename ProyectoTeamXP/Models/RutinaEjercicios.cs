using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Rutina de ejercicios personalizada
/// </summary>
[Table("RutinaEjercicios")]
public class RutinaEjercicios
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("DiaRutina")]
    public string? DiaRutina { get; set; }

    [Column("OrdenEjercicio")]
    public int? OrdenEjercicio { get; set; }

    [Column("GrupoMuscular")]
    public string? GrupoMuscular { get; set; }

    [Column("NombreEjercicio")]
    public string? NombreEjercicio { get; set; }

    [Column("LinkVideo")]
    public string? LinkVideo { get; set; }

    [Column("NotasTecnicas")]
    public string? NotasTecnicas { get; set; }

    [Column("SeriesObjetivo")]
    public int? SeriesObjetivo { get; set; }

    [Column("RepsRango")]
    public string? RepsRango { get; set; }

    [Column("EsfuerzoObjetivo")]
    public string? EsfuerzoObjetivo { get; set; }

    [Column("Eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("FechaEliminacion")]
    public DateTime? FechaEliminacion { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
    public ICollection<ProgresionSeries> ProgresionSeries { get; set; } = new List<ProgresionSeries>();
}
