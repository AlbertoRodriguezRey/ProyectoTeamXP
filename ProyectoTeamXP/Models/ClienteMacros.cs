using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Macronutrientes por tipo de día
/// </summary>
[Table("ClienteMacros")]
public class ClienteMacros
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("TipoDia")]
    public string TipoDia { get; set; } = string.Empty; // 'ENTRENO' o 'DESCANSO'

    [Column("Proteina")]
    public decimal? Proteina { get; set; }

    [Column("Grasa")]
    public decimal? Grasa { get; set; }

    [Column("Carbohidratos")]
    public decimal? Carbohidratos { get; set; }

    [Column("Calorias")]
    public int? Calorias { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
