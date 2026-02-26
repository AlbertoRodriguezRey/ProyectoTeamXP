using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Notas semanales del coach
/// </summary>
[Table("NotasSemanalesCoach")]
public class NotasSemanalesCoach
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("Anio")]
    public int Anio { get; set; }

    [Column("SemanaNumero")]
    public int SemanaNumero { get; set; }

    [Column("CambiosAlteraciones")]
    public string? CambiosAlteraciones { get; set; }

    [Column("NotasNutricion")]
    public string? NotasNutricion { get; set; }

    [Column("NotasCardio")]
    public string? NotasCardio { get; set; }

    [Column("NotasEntrenamiento")]
    public string? NotasEntrenamiento { get; set; }

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
