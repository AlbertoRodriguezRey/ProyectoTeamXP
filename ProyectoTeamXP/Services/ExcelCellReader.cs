using OfficeOpenXml;
using ProyectoTeamXP.Models;

namespace ProyectoTeamXP.Services;

/// <summary>
/// Lector universal de Excel que extrae TODAS las celdas de TODAS las pestañas,
/// resolviendo celdas combinadas (merged cells) para que ningún dato se pierda.
/// 
/// Enfoque: leer todo primero → buscar después (en vez de buscar posiciones fijas).
/// </summary>
public static class ExcelCellReader
{
    /// <summary>
    /// Extrae todas las celdas de un archivo Excel agrupadas por pestaña.
    /// Resuelve celdas combinadas: si una celda vacía pertenece a un rango combinado,
    /// recupera el valor de la celda maestra (superior-izquierda del rango).
    /// </summary>
    public static Dictionary<string, List<CellData>> LeerTodasLasCeldas(Stream excelStream)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(excelStream);
        var resultado = new Dictionary<string, List<CellData>>();

        foreach (var ws in package.Workbook.Worksheets)
        {
            if (ws.Dimension == null) continue;

            // Pre-calcular mapa de merges para esta hoja (evita iterar N² veces)
            var mergeMap = BuildMergeMap(ws);
            var celdas = new List<CellData>();

            for (int row = ws.Dimension.Start.Row; row <= ws.Dimension.End.Row; row++)
            {
                for (int col = ws.Dimension.Start.Column; col <= ws.Dimension.End.Column; col++)
                {
                    var cell = ws.Cells[row, col];
                    var text = cell.Text?.Trim();
                    var value = cell.Value;
                    var formula = cell.Formula;
                    bool isMerged = cell.Merge;

                    // ── Resolución de celdas combinadas ──
                    // Si la celda está vacía pero es parte de un merge, recuperar de la celda maestra
                    if ((string.IsNullOrEmpty(text) && value == null) && isMerged)
                    {
                        if (mergeMap.TryGetValue((row, col), out var master))
                        {
                            var masterCell = ws.Cells[master.Row, master.Col];
                            text = masterCell.Text?.Trim();
                            value = masterCell.Value;
                            formula = masterCell.Formula;
                        }
                    }

                    // Solo guardar celdas con contenido (evitar miles de celdas vacías)
                    if (!string.IsNullOrEmpty(text) || value != null || !string.IsNullOrEmpty(formula))
                    {
                        celdas.Add(new CellData
                        {
                            SheetName = ws.Name,
                            Address = cell.Address,
                            Row = row,
                            Col = col,
                            Value = value,
                            Formula = formula,
                            RawText = text,
                            IsMerged = isMerged
                        });
                    }
                }
            }

            resultado[ws.Name] = celdas;
        }

        return resultado;
    }

    // ═══════════════════════════════════════════════════
    //  BÚSQUEDAS EN EL MAPA DE CELDAS
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Busca la primera celda cuyo texto contenga el texto buscado (case-insensitive).
    /// </summary>
    public static CellData? BuscarPorTexto(List<CellData> celdas, string texto)
        => celdas.FirstOrDefault(c =>
            c.RawText != null && c.RawText.Contains(texto, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Busca todas las celdas cuyo texto contenga el texto buscado.
    /// </summary>
    public static List<CellData> BuscarTodosPorTexto(List<CellData> celdas, string texto)
        => celdas.Where(c =>
            c.RawText != null && c.RawText.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Obtiene el valor de la celda inmediatamente a la derecha de una etiqueta.
    /// Resuelve el patrón típico "PESO INICIAL | 71kg" donde la etiqueta está en una celda
    /// y el valor en la siguiente.
    /// </summary>
    public static string? ObtenerValorJuntoA(List<CellData> celdas, string etiqueta)
    {
        var celda = BuscarPorTexto(celdas, etiqueta);
        if (celda == null) return null;

        // Buscar la celda en la misma fila, columnas siguientes (+1, +2, +3)
        for (int offset = 1; offset <= 3; offset++)
        {
            var vecina = celdas.FirstOrDefault(c => c.Row == celda.Row && c.Col == celda.Col + offset);
            if (vecina != null && !string.IsNullOrWhiteSpace(vecina.RawText))
                return vecina.RawText;
        }

        // También intentar la fila de abajo (para valores que van debajo de la etiqueta)
        for (int rowOffset = 1; rowOffset <= 2; rowOffset++)
        {
            var abajo = celdas.FirstOrDefault(c => c.Row == celda.Row + rowOffset && c.Col == celda.Col);
            if (abajo != null && !string.IsNullOrWhiteSpace(abajo.RawText))
                return abajo.RawText;
        }

        return null;
    }

    /// <summary>
    /// Obtiene el valor de una celda por coordenada (row, col).
    /// </summary>
    public static string? ObtenerValor(List<CellData> celdas, int row, int col)
        => celdas.FirstOrDefault(c => c.Row == row && c.Col == col)?.RawText;

    // ═══════════════════════════════════════════════════
    //  INTERNALS
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Construye un mapa rápido: (row, col) → celda maestra del merge.
    /// Así la resolución de merges es O(1) en vez de O(N*M).
    /// </summary>
    private static Dictionary<(int Row, int Col), (int Row, int Col)> BuildMergeMap(ExcelWorksheet ws)
    {
        var map = new Dictionary<(int, int), (int, int)>();

        foreach (var mergedRange in ws.MergedCells)
        {
            if (mergedRange == null) continue;
            var range = ws.Cells[mergedRange];
            var masterRow = range.Start.Row;
            var masterCol = range.Start.Column;

            for (int r = range.Start.Row; r <= range.End.Row; r++)
            {
                for (int c = range.Start.Column; c <= range.End.Column; c++)
                {
                    if (r != masterRow || c != masterCol)
                        map[(r, c)] = (masterRow, masterCol);
                }
            }
        }

        return map;
    }
}
