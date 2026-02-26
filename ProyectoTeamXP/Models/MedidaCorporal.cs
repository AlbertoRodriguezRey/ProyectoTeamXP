using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Medidas corporales por semana
/// </summary>
[Table("MedidasCorporales")]
public class MedidaCorporal
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("NumeroSemana")]
    public int NumeroSemana { get; set; }

    [Column("Fecha")]
    public DateTime? Fecha { get; set; }

    [Column("Pecho")]
    public decimal? Pecho { get; set; }

    [Column("Brazo")]
    public decimal? Brazo { get; set; }

    [Column("CinturaSobreOmbligo")]
    public decimal? CinturaSobreOmbligo { get; set; }

    [Column("CinturaOmbligo")]
    public decimal? CinturaOmbligo { get; set; }

    [Column("CinturaBajoOmbligo")]
    public decimal? CinturaBajoOmbligo { get; set; }

    [Column("Cadera")]
    public decimal? Cadera { get; set; }

    [Column("Muslos")]
    public decimal? Muslos { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
