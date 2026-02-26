using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Archivos de progreso (fotos, documentos)
/// </summary>
[Table("ArchivosProgreso")]
public class ArchivoProgreso
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ClienteId")]
    [ForeignKey("Cliente")]
    public int ClienteId { get; set; }

    [Column("FechaSubida")]
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    [Column("NombreOriginal")]
    public string? NombreOriginal { get; set; }

    [Column("RutaServidor")]
    public string RutaServidor { get; set; } = string.Empty;

    [Column("TipoVista")]
    public string? TipoVista { get; set; }

    [Column("TipoArchivo")]
    public string? TipoArchivo { get; set; }

    [Column("Eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("FechaEliminacion")]
    public DateTime? FechaEliminacion { get; set; }

    // Navegación
    public ClientePerfil Cliente { get; set; } = null!;
}
