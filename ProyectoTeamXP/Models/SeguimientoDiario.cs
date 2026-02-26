using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Seguimiento diario de métricas del cliente
/// </summary>
[Table("SeguimientoDiario")]
public class SeguimientoDiario
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("Fecha")]
    public DateTime Fecha { get; set; }

    [Column("NumeroSemana")]
    public int? NumeroSemana { get; set; }

    [Column("Peso")]
    public decimal? Peso { get; set; }

    [Column("HoraPesaje")]
    public TimeSpan? HoraPesaje { get; set; }

    [Column("Proteina")]
    public decimal? Proteina { get; set; }

    [Column("Grasa")]
    public decimal? Grasa { get; set; }

    [Column("Carbohidratos")]
    public decimal? Carbohidratos { get; set; }

    [Column("TotalCalorias")]
    public int? TotalCalorias { get; set; }

    [Column("DiaEntreno")]
    public string? DiaEntreno { get; set; }

    [Column("RendimientoSesion")]
    public string? RendimientoSesion { get; set; }

    [Column("HorasSueno")]
    public decimal? HorasSueno { get; set; }

    [Column("CalidadSueno")]
    public string? CalidadSueno { get; set; }

    [Column("Apetito")]
    public string? Apetito { get; set; }

    [Column("NivelEstres")]
    public string? NivelEstres { get; set; }

    [Column("PasosRealizados")]
    public int? PasosRealizados { get; set; }

    [Column("DuracionCardio")]
    public string? DuracionCardio { get; set; }

    [Column("EntrenoRealizado")]
    public bool EntrenoRealizado { get; set; } = false;

    [Column("CardioRealizado")]
    public bool CardioRealizado { get; set; } = false;

    [Column("Notas")]
    public string? Notas { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
