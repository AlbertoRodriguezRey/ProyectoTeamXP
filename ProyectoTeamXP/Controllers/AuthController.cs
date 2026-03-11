using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Helpers;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Repositories;
using ProyectoTeamXP.ViewModels;

namespace ProyectoTeamXP.Controllers
{
    public class AuthController : Controller
    {
        private RepositoryUsuarios repoUsuarios;
        private RepositoryClientes repoClientes;

        public AuthController(RepositoryUsuarios repoUsuarios, RepositoryClientes repoClientes)
        {
            this.repoUsuarios = repoUsuarios;
            this.repoClientes = repoClientes;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = await this.repoUsuarios.FindUsuarioByEmailAsync(model.Email);

                if (usuario != null && usuario.Activo && !usuario.Eliminado)
                {
                    bool passwordValido = PasswordHelper.VerifyPasswordHash(
                        model.Password, 
                        usuario.PasswordHash, 
                        usuario.PasswordSalt);

                    if (passwordValido)
                    {
                        // Guardar datos en Session
                        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                        HttpContext.Session.SetString("UsuarioEmail", usuario.Email);
                        HttpContext.Session.SetString("UsuarioRol", usuario.Rol.Nombre);

                        // Actualizar último acceso
                        usuario.UltimoAcceso = DateTime.Now;
                        await this.repoUsuarios.ActualizarUsuarioAsync(usuario);

                        // Redirigir según el rol
                        if (usuario.Rol.Nombre == "Admin" || usuario.Rol.Nombre == "Coach")
                        {
                            return RedirectToAction("Index", "Clientes");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ViewData["ERROR"] = "Email o contraseña incorrectos";
            }

            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe
                bool emailExiste = await this.repoUsuarios.ExisteEmailAsync(model.Email);

                if (emailExiste)
                {
                    ViewData["ERROR"] = "El email ya está registrado";
                    return View(model);
                }

                // Crear el hash de la contraseña
                PasswordHelper.CreatePasswordHash(
                    model.Password,
                    out byte[] passwordHash,
                    out byte[] passwordSalt);

                // Crear el usuario
                var usuario = new UsuarioSeguridad
                {
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RolId = 2, // Por defecto, rol "Cliente"
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
#if DEBUG
                usuario.PasswordPlain_SOLO_TESTING = model.Password;
#endif

                await this.repoUsuarios.InsertarUsuarioAsync(usuario);

                // Crear el perfil del cliente
                var cliente = new ClientePerfil
                {
                    UsuarioSeguridadId = usuario.Id,
                    NombreCompleto = model.NombreCompleto,
                    TerminosAceptados = model.AceptaTerminos,
                    FechaCreacion = DateTime.Now
                };

                await this.repoClientes.InsertarClienteAsync(cliente);

                ViewData["MENSAJE"] = "Registro exitoso. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
