using ProyectoTeamXP.Helpers;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Data
{
    /// <summary>
    /// Siembra los usuarios de prueba con hashes reales al arrancar la app.
    /// El SQL original usaba hashes falsos (CAST('HashFalso123'...) que nunca coincidían.
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(TeamXPDbContext context)
        {
            // Solo reparar usuarios cuyos hashes son inválidos (longitud < 64 bytes = no son HMACSHA512)
            var usuariosConHashFalso = context.UsuariosSeguridad
                .Where(u => u.PasswordHash.Length < 64)
                .ToList();

            if (!usuariosConHashFalso.Any()) return;

            // Mapa email → contraseña correcta
            var credenciales = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "admin@teamxp.com",   "AdminTest2026!"   },
                { "alberto@teamxp.com", "AlbertoTest2026!" },
                { "maria@teamxp.com",   "MariaTest2026!"   }
            };

            foreach (var usuario in usuariosConHashFalso)
            {
                if (credenciales.TryGetValue(usuario.Email, out var password))
                {
                    PasswordHelper.CreatePasswordHash(password,
                        out byte[] hash,
                        out byte[] salt);

                    usuario.PasswordHash = hash;
                    usuario.PasswordSalt = salt;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
