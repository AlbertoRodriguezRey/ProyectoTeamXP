using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Services;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;
using Microsoft.EntityFrameworkCore;

namespace ProyectoTeamXP.Controllers;

public class ExcelController : Controller
{
    private readonly ExcelImportExportService _excelService;
    private readonly TeamXPDbContext _context;

    public ExcelController(ExcelImportExportService excelService, TeamXPDbContext context)
    {
        _excelService = excelService;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ImportarExcel(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Por favor selecciona un archivo Excel válido";
            return RedirectToAction("Index");
        }

        if (!archivo.FileName.EndsWith(".xlsx") && !archivo.FileName.EndsWith(".xls"))
        {
            TempData["Error"] = "El archivo debe ser un Excel (.xlsx o .xls)";
            return RedirectToAction("Index");
        }

        try
        {
            // Obtener el usuario actual (ajusta según tu sistema de autenticación)
            var usuarioId = ObtenerUsuarioActualId();

            using var stream = archivo.OpenReadStream();
            var resultado = await _excelService.ImportarExcel(stream, usuarioId);

            if (resultado.Success)
            {
                TempData["Success"] = resultado.Message;
                return RedirectToAction("Detalle", new { clienteId = resultado.ClienteId });
            }
            else
            {
                TempData["Error"] = resultado.Message;
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al importar: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(int clienteId)
    {
        try
        {
            var excelBytes = await _excelService.ExportarExcel(clienteId);
            var cliente = await _context.ClientesPerfil.FindAsync(clienteId);
            
            var nombreArchivo = $"{cliente?.NombreCompleto ?? "Cliente"}_Seguimiento_{DateTime.Now:yyyyMMdd}.xlsx";
            
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int clienteId)
    {
        var cliente = await _context.ClientesPerfil
            .Include(c => c.Macros)
            .Include(c => c.SeguimientoDiario)
            .FirstOrDefaultAsync(c => c.Id == clienteId && !c.Eliminado);

        if (cliente == null)
        {
            TempData["Error"] = "Cliente no encontrado";
            return RedirectToAction("Index");
        }

        var medidas = await _context.Set<MedidaCorporal>()
            .Where(m => m.ClienteId == clienteId)
            .OrderBy(m => m.NumeroSemana)
            .ToListAsync();

        ViewBag.Medidas = medidas;

        return View(cliente);
    }

    [HttpGet]
    public async Task<IActionResult> ListarClientes()
    {
        var clientes = await _context.ClientesPerfil
            .Where(c => !c.Eliminado)
            .OrderByDescending(c => c.FechaCreacion)
            .ToListAsync();

        return View(clientes);
    }

    private int ObtenerUsuarioActualId()
    {
        // Implementa tu lógica de autenticación aquí
        // Por ejemplo, desde User.Claims o Session
        var userIdClaim = User.FindFirst("UsuarioId")?.Value;
        
        if (int.TryParse(userIdClaim, out int userId))
            return userId;

        // Por defecto retorna 1 (ajusta según tu lógica)
        return 1;
    }
}
