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
            TempData["Error"] = "Por favor selecciona un archivo válido";
            return RedirectToAction("Index");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLower();
        if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
        {
            TempData["Error"] = "El archivo debe ser un Excel (.xlsx, .xls) o CSV (.csv)";
            return RedirectToAction("Index");
        }

        try
        {
            // Obtener el usuario actual
            var usuarioId = ObtenerUsuarioActualId();

            using var stream = archivo.OpenReadStream();
            
            // Si es CSV, convertir a Excel primero usando EPPlus
            Stream processingStream;
            if (extension == ".csv")
            {
                processingStream = await ConvertirCsvAExcel(stream);
            }
            else
            {
                processingStream = stream;
            }

            var resultado = await _excelService.ImportarExcel(processingStream, usuarioId);

            if (processingStream != stream)
            {
                processingStream.Dispose();
            }

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

    private async Task<Stream> ConvertirCsvAExcel(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var package = new OfficeOpenXml.ExcelPackage();
        
        var worksheet = package.Workbook.Worksheets.Add("Datos");
        
        int fila = 1;
        while (!reader.EndOfStream)
        {
            var linea = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(linea))
            {
                fila++;
                continue;
            }

            // Parsear CSV (simple, asumiendo separador por comas)
            var valores = ParsearLineaCsv(linea);
            
            for (int col = 0; col < valores.Length; col++)
            {
                worksheet.Cells[fila, col + 1].Value = valores[col];
            }
            
            fila++;
        }

        var memoryStream = new MemoryStream();
        await package.SaveAsAsync(memoryStream);
        memoryStream.Position = 0;
        
        return memoryStream;
    }

    private string[] ParsearLineaCsv(string linea)
    {
        var resultado = new List<string>();
        var actual = new System.Text.StringBuilder();
        bool entreComillas = false;

        for (int i = 0; i < linea.Length; i++)
        {
            char c = linea[i];

            if (c == '"')
            {
                entreComillas = !entreComillas;
            }
            else if (c == ',' && !entreComillas)
            {
                resultado.Add(actual.ToString().Trim());
                actual.Clear();
            }
            else
            {
                actual.Append(c);
            }
        }

        resultado.Add(actual.ToString().Trim());
        return resultado.ToArray();
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
