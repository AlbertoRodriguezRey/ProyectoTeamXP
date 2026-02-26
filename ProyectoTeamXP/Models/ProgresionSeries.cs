using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Progresión de cargas por serie
/// </summary>
[Table("ProgresionSeries")]
public class ProgresionSeries
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("RutinaEjercicioId")]
    [ForeignKey("RutinaEjercicio")]
    public int RutinaEjercicioId { get; set; }

    [Column("NumeroSemana")]
    public int NumeroSemana { get; set; }

    [Column("NumeroSerie")]
    public int NumeroSerie { get; set; }

    [Column("CargaKg")]
    public decimal? CargaKg { get; set; }

    [Column("RepsRealizadas")]
    public int? RepsRealizadas { get; set; }

    [Column("RIRRealizado")]
    public int? RIRRealizado { get; set; }

    [Column("Comentarios")]
    public string? Comentarios { get; set; }

    // Navegación
    public RutinaEjercicios RutinaEjercicio { get; set; } = null!;
}
