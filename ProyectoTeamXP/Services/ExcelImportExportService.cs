using OfficeOpenXml;
using ProyectoTeamXP.Models;
using ProyectoTeamXP.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ProyectoTeamXP.Services;

public class ExcelImportExportService
{
    private readonly TeamXPDbContext _context;

    public ExcelImportExportService(TeamXPDbContext context)
    {
        _context = context;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    // ═══════════════════════════════════════════════════
    //  CSV → Excel con detección inteligente de tipos
    // ═══════════════════════════════════════════════════

    public async Task<Stream> ConvertirCsvAExcel(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Datos");

        // Leer primera línea para detectar separador (;  o  ,)
        var primeraLinea = await reader.ReadLineAsync();
        reader.BaseStream.Position = 0;
        reader.DiscardBufferedData();

        char separador = DetectarSeparadorCsv(primeraLinea);

        int fila = 1;
        while (!reader.EndOfStream)
        {
            var linea = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(linea))
            {
                fila++;
                continue;
            }

            var valores = ParsearLineaCsv(linea, separador);

            for (int col = 0; col < valores.Length; col++)
            {
                var celda = worksheet.Cells[fila, col + 1];
                var tipado = ConvertirValorCsv(valores[col], separador);
                celda.Value = tipado;

                // Formatear fechas si se detectaron
                if (tipado is DateTime)
                    celda.Style.Numberformat.Format = "dd/MM/yyyy";
            }

            fila++;
        }

        var memoryStream = new MemoryStream();
        await package.SaveAsAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Detecta si el CSV usa punto y coma (locale español) o coma como separador.
    /// </summary>
    private static char DetectarSeparadorCsv(string? primeraLinea)
    {
        if (string.IsNullOrEmpty(primeraLinea))
            return ',';

        int comas = primeraLinea.Count(c => c == ',');
        int puntoYComa = primeraLinea.Count(c => c == ';');

        return puntoYComa > comas ? ';' : ',';
    }

    private static string[] ParsearLineaCsv(string linea, char separador)
    {
        var resultado = new List<string>();
        var actual = new System.Text.StringBuilder();
        bool entreComillas = false;

        foreach (char c in linea)
        {
            if (c == '"')
            {
                entreComillas = !entreComillas;
            }
            else if (c == separador && !entreComillas)
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

    /// <summary>
    /// Convierte un valor CSV string al tipo .NET correcto (double, DateTime o string).
    /// Así EPPlus almacena números como números y no como texto.
    /// </summary>
    private static object ConvertirValorCsv(string valor, char separadorCsv)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return valor;

        var limpio = valor.Trim();

        // Quitar unidades comunes para detectar el número subyacente
        var sinUnidades = limpio
            .Replace("kg", "", StringComparison.OrdinalIgnoreCase)
            .Replace("kcal", "", StringComparison.OrdinalIgnoreCase)
            .Replace("cm", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (!string.IsNullOrWhiteSpace(sinUnidades))
        {
            string paraParser = sinUnidades;

            // Si CSV usa ; como separador → la coma en valores es decimal (español)
            if (separadorCsv == ';')
                paraParser = sinUnidades.Replace(",", ".");

            if (double.TryParse(paraParser, NumberStyles.Any, CultureInfo.InvariantCulture, out double numero))
                return numero;
        }

        // Intentar detectar fechas comunes
        var formatosFecha = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-MMM-yyyy", "dd-MMM", "d-MMM" };
        foreach (var fmt in formatosFecha)
        {
            if (DateTime.TryParseExact(limpio, fmt, new CultureInfo("es-ES"), DateTimeStyles.None, out var fecha))
                return fecha;
        }

        return limpio;
    }

    // ═══════════════════════════════════════════════════
    //  IMPORTAR EXCEL
    // ═══════════════════════════════════════════════════

    public async Task<(bool Success, string Message, int ClienteId)> ImportarExcel(Stream excelStream, int usuarioSeguridadId)
    {
        try
        {
            using var package = new ExcelPackage(excelStream);

            if (package.Workbook.Worksheets.Count == 0)
                return (false, "El archivo Excel no contiene hojas de trabajo", 0);

            var worksheet = package.Workbook.Worksheets[0];

            // ── Perfil del cliente ──
            var nombreCompleto = ObtenerValorDerecha(worksheet, "NOMBRE Y EDAD")
                              ?? ObtenerValorDerecha(worksheet, "NOMBRE")
                              ?? worksheet.Cells["G4"].Text;

            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return (false, "No se encontró el nombre del cliente. Verifica el formato del Excel.", 0);

            var pesoInicial = ParseDecimal(ObtenerValorDerecha(worksheet, "PESO INICIAL") ?? worksheet.Cells["G8"].Text);
            var fechaInicio = ParseFecha(ObtenerValorDerecha(worksheet, "FECHA DE INICIO") ?? worksheet.Cells["G10"].Text);
            var objetivos = ObtenerValorDerecha(worksheet, "OBJETIVOS") ?? worksheet.Cells["G6"].Text;
            var objetivoPasos = ObtenerValorDerecha(worksheet, "PASOS") ?? worksheet.Cells["G14"].Text;
            var objetivoCardio = ObtenerValorDerecha(worksheet, "CARDIO") ?? worksheet.Cells["G16"].Text;
            var suplementacion = ObtenerValorDerecha(worksheet, "SUPLEMENTACIÓN")
                              ?? ObtenerValorDerecha(worksheet, "SUPLEMENTACION")
                              ?? worksheet.Cells["G20"].Text;

            // Crear o actualizar perfil
            var cliente = await _context.ClientesPerfil
                .Include(c => c.Macros)
                .Include(c => c.SeguimientoDiario)
                .FirstOrDefaultAsync(c => c.UsuarioSeguridadId == usuarioSeguridadId && !c.Eliminado);

            if (cliente == null)
            {
                cliente = new ClientePerfil
                {
                    UsuarioSeguridadId = usuarioSeguridadId,
                    NombreCompleto = nombreCompleto,
                    PesoInicial = pesoInicial,
                    FechaInicioPrograma = fechaInicio,
                    Objetivos = objetivos,
                    ObjetivoPasos = objetivoPasos,
                    ObjetivoCardio = objetivoCardio,
                    Suplementacion = suplementacion,
                    FechaCreacion = DateTime.UtcNow,
                    TerminosAceptados = true
                };
                _context.ClientesPerfil.Add(cliente);
                // SaveChanges #1 — necesario para obtener cliente.Id (FK)
                await _context.SaveChangesAsync();
            }
            else
            {
                cliente.NombreCompleto = nombreCompleto;
                cliente.PesoInicial = pesoInicial;
                cliente.FechaInicioPrograma = fechaInicio;
                cliente.Objetivos = objetivos;
                cliente.ObjetivoPasos = objetivoPasos;
                cliente.ObjetivoCardio = objetivoCardio;
                cliente.Suplementacion = suplementacion;
                cliente.FechaModificacion = DateTime.UtcNow;
            }

            // ── Macros ──
            CargarMacros(worksheet, cliente.Id, "ENTRENO",
                "MACROS DÍA ENTRENO", "MACROS DIA ENTRENO", "K4", "K6", "K8", "K10");

            CargarMacros(worksheet, cliente.Id, "DESCANSO",
                "MACROS DÍA DESCANSO", "MACROS DIA DESCANSO", "K14", "K16", "K18", "K20");

            // ── Eliminar datos anteriores (medidas + seguimiento) ──
            var medidasViejas = await _context.Set<MedidaCorporal>()
                .Where(m => m.ClienteId == cliente.Id).ToListAsync();
            if (medidasViejas.Count > 0)
                _context.Set<MedidaCorporal>().RemoveRange(medidasViejas);

            var seguimientosViejos = await _context.SeguimientoDiario
                .Where(s => s.ClienteId == cliente.Id).ToListAsync();
            if (seguimientosViejos.Count > 0)
                _context.SeguimientoDiario.RemoveRange(seguimientosViejos);

            // ── Agregar nuevos datos ──
            AgregarMedidas(worksheet, cliente.Id);
            AgregarSeguimientoDiario(worksheet, cliente.Id);

            // SaveChanges #2 — todo junto: macros + borrado + altas
            await _context.SaveChangesAsync();

            return (true, "Excel importado correctamente", cliente.Id);
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? "";
            var sql = dbEx.InnerException?.InnerException?.Message ?? "";
            return (false, $"Error al guardar en base de datos: {dbEx.Message}\n{inner}\n{sql}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Error al importar: {ex.Message}", 0);
        }
    }

    // ═══════════════════════════════════════════════════
    //  IMPORTAR USANDO CELL MAP (enfoque universal)
    //  Lee TODAS las celdas primero, luego busca por contenido.
    //  Funciona con cualquier layout, merges, y pestañas.
    // ═══════════════════════════════════════════════════

    public async Task<(bool Success, string Message, int ClienteId)> ImportarDesdeCellMap(
        Stream excelStream, int usuarioSeguridadId)
    {
        try
        {
            // 1) Leer TODAS las celdas de TODAS las pestañas (con merge resolution)
            var todasLasCeldas = ExcelCellReader.LeerTodasLasCeldas(excelStream);

            if (todasLasCeldas.Count == 0)
                return (false, "El archivo no contiene hojas con datos", 0);

            // Buscar la hoja "DATOS" explícitamente (evita falsos positivos en FAQ, FEEDBACK, etc.)
            List<CellData>? hojaPrincipal = null;
            foreach (var nombre in new[] { "DATOS", "Datos", "datos" })
            {
                if (todasLasCeldas.TryGetValue(nombre, out var h) && h.Count > 0)
                { hojaPrincipal = h; break; }
            }
            // Fallback: primera hoja que contenga "NOMBRE" (patrón TEAM XP)
            hojaPrincipal ??= todasLasCeldas.Values.FirstOrDefault(h =>
                h.Any(c => c.RawText != null && c.RawText.Contains("NOMBRE", StringComparison.OrdinalIgnoreCase)));
            // Último recurso: primera hoja con datos
            hojaPrincipal ??= todasLasCeldas.Values.FirstOrDefault(h => h.Count > 0);

            if (hojaPrincipal == null)
                return (false, "Ninguna hoja contiene datos válidos", 0);

            var celdas = hojaPrincipal;

            // 2) Extraer perfil buscando por CONTENIDO (no por posición fija)
            var nombreCompleto = ExcelCellReader.ObtenerValorJuntoA(celdas, "NOMBRE Y EDAD")
                              ?? ExcelCellReader.ObtenerValorJuntoA(celdas, "NOMBRE");

            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return (false, "No se encontró el nombre del cliente en el archivo.", 0);

            var pesoInicial = ParseDecimal(ExcelCellReader.ObtenerValorJuntoA(celdas, "PESO INICIAL"));
            var fechaInicio = ParseFecha(ExcelCellReader.ObtenerValorJuntoA(celdas, "FECHA DE INICIO"));
            var objetivos = ExcelCellReader.ObtenerValorJuntoA(celdas, "OBJETIVOS");
            var objetivoPasos = ExcelCellReader.ObtenerValorJuntoA(celdas, "PASOS");
            var objetivoCardio = ExcelCellReader.ObtenerValorJuntoA(celdas, "CARDIO");
            var suplementacion = ExcelCellReader.ObtenerValorJuntoA(celdas, "SUPLEMENTACIÓN")
                              ?? ExcelCellReader.ObtenerValorJuntoA(celdas, "SUPLEMENTACION");

            // 3) Crear o actualizar perfil
            var cliente = await _context.ClientesPerfil
                .Include(c => c.Macros).Include(c => c.SeguimientoDiario)
                .FirstOrDefaultAsync(c => c.UsuarioSeguridadId == usuarioSeguridadId && !c.Eliminado);

            if (cliente == null)
            {
                cliente = new ClientePerfil
                {
                    UsuarioSeguridadId = usuarioSeguridadId,
                    NombreCompleto = nombreCompleto,
                    PesoInicial = pesoInicial,
                    FechaInicioPrograma = fechaInicio,
                    Objetivos = objetivos,
                    ObjetivoPasos = objetivoPasos,
                    ObjetivoCardio = objetivoCardio,
                    Suplementacion = suplementacion,
                    FechaCreacion = DateTime.UtcNow,
                    TerminosAceptados = true
                };
                _context.ClientesPerfil.Add(cliente);
                await _context.SaveChangesAsync();
            }
            else
            {
                cliente.NombreCompleto = nombreCompleto;
                cliente.PesoInicial = pesoInicial;
                cliente.FechaInicioPrograma = fechaInicio;
                cliente.Objetivos = objetivos;
                cliente.ObjetivoPasos = objetivoPasos;
                cliente.ObjetivoCardio = objetivoCardio;
                cliente.Suplementacion = suplementacion;
                cliente.FechaModificacion = DateTime.UtcNow;
            }

            // 4) Macros — buscar secciones por contenido
            CargarMacrosDesdeCeldas(celdas, cliente.Id, "ENTRENO", "MACROS DÍA ENTRENO", "MACROS DIA ENTRENO");
            CargarMacrosDesdeCeldas(celdas, cliente.Id, "DESCANSO", "MACROS DÍA DESCANSO", "MACROS DIA DESCANSO");

            // 5) Eliminar datos previos
            var medidasViejas = await _context.Set<MedidaCorporal>()
                .Where(m => m.ClienteId == cliente.Id).ToListAsync();
            if (medidasViejas.Count > 0)
                _context.Set<MedidaCorporal>().RemoveRange(medidasViejas);

            var seguimientosViejos = await _context.SeguimientoDiario
                .Where(s => s.ClienteId == cliente.Id).ToListAsync();
            if (seguimientosViejos.Count > 0)
                _context.SeguimientoDiario.RemoveRange(seguimientosViejos);

            // 6) Medidas corporales — buscar sección INICIO y leer columnas
            AgregarMedidasDesdeCeldas(celdas, cliente.Id);

            // 7) Seguimiento diario — buscar SEMANA y leer filas
            AgregarSeguimientoDesdeCeldas(celdas, cliente.Id);

            await _context.SaveChangesAsync();
            return (true, "Importación completada correctamente", cliente.Id);
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? "";
            return (false, $"Error al guardar: {dbEx.Message}\n{inner}", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Error al importar: {ex.Message}", 0);
        }
    }

    // ── Helpers para importación basada en Cell Map ──

    private void CargarMacrosDesdeCeldas(List<CellData> celdas, int clienteId, string tipoDia, params string[] buscar)
    {
        CellData? titulo = null;
        foreach (var b in buscar)
        {
            titulo = ExcelCellReader.BuscarPorTexto(celdas, b);
            if (titulo != null) break;
        }

        if (titulo == null) return;

        // Buscar etiquetas debajo del título de macros
        var macrosZona = celdas
            .Where(c => c.Row > titulo.Row && c.Row <= titulo.Row + 10 && c.Col == titulo.Col)
            .ToList();

        decimal? proteina = null, grasa = null, carbs = null;
        int? calorias = null;

        foreach (var etiq in macrosZona)
        {
            var label = etiq.RawText?.ToUpper() ?? "";
            var valor = ExcelCellReader.ObtenerValor(celdas, etiq.Row, etiq.Col + 1);

            if (label.Contains("PROTE")) proteina = ParseDecimal(valor);
            else if (label.Contains("GRASA")) grasa = ParseDecimal(valor);
            else if (label.Contains("CARB")) carbs = ParseDecimal(valor);
            else if (label.Contains("CALOR") || label.Contains("KCAL")) calorias = ParseInt(valor);
        }

        var macros = _context.ClienteMacros.Local.FirstOrDefault(m => m.ClienteId == clienteId && m.TipoDia == tipoDia)
                  ?? _context.ClienteMacros.FirstOrDefault(m => m.ClienteId == clienteId && m.TipoDia == tipoDia);

        if (macros == null)
        {
            macros = new ClienteMacros { ClienteId = clienteId, TipoDia = tipoDia };
            _context.ClienteMacros.Add(macros);
        }

        macros.Proteina = proteina;
        macros.Grasa = grasa;
        macros.Carbohidratos = carbs;
        macros.Calorias = calorias;
    }

    private void AgregarMedidasDesdeCeldas(List<CellData> celdas, int clienteId)
    {
        // Diagnóstico real: MEDIDAS=F23, INICIO=F24
        // Buscar "INICIO" — ancla de columnas de medidas
        var inicio = ExcelCellReader.BuscarPorTexto(celdas, "INICIO");
        if (inicio == null) return;

        // Encabezados de semana en la fila de INICIO (F24: INICIO, G24: SEMANA 2, H24: SEMANA 4...)
        var encabezados = celdas
            .Where(c => c.Row == inicio.Row && c.Col >= inicio.Col)
            .OrderBy(c => c.Col)
            .Take(7)
            .ToList();

        if (encabezados.Count == 0) return;

        // Buscar PECHO como ancla de datos (puede estar hasta 8 filas debajo de INICIO)
        var pechoCelda = celdas.FirstOrDefault(c =>
            c.Row > inicio.Row && c.Row <= inicio.Row + 8 &&
            c.RawText != null && c.RawText.Contains("PECHO", StringComparison.OrdinalIgnoreCase));

        // Si no encuentra PECHO, intentar buscar por patrón: primera fila con datos numéricos
        // debajo de INICIO en la misma columna
        int filaPecho;
        if (pechoCelda != null)
        {
            filaPecho = pechoCelda.Row;
        }
        else
        {
            // Fallback: INICIO+2 (fila de INICIO + 1 fecha + 1 datos)
            filaPecho = inicio.Row + 2;
        }

        // Fechas: buscar entre INICIO y PECHO
        int filaFechas = filaPecho - 1;

        for (int i = 0; i < encabezados.Count; i++)
        {
            int col = encabezados[i].Col;

            // Fecha: probar la fila de fechas en col y col+1 (por si hay merge desplazado)
            var fecha = ParseFecha(ExcelCellReader.ObtenerValor(celdas, filaFechas, col))
                     ?? ParseFecha(ExcelCellReader.ObtenerValor(celdas, filaFechas, col + 1));

            var pecho = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho, col));
            var brazo = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho + 1, col));
            var cinturaSobre = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho + 2, col));
            var cinturaOmbligo = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho + 3, col));
            var cinturaBajo = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho + 4, col));
            var cadera = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho + 5, col));
            var muslos = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, filaPecho + 6, col));

            if (pecho.HasValue || brazo.HasValue || cinturaOmbligo.HasValue ||
                cinturaSobre.HasValue || cinturaBajo.HasValue || cadera.HasValue || muslos.HasValue)
            {
                _context.Set<MedidaCorporal>().Add(new MedidaCorporal
                {
                    ClienteId = clienteId, NumeroSemana = i, Fecha = fecha,
                    Pecho = pecho, Brazo = brazo,
                    CinturaSobreOmbligo = cinturaSobre, CinturaOmbligo = cinturaOmbligo,
                    CinturaBajoOmbligo = cinturaBajo, Cadera = cadera, Muslos = muslos
                });
            }
        }
    }

    private void AgregarSeguimientoDesdeCeldas(List<CellData> celdas, int clienteId)
    {
        // Buscar encabezado "SEMANA" en columna 1 (A)
        var semanaHeader = celdas.FirstOrDefault(c =>
            c.Col == 1 && c.RawText != null &&
            c.RawText.Equals("SEMANA", StringComparison.OrdinalIgnoreCase));

        if (semanaHeader == null) return;

        var fechasAgregadas = new HashSet<DateTime>();

        // Recorrer filas desde la fila del encabezado + 1
        for (int fila = semanaHeader.Row + 1; fila <= semanaHeader.Row + 250; fila++)
        {
            var semanaTexto = ExcelCellReader.ObtenerValor(celdas, fila, 1);
            var fechaTexto = ExcelCellReader.ObtenerValor(celdas, fila, 2);

            if (string.IsNullOrWhiteSpace(fechaTexto)) continue;

            // Saltar filas de control
            if (semanaTexto != null && (
                semanaTexto.Equals("EJEMPLO", StringComparison.OrdinalIgnoreCase) ||
                semanaTexto.Contains("RESUMEN", StringComparison.OrdinalIgnoreCase) ||
                semanaTexto.Equals("SEMANA Nº", StringComparison.OrdinalIgnoreCase)))
                continue;

            if (fechaTexto.Equals("REVISION", StringComparison.OrdinalIgnoreCase))
                continue;

            var fecha = ParseFecha(fechaTexto);
            if (!fecha.HasValue || !fechasAgregadas.Add(fecha.Value.Date))
                continue;

            _context.SeguimientoDiario.Add(new SeguimientoDiario
            {
                ClienteId = clienteId,
                NumeroSemana = ParseInt(semanaTexto),
                Fecha = fecha.Value,
                Peso = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, fila, 3)),
                HoraPesaje = ParseHora(ExcelCellReader.ObtenerValor(celdas, fila, 4)),
                Proteina = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, fila, 5)),
                Grasa = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, fila, 6)),
                Carbohidratos = ParseDecimal(ExcelCellReader.ObtenerValor(celdas, fila, 7)),
                TotalCalorias = ParseInt(ExcelCellReader.ObtenerValor(celdas, fila, 8)),
                DiaEntreno = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 9), 100),
                RendimientoSesion = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 10), 100),
                PasosRealizados = ParseInt(ExcelCellReader.ObtenerValor(celdas, fila, 11)),
                DuracionCardio = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 12), 50),
                HorasSueno = ParseDecimalOHoras(ExcelCellReader.ObtenerValor(celdas, fila, 13)),
                CalidadSueno = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 14), 50),
                Apetito = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 15), 50),
                NivelEstres = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 16), 50),
                Notas = TruncateString(ExcelCellReader.ObtenerValor(celdas, fila, 17), 500),
                EntrenoRealizado = !string.IsNullOrWhiteSpace(ExcelCellReader.ObtenerValor(celdas, fila, 9)),
                CardioRealizado = !string.IsNullOrWhiteSpace(ExcelCellReader.ObtenerValor(celdas, fila, 12))
            });
        }
    }

    // ── Helpers de importación (método original) ──

    private void CargarMacros(ExcelWorksheet ws, int clienteId, string tipoDia,
        string buscar1, string buscar2, string fallbackProt, string fallbackGrasa, string fallbackCarbs, string fallbackCals)
    {
        var (prot, grasa, carbs, cals) = BuscarMacros(ws, buscar1, buscar2);

        var macros = _context.ClienteMacros
            .Local.FirstOrDefault(m => m.ClienteId == clienteId && m.TipoDia == tipoDia)
            ?? _context.ClienteMacros
                .FirstOrDefault(m => m.ClienteId == clienteId && m.TipoDia == tipoDia);

        if (macros == null)
        {
            macros = new ClienteMacros { ClienteId = clienteId, TipoDia = tipoDia };
            _context.ClienteMacros.Add(macros);
        }

        macros.Proteina = prot ?? ParseDecimal(ws.Cells[fallbackProt].Text);
        macros.Grasa = grasa ?? ParseDecimal(ws.Cells[fallbackGrasa].Text);
        macros.Carbohidratos = carbs ?? ParseDecimal(ws.Cells[fallbackCarbs].Text);
        macros.Calorias = cals ?? ParseInt(ws.Cells[fallbackCals].Text);
    }

    private void AgregarMedidas(ExcelWorksheet ws, int clienteId)
    {
        int filaMedidas = BuscarFilaPorTexto(ws, "MEDIDAS", 20, 35, 1, 15) ?? 24;

        // Buscar "INICIO" en las filas cercanas a MEDIDAS (puede estar 1-3 filas abajo)
        // Usa LeerCelda para resolver celdas combinadas
        int filaEncabezados = -1;
        int columnaInicio = -1;
        for (int fila = filaMedidas; fila <= filaMedidas + 3; fila++)
        {
            for (int col = 1; col <= 15; col++)
            {
                if (LeerCelda(ws, fila, col)?.Equals("INICIO", StringComparison.OrdinalIgnoreCase) == true)
                {
                    filaEncabezados = fila;
                    columnaInicio = col;
                    break;
                }
            }
            if (filaEncabezados > 0) break;
        }

        // Fallback: estructura típica del Excel TEAM XP
        if (filaEncabezados == -1)
        {
            filaEncabezados = filaMedidas + 1;
            columnaInicio = 6; // Col F
        }

        // Las fechas están 1 fila debajo de los encabezados, los datos 2 filas debajo
        int filaFechas = filaEncabezados + 1;
        int filaDatos = filaEncabezados + 2;

        // Auto-detectar: si la fila de fechas no tiene datos, probar la siguiente
        if (ParseFecha(LeerCelda(ws, filaFechas, columnaInicio)) == null &&
            ParseFecha(LeerCelda(ws, filaFechas, columnaInicio + 1)) == null)
        {
            filaFechas++;
            filaDatos++;
        }

        for (int colOffset = 0; colOffset < 7; colOffset++)
        {
            int col = columnaInicio + colOffset;

            // Intentar fecha en col actual y col+1 (celdas combinadas pueden desplazarlas)
            var fecha = ParseFecha(LeerCelda(ws, filaFechas, col))
                     ?? ParseFecha(LeerCelda(ws, filaFechas, col + 1));

            var pecho = ParseDecimal(LeerCelda(ws, filaDatos, col));
            var brazo = ParseDecimal(LeerCelda(ws, filaDatos + 1, col));
            var cinturaSobre = ParseDecimal(LeerCelda(ws, filaDatos + 2, col));
            var cinturaOmbligo = ParseDecimal(LeerCelda(ws, filaDatos + 3, col));
            var cinturaBajo = ParseDecimal(LeerCelda(ws, filaDatos + 4, col));
            var cadera = ParseDecimal(LeerCelda(ws, filaDatos + 5, col));
            var muslos = ParseDecimal(LeerCelda(ws, filaDatos + 6, col));

            if (pecho.HasValue || brazo.HasValue || cinturaOmbligo.HasValue ||
                cinturaSobre.HasValue || cinturaBajo.HasValue || cadera.HasValue || muslos.HasValue)
            {
                _context.Set<MedidaCorporal>().Add(new MedidaCorporal
                {
                    ClienteId = clienteId,
                    NumeroSemana = colOffset,
                    Fecha = fecha,
                    Pecho = pecho,
                    Brazo = brazo,
                    CinturaSobreOmbligo = cinturaSobre,
                    CinturaOmbligo = cinturaOmbligo,
                    CinturaBajoOmbligo = cinturaBajo,
                    Cadera = cadera,
                    Muslos = muslos
                });
            }
        }
    }

    private void AgregarSeguimientoDiario(ExcelWorksheet ws, int clienteId)
    {
        int filaEncabezados = BuscarFilaPorTexto(ws, "SEMANA", 35, 50, 1, 1) ?? 39;
        int filaInicio = filaEncabezados + 4;
        var fechasAgregadas = new HashSet<DateTime>();

        for (int fila = filaInicio; fila < filaInicio + 200; fila++)
        {
            var semanaTexto = LeerCelda(ws, fila, 1);
            var fechaTexto = LeerCelda(ws, fila, 2);

            if (string.IsNullOrWhiteSpace(fechaTexto))
                continue;

            // Saltar filas de ejemplo, resumen, revisión y encabezados repetidos
            if (semanaTexto != null && (
                semanaTexto.Equals("EJEMPLO", StringComparison.OrdinalIgnoreCase) ||
                semanaTexto.Contains("RESUMEN", StringComparison.OrdinalIgnoreCase) ||
                semanaTexto.Equals("SEMANA Nº", StringComparison.OrdinalIgnoreCase)))
                continue;

            // "REVISION" puede aparecer en columna de fecha en filas RESUMEN
            if (fechaTexto.Equals("REVISION", StringComparison.OrdinalIgnoreCase))
                continue;

            var fecha = ParseFecha(fechaTexto);
            if (!fecha.HasValue || !fechasAgregadas.Add(fecha.Value.Date))
                continue;

            _context.SeguimientoDiario.Add(new SeguimientoDiario
            {
                ClienteId = clienteId,
                NumeroSemana = ParseInt(semanaTexto),
                Fecha = fecha.Value,
                Peso = ParseDecimal(LeerCelda(ws, fila, 3)),
                HoraPesaje = ParseHora(LeerCelda(ws, fila, 4)),
                Proteina = ParseDecimal(LeerCelda(ws, fila, 5)),
                Grasa = ParseDecimal(LeerCelda(ws, fila, 6)),
                Carbohidratos = ParseDecimal(LeerCelda(ws, fila, 7)),
                TotalCalorias = ParseInt(LeerCelda(ws, fila, 8)),
                DiaEntreno = TruncateString(LeerCelda(ws, fila, 9), 100),
                RendimientoSesion = TruncateString(LeerCelda(ws, fila, 10), 100),
                PasosRealizados = ParseInt(LeerCelda(ws, fila, 11)),
                DuracionCardio = TruncateString(LeerCelda(ws, fila, 12), 50),
                HorasSueno = ParseDecimalOHoras(LeerCelda(ws, fila, 13)),
                CalidadSueno = TruncateString(LeerCelda(ws, fila, 14), 50),
                Apetito = TruncateString(LeerCelda(ws, fila, 15), 50),
                NivelEstres = TruncateString(LeerCelda(ws, fila, 16), 50),
                Notas = TruncateString(LeerCelda(ws, fila, 17), 500),
                EntrenoRealizado = !string.IsNullOrWhiteSpace(LeerCelda(ws, fila, 9)),
                CardioRealizado = !string.IsNullOrWhiteSpace(LeerCelda(ws, fila, 12))
            });
        }
    }

    // ═══════════════════════════════════════════════════
    //  EXPORTAR EXCEL
    // ═══════════════════════════════════════════════════

    public async Task<byte[]> ExportarExcel(int clienteId)
    {
        var cliente = await _context.ClientesPerfil
            .Include(c => c.Macros)
            .Include(c => c.SeguimientoDiario)
            .FirstOrDefaultAsync(c => c.Id == clienteId && !c.Eliminado)
            ?? throw new Exception("Cliente no encontrado");

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Seguimiento");

        // ── Perfil ──
        EscribirPar(ws, "F4", "NOMBRE Y EDAD", "G4", cliente.NombreCompleto);
        EscribirPar(ws, "F6", "OBJETIVOS", "G6", cliente.Objetivos);
        ws.Cells["F8"].Value = "PESO INICIAL";
        ws.Cells["G8"].Value = cliente.PesoInicial;   // numérico, no "70kg"
        EscribirPar(ws, "F10", "FECHA DE INICIO", "G10",
            cliente.FechaInicioPrograma?.ToString("dd MMMM yyyy", new CultureInfo("es-ES")));
        EscribirPar(ws, "F14", "PASOS", "G14", cliente.ObjetivoPasos);
        EscribirPar(ws, "F16", "CARDIO", "G16", cliente.ObjetivoCardio);
        EscribirPar(ws, "F18", "SUPLEMENTACIÓN", "G18", cliente.Suplementacion);

        // ── Macros ──
        ExportarMacros(ws, cliente.Macros.FirstOrDefault(m => m.TipoDia == "ENTRENO"), "J2", 4);
        ExportarMacros(ws, cliente.Macros.FirstOrDefault(m => m.TipoDia == "DESCANSO"), "J12", 14);

        // ── Medidas corporales ──
        var medidas = await _context.Set<MedidaCorporal>()
            .Where(m => m.ClienteId == clienteId)
            .OrderBy(m => m.NumeroSemana)
            .ToListAsync();

        int fm = 24; // fila base medidas
        ws.Cells[fm, 4].Value = "MEDIDAS";
        string[] etiquetasMedidas = ["PECHO", "BRAZO", "3cm SOBRE OMBLIGO", "OMBLIGO", "3cm BAJO OMBLIGO", "CADERA", "MUSLOS"];
        for (int i = 0; i < etiquetasMedidas.Length; i++)
            ws.Cells[fm + 2 + i, 4].Value = etiquetasMedidas[i];

        int colMedida = 5; // columna E — alineada con ImportarMedidas
        int medidaIdx = 0;
        foreach (var m in medidas)
        {
            ws.Cells[fm, colMedida].Value = medidaIdx == 0 ? "INICIO" : $"SEMANA {medidaIdx * 2}";
            ws.Cells[fm + 1, colMedida].Value = m.Fecha;
            ws.Cells[fm + 1, colMedida].Style.Numberformat.Format = "dd/MM/yyyy";
            ws.Cells[fm + 2, colMedida].Value = m.Pecho;
            ws.Cells[fm + 3, colMedida].Value = m.Brazo;
            ws.Cells[fm + 4, colMedida].Value = m.CinturaSobreOmbligo;
            ws.Cells[fm + 5, colMedida].Value = m.CinturaOmbligo;
            ws.Cells[fm + 6, colMedida].Value = m.CinturaBajoOmbligo;
            ws.Cells[fm + 7, colMedida].Value = m.Cadera;
            ws.Cells[fm + 8, colMedida].Value = m.Muslos;
            colMedida++;
            medidaIdx++;
        }

        // ── Seguimiento diario ──
        int fs = 39; // fila base seguimiento
        string[] encabezados = [
            "SEMANA", "FECHA", "PESO EN AYUNAS", "HORA DEL PESAJE", "PROTEINA", "GRASA",
            "CARBS", "TOTAL CALORÍAS", "ENTRENO", "RENDIMIENTO DE LA SESIÓN", "PASOS", "CARDIO",
            "DURACIÓN SUEÑO EN HORAS", "CALIDAD SUEÑO", "APETITO", "NIVEL DE ESTRÉS", "NOTAS EXTRA"
        ];
        for (int i = 0; i < encabezados.Length; i++)
            ws.Cells[fs, i + 1].Value = encabezados[i];

        // Fila EJEMPLO (el import la salta)
        ws.Cells[fs + 1, 1].Value = "EJEMPLO";
        ws.Cells[fs + 1, 2].Value = "01-ene";
        ws.Cells[fs + 1, 3].Value = 70;

        // Datos reales en fs+4, alineado con AgregarSeguimientoDiario
        int filaActual = fs + 4;
        foreach (var seg in cliente.SeguimientoDiario.OrderBy(s => s.Fecha))
        {
            ws.Cells[filaActual, 1].Value = seg.NumeroSemana;
            ws.Cells[filaActual, 2].Value = seg.Fecha.ToString("dd-MMM", new CultureInfo("es-ES"));
            ws.Cells[filaActual, 3].Value = seg.Peso;
            ws.Cells[filaActual, 4].Value = seg.HoraPesaje?.ToString(@"hh\:mm");
            ws.Cells[filaActual, 5].Value = seg.Proteina;
            ws.Cells[filaActual, 6].Value = seg.Grasa;
            ws.Cells[filaActual, 7].Value = seg.Carbohidratos;
            ws.Cells[filaActual, 8].Value = seg.TotalCalorias;
            ws.Cells[filaActual, 9].Value = seg.DiaEntreno;
            ws.Cells[filaActual, 10].Value = seg.RendimientoSesion;
            ws.Cells[filaActual, 11].Value = seg.PasosRealizados;
            ws.Cells[filaActual, 12].Value = seg.DuracionCardio;
            ws.Cells[filaActual, 13].Value = seg.HorasSueno;
            ws.Cells[filaActual, 14].Value = seg.CalidadSueno;
            ws.Cells[filaActual, 15].Value = seg.Apetito;
            ws.Cells[filaActual, 16].Value = seg.NivelEstres;
            ws.Cells[filaActual, 17].Value = seg.Notas;
            filaActual++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private static void ExportarMacros(ExcelWorksheet ws, ClienteMacros? macros, string celdaTitulo, int filaBase)
    {
        var esTipoEntreno = celdaTitulo == "J2";
        ws.Cells[celdaTitulo].Value = esTipoEntreno ? "MACROS DÍA ENTRENO" : "MACROS DÍA DESCANSO";
        ws.Cells[$"J{filaBase}"].Value = "PROTEÍNA";
        ws.Cells[$"K{filaBase}"].Value = macros?.Proteina;
        ws.Cells[$"J{filaBase + 2}"].Value = "GRASA";
        ws.Cells[$"K{filaBase + 2}"].Value = macros?.Grasa;
        ws.Cells[$"J{filaBase + 4}"].Value = "CARBOHIDRATOS";
        ws.Cells[$"K{filaBase + 4}"].Value = macros?.Carbohidratos;
        ws.Cells[$"J{filaBase + 6}"].Value = "CALORÍAS";
        ws.Cells[$"K{filaBase + 6}"].Value = macros?.Calorias;
    }

    private static void EscribirPar(ExcelWorksheet ws, string celdaEtiqueta, string etiqueta,
        string celdaValor, object? valor)
    {
        ws.Cells[celdaEtiqueta].Value = etiqueta;
        ws.Cells[celdaValor].Value = valor;
    }

    // ═══════════════════════════════════════════════════
    //  UTILIDADES DE PARSING
    // ═══════════════════════════════════════════════════

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Ignorar errores de fórmulas Excel (#DIV/0!, #REF!, #N/A, #VALUE!, etc.)
        if (value.TrimStart().StartsWith('#'))
            return null;

        // Quitar unidades y espacios
        value = value
            .Replace("kg", "", StringComparison.OrdinalIgnoreCase)
            .Replace("kcal", "", StringComparison.OrdinalIgnoreCase)
            .Replace("cm", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "")
            .Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Normalizar separadores decimales
        int puntos = value.Count(c => c == '.');
        int comas = value.Count(c => c == ',');

        if (puntos > 0 && comas > 0)
        {
            // Formato europeo 1.234,56
            value = value.Replace(".", "").Replace(",", ".");
        }
        else if (comas == 1 && puntos == 0)
        {
            // Decimal español 70,2
            value = value.Replace(",", ".");
        }
        else if (comas > 1)
        {
            // Miles anglosajón 1,234,567
            value = value.Replace(",", "");
        }
        else if (puntos > 1)
        {
            // Miles europeo 1.234.567
            var parts = value.Split('.');
            value = string.Join("", parts.Take(parts.Length - 1)) + "." + parts.Last();
        }

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Primero intentar como decimal y truncar (maneja "8.0" → 8)
        var dec = ParseDecimal(value);
        if (dec.HasValue)
            return (int)dec.Value;

        // Fallback: solo dígitos y signo negativo
        var limpio = new string(value.Where(c => char.IsDigit(c) || c == '-').ToArray());
        return int.TryParse(limpio, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static DateTime? ParseFecha(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Número de Excel (OADate)
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double oaDate))
        {
            try { return DateTime.FromOADate(oaDate); }
            catch { /* no era OADate */ }
        }

        // Formatos exactos
        // Formatos exactos (incluye los del Excel TEAM XP: "19 ene", "1 feb", etc.)
        var formatos = new[]
        {
            "d/M/yyyy", "dd/MM/yyyy", "d-MMM-yyyy", "dd-MMM-yyyy",
            "d MMMM yyyy", "dd MMMM yyyy", "M/d/yyyy",
            "d MMM", "dd MMM", "d-MMM", "dd-MMM",
            "d MMM yyyy", "dd MMM yyyy",
            "d' 'MMM", "dd' 'MMM",     // Forzar espacio literal
            "d MMM.", "dd MMM.",         // "ene." con punto (algunas culturas)
        };

        var culturas = new[] { new CultureInfo("es-ES"), new CultureInfo("en-US"), CultureInfo.InvariantCulture };

        foreach (var cultura in culturas)
        {
            foreach (var formato in formatos)
            {
                if (DateTime.TryParseExact(value, formato, cultura, DateTimeStyles.None, out var result))
                {
                    if (result.Year < 2000)
                        result = new DateTime(DateTime.Now.Year, result.Month, result.Day);
                    return result;
                }
            }
        }

        // Parse general
        if (DateTime.TryParse(value, new CultureInfo("es-ES"), DateTimeStyles.None, out var fecha))
        {
            if (fecha.Year < 2000)
                fecha = new DateTime(DateTime.Now.Year, fecha.Month, fecha.Day);
            return fecha;
        }

        return null;
    }

    private static TimeSpan? ParseHora(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        if (TimeSpan.TryParse(value, out var ts))
            return ts;

        // HH:mm
        var partes = value.Split(':');
        if (partes.Length == 2
            && int.TryParse(partes[0], out int h) && h is >= 0 and < 24
            && int.TryParse(partes[1], out int m) && m is >= 0 and < 60)
        {
            return new TimeSpan(h, m, 0);
        }

        // Fracción de día de Excel (0.33 → ~08:00)
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double frac) && frac < 1.0)
            return TimeSpan.FromMinutes((int)(frac * 24 * 60));

        return null;
    }

    /// <summary>
    /// Parsea un valor como decimal o, si tiene formato hora (7:00, 8:30), lo convierte a horas decimales.
    /// Usado para "DURACIÓN SUEÑO EN HORAS" que en el Excel real viene como "7:00" en vez de 7.
    /// </summary>
    private static decimal? ParseDecimalOHoras(string? value)
    {
        var dec = ParseDecimal(value);
        if (dec.HasValue)
            return dec;

        // "7:00" → 7.0h, "7:30" → 7.5h
        var hora = ParseHora(value);
        if (hora.HasValue)
            return (decimal)hora.Value.TotalHours;

        return null;
    }

    private static string? TruncateString(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? value
         : value.Length <= maxLength ? value
         : value[..maxLength];

    // ═══════════════════════════════════════════════════
    //  CELDAS COMBINADAS (MERGED CELLS)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Lee el texto de una celda resolviendo celdas combinadas (merged).
    /// Si la celda está dentro de un rango combinado, devuelve el valor de la primera celda del rango.
    /// Vital para el Excel "ALBERTO FASE 1" que usa encabezados combinados por diseño estético.
    /// </summary>
    private static string? LeerCelda(ExcelWorksheet ws, int row, int col)
    {
        var celda = ws.Cells[row, col];
        var texto = celda.Text?.Trim();

        // Si ya tiene valor, devolverlo directamente
        if (!string.IsNullOrEmpty(texto))
            return texto;

        // Si está mergeada, buscar el valor en la celda principal del merge
        if (celda.Merge)
        {
            foreach (var mergedRange in ws.MergedCells)
            {
                if (mergedRange == null) continue;
                var rango = ws.Cells[mergedRange];
                if (row >= rango.Start.Row && row <= rango.End.Row &&
                    col >= rango.Start.Column && col <= rango.End.Column)
                {
                    return ws.Cells[rango.Start.Row, rango.Start.Column].Text?.Trim();
                }
            }
        }

        return texto;
    }

    // ═══════════════════════════════════════════════════
    //  UTILIDADES DE BÚSQUEDA EN WORKSHEET
    // ═══════════════════════════════════════════════════

    private static int? BuscarFilaPorTexto(ExcelWorksheet ws, string texto,
        int filaDesde, int filaHasta, int colDesde, int colHasta)
    {
        for (int fila = filaDesde; fila <= filaHasta; fila++)
        {
            for (int col = colDesde; col <= colHasta; col++)
            {
                var celda = LeerCelda(ws, fila, col);
                if (celda != null && celda.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    return fila;
            }
        }
        return null;
    }

    private static (int row, int col)? BuscarCelda(ExcelWorksheet ws, string textoBuscar,
        int maxRows = 30, int maxCols = 20)
    {
        for (int row = 1; row <= maxRows; row++)
        {
            for (int col = 1; col <= maxCols; col++)
            {
                var cellText = LeerCelda(ws, row, col);
                if (!string.IsNullOrEmpty(cellText) &&
                    cellText.Contains(textoBuscar, StringComparison.OrdinalIgnoreCase))
                {
                    return (row, col);
                }
            }
        }
        return null;
    }

    private static string? ObtenerValorDerecha(ExcelWorksheet ws, string etiqueta, int offsetCol = 1)
    {
        var celda = BuscarCelda(ws, etiqueta);
        if (!celda.HasValue) return null;

        // Intentar la celda derecha, y si está mergeada resolver
        var valor = LeerCelda(ws, celda.Value.row, celda.Value.col + offsetCol);

        // Si la etiqueta misma es parte de un merge ancho, el valor puede estar más a la derecha
        if (string.IsNullOrEmpty(valor))
            valor = LeerCelda(ws, celda.Value.row, celda.Value.col + offsetCol + 1);

        return valor;
    }

    private (decimal? Proteina, decimal? Grasa, decimal? Carbohidratos, int? Calorias) BuscarMacros(
        ExcelWorksheet ws, params string[] titulosBuscar)
    {
        (int row, int col)? titulo = null;
        foreach (var buscar in titulosBuscar)
        {
            titulo = BuscarCelda(ws, buscar);
            if (titulo.HasValue) break;
        }

        if (!titulo.HasValue)
            return (null, null, null, null);

        int colMacros = titulo.Value.col + 1;
        int filaBase = titulo.Value.row;

        decimal? proteina = null, grasa = null, carbohidratos = null;
        int? calorias = null;

        for (int offset = 1; offset <= 10; offset++)
        {
            var etiqueta = LeerCelda(ws, filaBase + offset, titulo.Value.col)?.ToUpper();
            var valor = LeerCelda(ws, filaBase + offset, colMacros);

            if (etiqueta == null) continue;

            if (etiqueta.Contains("PROTE") || etiqueta.Contains("PROTEIN"))
                proteina = ParseDecimal(valor);
            else if (etiqueta.Contains("GRASA") || etiqueta.Contains("FAT"))
                grasa = ParseDecimal(valor);
            else if (etiqueta.Contains("CARB"))
                carbohidratos = ParseDecimal(valor);
            else if (etiqueta.Contains("CALOR") || etiqueta.Contains("KCAL"))
                calorias = ParseInt(valor);
        }

        return (proteina, grasa, carbohidratos, calorias);
    }
}
