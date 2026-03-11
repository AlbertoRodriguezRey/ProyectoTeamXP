using Microsoft.AspNetCore.Mvc;
using ProyectoTeamXP.Services;
using ProyectoTeamXP.Data;
using ProyectoTeamXP.Models;
using Microsoft.EntityFrameworkCore;

namespace ProyectoTeamXP.Controllers;

public class ExcelController : Controller
{
    private readonly ExcelImportExportService _excelService;
    private readonly GoogleDriveService _driveService;
    private readonly TeamXPDbContext _context;

    public ExcelController(ExcelImportExportService excelService, GoogleDriveService driveService, TeamXPDbContext context)
    {
        _excelService = excelService;
        _driveService = driveService;
        _context = context;
    }

    public IActionResult Index() => View();

    // ── Importar desde archivo subido ──
    [HttpPost]
    public async Task<IActionResult> ImportarExcel(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Por favor selecciona un archivo válido";
            return RedirectToAction("Index");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLower();
        if (extension is not ".xlsx" and not ".xls" and not ".csv")
        {
            TempData["Error"] = "El archivo debe ser un Excel (.xlsx, .xls) o CSV (.csv)";
            return RedirectToAction("Index");
        }

        try
        {
            var usuarioId = ObtenerUsuarioActualId();
            using var stream = archivo.OpenReadStream();

            Stream processingStream = extension == ".csv"
                ? await _excelService.ConvertirCsvAExcel(stream)
                : stream;

            try
            {
                // Usar el nuevo método basado en Cell Map (resuelve merges automáticamente)
                var resultado = await _excelService.ImportarDesdeCellMap(processingStream, usuarioId);

                if (resultado.Success)
                {
                    TempData["Success"] = resultado.Message;
                    return RedirectToAction("Detalle", new { clienteId = resultado.ClienteId });
                }

                TempData["Error"] = resultado.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                if (processingStream != stream)
                    processingStream.Dispose();
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al importar: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    // ── Importar directo desde Google Drive ──
    [HttpPost]
    public async Task<IActionResult> ImportarDesdeDrive(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            TempData["Error"] = "Introduce el ID del archivo de Google Drive";
            return RedirectToAction("Index");
        }

        // Limpiar: el usuario puede pegar la URL completa o solo el ID
        fileId = ExtraerFileIdDeUrl(fileId);

        try
        {
            var usuarioId = ObtenerUsuarioActualId();

            // Descargar de Drive como .xlsx (preserva merges, fórmulas, formato)
            using var stream = await _driveService.DescargarComoExcelAsync(fileId);

            var resultado = await _excelService.ImportarDesdeCellMap(stream, usuarioId);

            if (resultado.Success)
            {
                TempData["Success"] = $"✅ Importado desde Google Drive correctamente";
                return RedirectToAction("Detalle", new { clienteId = resultado.ClienteId });
            }

            TempData["Error"] = resultado.Message;
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error con Google Drive: {ex.Message}";
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

    // ── DIAGNÓSTICO: Ver exactamente qué lee EPPlus del Excel ──
    [HttpPost]
    public async Task<IActionResult> Diagnostico(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo para diagnosticar";
            return RedirectToAction("Index");
        }

        try
        {
            using var stream = archivo.OpenReadStream();
            var extension = Path.GetExtension(archivo.FileName).ToLower();

            Stream processingStream = extension == ".csv"
                ? await _excelService.ConvertirCsvAExcel(stream)
                : stream;

            var todasLasCeldas = ExcelCellReader.LeerTodasLasCeldas(processingStream);

            if (processingStream != stream) processingStream.Dispose();

            ViewBag.NombreArchivo = archivo.FileName;
            return View(todasLasCeldas);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al leer: {ex.Message}";
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

        ViewBag.Medidas = await _context.Set<MedidaCorporal>()
            .Where(m => m.ClienteId == clienteId)
            .OrderBy(m => m.NumeroSemana)
            .ToListAsync();

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

    /// <summary>
    /// Extrae el FileId de una URL de Google Drive/Sheets o devuelve el string tal cual si ya es un ID.
    /// Soporta: https://docs.google.com/spreadsheets/d/{ID}/edit
    ///          https://drive.google.com/file/d/{ID}/view
    ///          o solo el ID directo
    /// </summary>
    private static string ExtraerFileIdDeUrl(string input)
    {
        input = input.Trim();

        // URL de Google Sheets: https://docs.google.com/spreadsheets/d/{ID}/...
        if (input.Contains("/spreadsheets/d/"))
        {
            var start = input.IndexOf("/spreadsheets/d/") + "/spreadsheets/d/".Length;
            var end = input.IndexOf('/', start);
            return end > start ? input[start..end] : input[start..];
        }

        // URL de Google Drive: https://drive.google.com/file/d/{ID}/...
        if (input.Contains("/file/d/"))
        {
            var start = input.IndexOf("/file/d/") + "/file/d/".Length;
            var end = input.IndexOf('/', start);
            return end > start ? input[start..end] : input[start..];
        }

        // Ya es un ID directo
        return input;
    }

    private int ObtenerUsuarioActualId()
    {
        var userIdClaim = User.FindFirst("UsuarioId")?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
    }
}
