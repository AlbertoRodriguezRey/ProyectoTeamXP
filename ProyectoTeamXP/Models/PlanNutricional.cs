using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Plan nutricional personalizado
/// </summary>
[Table("PlanNutricional")]
public class PlanNutricional
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("Comida")]
    public string? Comida { get; set; }

    [Column("OrdenComida")]
    public int? OrdenComida { get; set; }

    [Column("Alimento")]
    public string? Alimento { get; set; }

    [Column("Cantidad")]
    public string? Cantidad { get; set; }

    [Column("Sustituto")]
    public string? Sustituto { get; set; }

    [Column("CantidadSustituto")]
    public string? CantidadSustituto { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
