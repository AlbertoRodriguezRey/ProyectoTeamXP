using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Recursos y FAQ
/// </summary>
[Table("RecursosFAQ")]
public class RecursoFAQ
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Categoria")]
    public string? Categoria { get; set; }

    [Column("Titulo")]
    public string? Titulo { get; set; }

    [Column("Descripcion")]
    public string? Descripcion { get; set; }

    [Column("UrlVideo1")]
    public string? UrlVideo1 { get; set; }

    [Column("UrlVideo2")]
    public string? UrlVideo2 { get; set; }

    [Column("Activo")]
    public bool Activo { get; set; } = true;

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
