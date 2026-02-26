using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTeamXP.Models;

/// <summary>
/// Entidad: Usuarios con información de autenticación
/// </summary>
[Table("UsuariosSeguridad")]
public class UsuarioSeguridad
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [Column("PasswordHash")]
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    [Column("PasswordSalt")]
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    // Campo SOLO para testing (Borrar en Producción)
    [Column("PasswordPlain_SOLO_TESTING")]
    public string? PasswordPlain_SOLO_TESTING { get; set; }

    [Column("RolId")]
    [ForeignKey("Rol")]
    public int RolId { get; set; }

    [Column("FechaCreacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [Column("UltimoAcceso")]
    public DateTime? UltimoAcceso { get; set; }

    [Column("Activo")]
    public bool Activo { get; set; } = true;

    [Column("Eliminado")]
    public bool Eliminado { get; set; } = false;

    [Column("FechaEliminacion")]
    public DateTime? FechaEliminacion { get; set; }

    [Column("UsuarioEliminacion")]
    public string? UsuarioEliminacion { get; set; }

    // Navegación
    public Rol Rol { get; set; } = null!;
    public ClientePerfil? ClientePerfil { get; set; }
}
