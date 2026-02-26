using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Roles del sistema
/// </summary>
[Table("Roles")]
public class Rol
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    // Navegación
    public ICollection<UsuarioSeguridad> Usuarios { get; set; } = new List<UsuarioSeguridad>();
}
