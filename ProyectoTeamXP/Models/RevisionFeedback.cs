using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Revisiones y feedback periódico
/// </summary>
[Table("RevisionesFeedback")]
public class RevisionFeedback
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("FechaRevision")]
    public DateTime FechaRevision { get; set; }

    [Column("Estado")]
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Completado, Cancelado

    [Column("ExitosSemana")]
    public string? ExitosSemana { get; set; }

    [Column("FeedbackEntreno")]
    public string? FeedbackEntreno { get; set; }

    [Column("FeedbackNutricion")]
    public string? FeedbackNutricion { get; set; }

    [Column("FeedbackFisico")]
    public string? FeedbackFisico { get; set; }

    [Column("FeedbackAnimo")]
    public string? FeedbackAnimo { get; set; }

    [Column("FeedbackApoyo")]
    public string? FeedbackApoyo { get; set; }

    [Column("ProximasSemanas")]
    public string? ProximasSemanas { get; set; }

    [Column("FechaCompletado")]
    public DateTime? FechaCompletado { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
