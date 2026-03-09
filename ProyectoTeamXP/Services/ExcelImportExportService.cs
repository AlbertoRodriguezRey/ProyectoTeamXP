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

    public async Task<(bool Success, string Message, int ClienteId)> ImportarExcel(Stream excelStream, int usuarioSeguridadId)
    {
        try
        {
            using var package = new ExcelPackage(excelStream);

            if (package.Workbook.Worksheets.Count == 0)
                return (false, "El archivo Excel no contiene hojas de trabajo", 0);

            var worksheet = package.Workbook.Worksheets[0];

            // Buscar datos del perfil de forma dinámica
            var nombreCompleto = ObtenerValorDerecha(worksheet, "NOMBRE Y EDAD") ?? 
                               ObtenerValorDerecha(worksheet, "NOMBRE");

            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                // Intentar con coordenadas fijas como fallback
                nombreCompleto = worksheet.Cells["G4"].Text;
            }

            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return (false, "No se encontró el nombre del cliente. Verifica el formato del Excel.", 0);

            var pesoInicial = ParseDecimal(ObtenerValorDerecha(worksheet, "PESO INICIAL") ?? worksheet.Cells["G8"].Text);
            var fechaInicio = ParseFecha(ObtenerValorDerecha(worksheet, "FECHA DE INICIO") ?? worksheet.Cells["G10"].Text);
            var objetivos = ObtenerValorDerecha(worksheet, "OBJETIVOS") ?? worksheet.Cells["G6"].Text;
            var objetivoPasos = ObtenerValorDerecha(worksheet, "PASOS") ?? worksheet.Cells["G14"].Text;
            var objetivoCardio = ObtenerValorDerecha(worksheet, "CARDIO") ?? worksheet.Cells["G16"].Text;
            var suplementacion = ObtenerValorDerecha(worksheet, "SUPLEMENTACIÓN") ?? 
                                ObtenerValorDerecha(worksheet, "SUPLEMENTACION") ?? 
                                worksheet.Cells["G20"].Text;

            // Crear o actualizar perfil del cliente
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

            // Extraer macros - buscar la sección de macros dinámicamente
            var (macrosEntrenoProteina, macrosEntrenoGrasa, macrosEntrenoCarbos, macrosEntrenoCals) = 
                BuscarMacros(worksheet, "MACROS DÍA ENTRENO", "MACROS DIA ENTRENO");

            var (macrosDescansoProteina, macrosDescansoGrasa, macrosDescansoCarbos, macrosDescansoCals) = 
                BuscarMacros(worksheet, "MACROS DÍA DESCANSO", "MACROS DIA DESCANSO");

            // Extraer macros de día de entreno
            var macrosEntreno = await _context.ClienteMacros
                .FirstOrDefaultAsync(m => m.ClienteId == cliente.Id && m.TipoDia == "ENTRENO");

            if (macrosEntreno == null)
            {
                macrosEntreno = new ClienteMacros { ClienteId = cliente.Id, TipoDia = "ENTRENO" };
                _context.ClienteMacros.Add(macrosEntreno);
            }

            macrosEntreno.Proteina = macrosEntrenoProteina ?? ParseDecimal(worksheet.Cells["K4"].Text);
            macrosEntreno.Grasa = macrosEntrenoGrasa ?? ParseDecimal(worksheet.Cells["K6"].Text);
            macrosEntreno.Carbohidratos = macrosEntrenoCarbos ?? ParseDecimal(worksheet.Cells["K8"].Text);
            macrosEntreno.Calorias = macrosEntrenoCals ?? ParseInt(worksheet.Cells["K10"].Text);

            // Extraer macros de día de descanso
            var macrosDescanso = await _context.ClienteMacros
                .FirstOrDefaultAsync(m => m.ClienteId == cliente.Id && m.TipoDia == "DESCANSO");

            if (macrosDescanso == null)
            {
                macrosDescanso = new ClienteMacros { ClienteId = cliente.Id, TipoDia = "DESCANSO" };
                _context.ClienteMacros.Add(macrosDescanso);
            }

            macrosDescanso.Proteina = macrosDescansoProteina ?? ParseDecimal(worksheet.Cells["K14"].Text);
            macrosDescanso.Grasa = macrosDescansoGrasa ?? ParseDecimal(worksheet.Cells["K16"].Text);
            macrosDescanso.Carbohidratos = macrosDescansoCarbos ?? ParseDecimal(worksheet.Cells["K18"].Text);
            macrosDescanso.Calorias = macrosDescansoCals ?? ParseInt(worksheet.Cells["K20"].Text);

            await _context.SaveChangesAsync();

            // Importar medidas corporales (desde fila 26)
            try
            {
                await ImportarMedidas(worksheet, cliente.Id);
            }
            catch (Exception ex)
            {
                return (false, $"Error al importar medidas: {ex.Message}", cliente.Id);
            }

            // Importar seguimiento diario (desde fila 40 aproximadamente)
            try
            {
                await ImportarSeguimientoDiario(worksheet, cliente.Id);
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "";
                return (false, $"Error al importar seguimiento diario: {ex.Message}\nInner: {innerMsg}", cliente.Id);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                var innerMsg = dbEx.InnerException?.Message ?? "";
                var details = "";

                if (dbEx.InnerException != null)
                {
                    var sqlException = dbEx.InnerException.InnerException;
                    if (sqlException != null)
                        details = $"\nSQL Error: {sqlException.Message}";
                }

                return (false, $"Error al guardar en base de datos: {dbEx.Message}{details}\nInner: {innerMsg}", cliente.Id);
            }

            return (true, "Excel importado correctamente", cliente.Id);
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? "";
            return (false, $"Error general al importar Excel: {ex.Message}\nInner: {innerMsg}\nStack: {ex.StackTrace}", 0);
        }
    }

    private async Task ImportarMedidas(ExcelWorksheet worksheet, int clienteId)
    {
        // Eliminar medidas existentes
        var existentes = await _context.Set<MedidaCorporal>()
            .Where(m => m.ClienteId == clienteId)
            .ToListAsync();

        if (existentes.Any())
        {
            _context.Set<MedidaCorporal>().RemoveRange(existentes);
            await _context.SaveChangesAsync();
        }

        // Buscar la fila donde empieza "MEDIDAS"
        int filaInicio = -1;
        for (int fila = 20; fila < 35; fila++)
        {
            for (int col = 1; col <= 10; col++)
            {
                var celda = worksheet.Cells[fila, col].Text?.Trim().ToUpper();
                if (celda != null && celda.Contains("MEDIDAS"))
                {
                    filaInicio = fila;
                    break;
                }
            }
            if (filaInicio > 0) break;
        }

        if (filaInicio == -1)
            filaInicio = 24; // Valor por defecto

        // Buscar la columna donde empiezan las medidas (buscar "INICIO")
        int columnaInicio = -1;
        for (int col = 1; col <= 15; col++)
        {
            var celda = worksheet.Cells[filaInicio, col].Text?.Trim().ToUpper();
            if (celda == "INICIO")
            {
                columnaInicio = col;
                break;
            }
        }

        if (columnaInicio == -1)
            columnaInicio = 5; // Columna E por defecto

        int medidasAgregadas = 0;

        // Leer las columnas de medidas (INICIO, SEMANA 2, SEMANA 4, etc.)
        for (int colOffset = 0; colOffset < 7; colOffset++)
        {
            int colIndex = columnaInicio + colOffset;
            
            // Obtener la fecha de la fila siguiente al título
            var fechaTexto = worksheet.Cells[filaInicio + 1, colIndex].Text?.Trim();
            var fecha = ParseFecha(fechaTexto);

            // Leer las medidas (las siguientes filas después de la fecha)
            var pecho = ParseDecimal(worksheet.Cells[filaInicio + 2, colIndex].Text);
            var brazo = ParseDecimal(worksheet.Cells[filaInicio + 3, colIndex].Text);
            var cinturaSobre = ParseDecimal(worksheet.Cells[filaInicio + 4, colIndex].Text);
            var cinturaOmbligo = ParseDecimal(worksheet.Cells[filaInicio + 5, colIndex].Text);
            var cinturaBajo = ParseDecimal(worksheet.Cells[filaInicio + 6, colIndex].Text);
            var cadera = ParseDecimal(worksheet.Cells[filaInicio + 7, colIndex].Text);
            var muslos = ParseDecimal(worksheet.Cells[filaInicio + 8, colIndex].Text);

            // Solo agregar si hay al menos una medida
            if (pecho.HasValue || brazo.HasValue || cinturaOmbligo.HasValue || 
                cinturaSobre.HasValue || cinturaBajo.HasValue || cadera.HasValue || muslos.HasValue)
            {
                var medida = new MedidaCorporal
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
                };

                _context.Set<MedidaCorporal>().Add(medida);
                medidasAgregadas++;
            }
        }

        // Guardar las medidas si se agregaron
        if (medidasAgregadas > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task ImportarSeguimientoDiario(ExcelWorksheet worksheet, int clienteId)
    {
        // Eliminar seguimientos existentes
        var existentes = await _context.SeguimientoDiario
            .Where(s => s.ClienteId == clienteId)
            .ToListAsync();

        if (existentes.Any())
        {
            _context.SeguimientoDiario.RemoveRange(existentes);
            await _context.SaveChangesAsync(); // Guardar la eliminación primero
        }

        // Buscar la fila donde están los encabezados del seguimiento
        int filaEncabezados = -1;
        for (int fila = 35; fila < 50; fila++)
        {
            var celda = worksheet.Cells[fila, 1].Text?.Trim().ToUpper();
            if (celda == "SEMANA" || (celda != null && celda.Contains("SEMANA")))
            {
                filaEncabezados = fila;
                break;
            }
        }

        if (filaEncabezados == -1)
            filaEncabezados = 39; // Valor por defecto

        // La fila de datos empieza después de 3 filas: encabezados, línea vacía, línea vacía, EJEMPLO
        int filaInicio = filaEncabezados + 4;
        int maxFilas = 200;
        int registrosAgregados = 0;

        // Diccionario para evitar fechas duplicadas
        var fechasAgregadas = new HashSet<DateTime>();

        for (int fila = filaInicio; fila < filaInicio + maxFilas; fila++)
        {
            // Columna A (1) = SEMANA
            var semanaTexto = worksheet.Cells[fila, 1].Text?.Trim();
            
            // Columna B (2) = FECHA
            var fechaTexto = worksheet.Cells[fila, 2].Text?.Trim();

            // Saltar filas vacías, de ejemplo o de resumen
            if (string.IsNullOrWhiteSpace(fechaTexto))
                continue;
                
            if (semanaTexto != null && (
                semanaTexto.ToUpper() == "EJEMPLO" ||
                semanaTexto.ToUpper().Contains("RESUMEN") ||
                semanaTexto.ToUpper() == "SEMANA Nº"))
                continue;

            var fecha = ParseFecha(fechaTexto);
            if (!fecha.HasValue)
                continue;

            // Saltar si ya tenemos un registro para esta fecha
            if (fechasAgregadas.Contains(fecha.Value.Date))
                continue;

            // Leer datos de las columnas correspondientes
            var pesoTexto = worksheet.Cells[fila, 3].Text?.Trim();
            var horaTexto = worksheet.Cells[fila, 4].Text?.Trim();
            var proteinaTexto = worksheet.Cells[fila, 5].Text?.Trim();
            var grasaTexto = worksheet.Cells[fila, 6].Text?.Trim();
            var carbsTexto = worksheet.Cells[fila, 7].Text?.Trim();
            var caloriasTexto = worksheet.Cells[fila, 8].Text?.Trim();
            var entrenoTexto = worksheet.Cells[fila, 9].Text?.Trim();
            var rendimientoTexto = worksheet.Cells[fila, 10].Text?.Trim();
            var pasosTexto = worksheet.Cells[fila, 11].Text?.Trim();
            var cardioTexto = worksheet.Cells[fila, 12].Text?.Trim();
            var suenoTexto = worksheet.Cells[fila, 13].Text?.Trim();
            var calidadSuenoTexto = worksheet.Cells[fila, 14].Text?.Trim();
            var apetitoTexto = worksheet.Cells[fila, 15].Text?.Trim();
            var estresTexto = worksheet.Cells[fila, 16].Text?.Trim();
            var notasTexto = worksheet.Cells[fila, 17].Text?.Trim();

            var seguimiento = new SeguimientoDiario
            {
                ClienteId = clienteId,
                NumeroSemana = ParseInt(semanaTexto),
                Fecha = fecha.Value,
                Peso = ParseDecimal(pesoTexto),
                HoraPesaje = ParseHora(horaTexto),
                Proteina = ParseDecimal(proteinaTexto),
                Grasa = ParseDecimal(grasaTexto),
                Carbohidratos = ParseDecimal(carbsTexto),
                TotalCalorias = ParseInt(caloriasTexto),
                DiaEntreno = TruncateString(entrenoTexto, 100),
                RendimientoSesion = TruncateString(rendimientoTexto, 100),
                PasosRealizados = ParseInt(pasosTexto),
                DuracionCardio = TruncateString(cardioTexto, 50),
                HorasSueno = ParseDecimal(suenoTexto),
                CalidadSueno = TruncateString(calidadSuenoTexto, 50),
                Apetito = TruncateString(apetitoTexto, 50),
                NivelEstres = TruncateString(estresTexto, 50),
                Notas = TruncateString(notasTexto, 500),
                EntrenoRealizado = !string.IsNullOrWhiteSpace(entrenoTexto),
                CardioRealizado = !string.IsNullOrWhiteSpace(cardioTexto)
            };

            _context.SeguimientoDiario.Add(seguimiento);
            fechasAgregadas.Add(fecha.Value.Date);
            registrosAgregados++;
        }

        // Guardar todos los nuevos registros
        if (registrosAgregados > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<byte[]> ExportarExcel(int clienteId)
    {
        var cliente = await _context.ClientesPerfil
            .Include(c => c.Macros)
            .Include(c => c.SeguimientoDiario)
            .FirstOrDefaultAsync(c => c.Id == clienteId && !c.Eliminado);

        if (cliente == null)
            throw new Exception("Cliente no encontrado");

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Seguimiento");

        // Configurar cabecera con información del cliente
        worksheet.Cells["F4"].Value = "NOMBRE Y EDAD";
        worksheet.Cells["G4"].Value = cliente.NombreCompleto;
        
        worksheet.Cells["F6"].Value = "OBJETIVOS";
        worksheet.Cells["G6"].Value = cliente.Objetivos;

        worksheet.Cells["F8"].Value = "PESO INICIAL";
        worksheet.Cells["G8"].Value = cliente.PesoInicial?.ToString() + "kg";

        worksheet.Cells["F10"].Value = "FECHA DE INICIO";
        worksheet.Cells["G10"].Value = cliente.FechaInicioPrograma?.ToString("dd MMMM yyyy", new CultureInfo("es-ES"));

        worksheet.Cells["F14"].Value = "PASOS";
        worksheet.Cells["G14"].Value = cliente.ObjetivoPasos;

        worksheet.Cells["F16"].Value = "CARDIO";
        worksheet.Cells["G16"].Value = cliente.ObjetivoCardio;

        worksheet.Cells["F18"].Value = "SUPLEMENTACIÓN";
        worksheet.Cells["G18"].Value = cliente.Suplementacion;

        // Macros día de entreno
        var macrosEntreno = cliente.Macros.FirstOrDefault(m => m.TipoDia == "ENTRENO");
        worksheet.Cells["J2"].Value = "MACROS DÍA ENTRENO";
        worksheet.Cells["J4"].Value = "PROTEÍNA";
        worksheet.Cells["K4"].Value = macrosEntreno?.Proteina;
        worksheet.Cells["J6"].Value = "GRASA";
        worksheet.Cells["K6"].Value = macrosEntreno?.Grasa;
        worksheet.Cells["J8"].Value = "CARBOHIDRATOS";
        worksheet.Cells["K8"].Value = macrosEntreno?.Carbohidratos;
        worksheet.Cells["J10"].Value = "CALORÍAS";
        worksheet.Cells["K10"].Value = macrosEntreno?.Calorias;

        // Macros día de descanso
        var macrosDescanso = cliente.Macros.FirstOrDefault(m => m.TipoDia == "DESCANSO");
        worksheet.Cells["J12"].Value = "MACROS DÍA DESCANSO";
        worksheet.Cells["J14"].Value = "PROTEÍNA";
        worksheet.Cells["K14"].Value = macrosDescanso?.Proteina;
        worksheet.Cells["J16"].Value = "GRASA";
        worksheet.Cells["K16"].Value = macrosDescanso?.Grasa;
        worksheet.Cells["J18"].Value = "CARBOHIDRATOS";
        worksheet.Cells["K18"].Value = macrosDescanso?.Carbohidratos;
        worksheet.Cells["J20"].Value = "CALORÍAS";
        worksheet.Cells["K20"].Value = macrosDescanso?.Calorias;

        // Medidas corporales
        var medidas = await _context.Set<MedidaCorporal>()
            .Where(m => m.ClienteId == clienteId)
            .OrderBy(m => m.NumeroSemana)
            .ToListAsync();

        int filaMedidas = 24;
        worksheet.Cells[filaMedidas, 4].Value = "MEDIDAS";
        worksheet.Cells[filaMedidas + 2, 3].Value = "PECHO";
        worksheet.Cells[filaMedidas + 3, 3].Value = "BRAZO";
        worksheet.Cells[filaMedidas + 4, 3].Value = "3cm SOBRE OMBLIGO";
        worksheet.Cells[filaMedidas + 5, 3].Value = "OMBLIGO";
        worksheet.Cells[filaMedidas + 6, 3].Value = "3cm BAJO OMBLIGO";
        worksheet.Cells[filaMedidas + 7, 3].Value = "CADERA";
        worksheet.Cells[filaMedidas + 8, 3].Value = "MUSLOS";

        int colMedida = 6;
        foreach (var medida in medidas)
        {
            worksheet.Cells[filaMedidas + 1, colMedida].Value = medida.Fecha?.ToString("dd/MM/yyyy");
            worksheet.Cells[filaMedidas + 2, colMedida].Value = medida.Pecho?.ToString() + "cm";
            worksheet.Cells[filaMedidas + 3, colMedida].Value = medida.Brazo?.ToString() + "cm";
            worksheet.Cells[filaMedidas + 4, colMedida].Value = medida.CinturaSobreOmbligo?.ToString() + "cm";
            worksheet.Cells[filaMedidas + 5, colMedida].Value = medida.CinturaOmbligo?.ToString() + "cm";
            worksheet.Cells[filaMedidas + 6, colMedida].Value = medida.CinturaBajoOmbligo?.ToString() + "cm";
            worksheet.Cells[filaMedidas + 7, colMedida].Value = medida.Cadera?.ToString() + "cm";
            worksheet.Cells[filaMedidas + 8, colMedida].Value = medida.Muslos?.ToString() + "cm";
            colMedida++;
        }

        // Seguimiento diario
        int filaSeguimiento = 39;
        var encabezados = new[] { "SEMANA", "FECHA", "PESO EN AYUNAS", "HORA DEL PESAJE", "PROTEINA", "GRASA", 
            "CARBS", "TOTAL CALORÍAS", "ENTRENO", "RENDIMIENTO DE LA SESIÓN", "PASOS", "CARDIO", 
            "DURACIÓN SUEÑO EN HORAS", "CALIDAD SUEÑO", "APETITO", "NIVEL DE ESTRÉS", "NOTAS EXTRA" };

        for (int i = 0; i < encabezados.Length; i++)
        {
            worksheet.Cells[filaSeguimiento, i + 1].Value = encabezados[i];
        }

        var seguimientos = cliente.SeguimientoDiario.OrderBy(s => s.Fecha).ToList();
        int filaActual = filaSeguimiento + 1;

        foreach (var seg in seguimientos)
        {
            worksheet.Cells[filaActual, 1].Value = seg.NumeroSemana;
            worksheet.Cells[filaActual, 2].Value = seg.Fecha.ToString("dd-MMM", new CultureInfo("es-ES"));
            worksheet.Cells[filaActual, 3].Value = seg.Peso;
            worksheet.Cells[filaActual, 4].Value = seg.HoraPesaje?.ToString(@"hh\:mm");
            worksheet.Cells[filaActual, 5].Value = seg.Proteina;
            worksheet.Cells[filaActual, 6].Value = seg.Grasa;
            worksheet.Cells[filaActual, 7].Value = seg.Carbohidratos;
            worksheet.Cells[filaActual, 8].Value = seg.TotalCalorias;
            worksheet.Cells[filaActual, 9].Value = seg.DiaEntreno;
            worksheet.Cells[filaActual, 10].Value = seg.RendimientoSesion;
            worksheet.Cells[filaActual, 11].Value = seg.PasosRealizados;
            worksheet.Cells[filaActual, 12].Value = seg.DuracionCardio;
            worksheet.Cells[filaActual, 13].Value = seg.HorasSueno;
            worksheet.Cells[filaActual, 14].Value = seg.CalidadSueno;
            worksheet.Cells[filaActual, 15].Value = seg.Apetito;
            worksheet.Cells[filaActual, 16].Value = seg.NivelEstres;
            worksheet.Cells[filaActual, 17].Value = seg.Notas;
            filaActual++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }

    private decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Eliminar texto adicional (kg, cm, etc.)
        value = value.Replace("kg", "").Replace("cm", "").Replace(" ", "").Trim();

        // Si el valor contiene coma, podría ser separador decimal (español)
        // Si contiene punto y coma, la coma es separador de miles
        if (value.Contains(","))
        {
            // Contar puntos y comas
            int puntosCount = value.Count(c => c == '.');
            int comasCount = value.Count(c => c == ',');

            if (puntosCount > 0 && comasCount > 0)
            {
                // Ambos presentes: formato europeo (1.234,56)
                value = value.Replace(".", "").Replace(",", ".");
            }
            else if (comasCount == 1 && puntosCount == 0)
            {
                // Solo una coma: separador decimal español (70,2)
                value = value.Replace(",", ".");
            }
            else if (comasCount > 1)
            {
                // Múltiples comas: separador de miles (1,234,567.89)
                value = value.Replace(",", "");
            }
        }
        else if (value.Contains("."))
        {
            // Solo puntos
            var parts = value.Split('.');
            if (parts.Length > 2)
            {
                // Múltiples puntos: separadores de miles (1.234.567)
                value = string.Join("", parts.Take(parts.Length - 1)) + "." + parts.Last();
            }
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    private int? ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Eliminar espacios y caracteres no numéricos excepto signos
        value = value.Replace(".", "").Replace(",", "").Replace(" ", "").Trim();

        // Eliminar caracteres no numéricos excepto el signo negativo
        value = new string(value.Where(c => char.IsDigit(c) || c == '-').ToArray());

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    private DateTime? ParseFecha(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Intentar parsear como número de Excel primero
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double oaDate))
        {
            try
            {
                return DateTime.FromOADate(oaDate);
            }
            catch
            {
                // No era una fecha de Excel
            }
        }

        var formatos = new[] { 
            "d/M/yyyy", "dd/MM/yyyy", "d-MMM-yyyy", "dd-MMM-yyyy",
            "d MMMM yyyy", "dd MMMM yyyy", "M/d/yyyy",
            "d MMM", "dd MMM", "d-MMM", "dd-MMM", 
            "d MMM yyyy", "dd MMM yyyy",
            "d-MMM-yyyy", "dd-MMM-yyyy",
            "d ene", "d feb", "d mar", "d abr", "d may", "d jun",
            "d jul", "d ago", "d sep", "d oct", "d nov", "d dic"
        };

        var culturas = new[] { 
            new CultureInfo("es-ES"), 
            new CultureInfo("en-US"),
            CultureInfo.InvariantCulture 
        };

        foreach (var cultura in culturas)
        {
            foreach (var formato in formatos)
            {
                if (DateTime.TryParseExact(value, formato, cultura, DateTimeStyles.None, out var result))
                {
                    // Si no tiene año, usar el año actual
                    if (result.Year == 1 || result.Year < 2000)
                        result = new DateTime(DateTime.Now.Year, result.Month, result.Day);

                    return result;
                }
            }
        }

        // Intentar parse general con cultura española
        if (DateTime.TryParse(value, new CultureInfo("es-ES"), DateTimeStyles.None, out var fecha))
        {
            if (fecha.Year < 2000)
                fecha = new DateTime(DateTime.Now.Year, fecha.Month, fecha.Day);
            return fecha;
        }

        // Intentar parse general con cultura inglesa
        if (DateTime.TryParse(value, new CultureInfo("en-US"), DateTimeStyles.None, out fecha))
        {
            if (fecha.Year < 2000)
                fecha = new DateTime(DateTime.Now.Year, fecha.Month, fecha.Day);
            return fecha;
        }

        return null;
    }

    private TimeSpan? ParseHora(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Intentar parsear directamente como TimeSpan
        if (TimeSpan.TryParse(value, out var result))
            return result;

        // Intentar parsear como HH:mm
        var partes = value.Split(':');
        if (partes.Length == 2)
        {
            if (int.TryParse(partes[0], out int horas) && int.TryParse(partes[1], out int minutos))
            {
                if (horas >= 0 && horas < 24 && minutos >= 0 && minutos < 60)
                {
                    return new TimeSpan(horas, minutos, 0);
                }
            }
        }

        // Intentar parsear como número decimal (Excel puede guardar horas así)
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double horaDecimal))
        {
            try
            {
                // Excel guarda horas como fracciones de día
                if (horaDecimal < 1.0)
                {
                    var totalMinutos = (int)(horaDecimal * 24 * 60);
                    return TimeSpan.FromMinutes(totalMinutos);
                }
            }
            catch
            {
                // No era una hora válida
            }
        }

        return null;
    }

    private string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    private (int row, int col)? BuscarCelda(ExcelWorksheet worksheet, string textoBuscar, int maxRows = 30, int maxCols = 20)
    {
        for (int row = 1; row <= maxRows; row++)
        {
            for (int col = 1; col <= maxCols; col++)
            {
                var cellText = worksheet.Cells[row, col].Text?.Trim();
                if (!string.IsNullOrEmpty(cellText) && 
                    cellText.Contains(textoBuscar, StringComparison.OrdinalIgnoreCase))
                {
                    return (row, col);
                }
            }
        }
        return null;
    }

    private string? ObtenerValorDerecha(ExcelWorksheet worksheet, string etiqueta, int offsetCol = 1)
    {
        var celda = BuscarCelda(worksheet, etiqueta);
        if (celda.HasValue)
        {
            return worksheet.Cells[celda.Value.row, celda.Value.col + offsetCol].Text;
        }
        return null;
    }

    private (decimal? Proteina, decimal? Grasa, decimal? Carbohidratos, int? Calorias) BuscarMacros(
        ExcelWorksheet worksheet, params string[] titulosBuscar)
    {
        // Buscar la sección de macros por título
        (int row, int col)? titulo = null;
        foreach (var buscar in titulosBuscar)
        {
            titulo = BuscarCelda(worksheet, buscar);
            if (titulo.HasValue) break;
        }

        if (!titulo.HasValue)
            return (null, null, null, null);

        // Los macros están típicamente en la misma columna, filas debajo del título
        int colMacros = titulo.Value.col + 1; // Columna de valores (derecha del título)
        int filaBase = titulo.Value.row;

        decimal? proteina = null;
        decimal? grasa = null;
        decimal? carbohidratos = null;
        int? calorias = null;

        // Buscar en las siguientes 10 filas
        for (int offset = 1; offset <= 10; offset++)
        {
            var etiqueta = worksheet.Cells[filaBase + offset, titulo.Value.col].Text?.Trim().ToUpper();
            var valor = worksheet.Cells[filaBase + offset, colMacros].Text;

            if (etiqueta != null)
            {
                if (etiqueta.Contains("PROTE") || etiqueta.Contains("PROTEIN"))
                    proteina = ParseDecimal(valor);
                else if (etiqueta.Contains("GRASA") || etiqueta.Contains("FAT"))
                    grasa = ParseDecimal(valor);
                else if (etiqueta.Contains("CARB"))
                    carbohidratos = ParseDecimal(valor);
                else if (etiqueta.Contains("CALOR") || etiqueta.Contains("KCAL"))
                    calorias = ParseInt(valor);
            }
        }

        return (proteina, grasa, carbohidratos, calorias);
    }
}
