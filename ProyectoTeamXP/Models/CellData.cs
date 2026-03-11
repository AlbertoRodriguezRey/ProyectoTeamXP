namespace ProyectoTeamXP.Models;

/// <summary>
/// DTO universal: datos extraídos de una celda de Excel con soporte de celdas combinadas.
/// </summary>
public class CellData
{
    public string SheetName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty; // "A1", "G4"
    public int Row { get; set; }
    public int Col { get; set; }
    public object? Value { get; set; }        // Resultado calculado (double, DateTime, string...)
    public string? Formula { get; set; }       // Fórmula sin el '=' (ej: "SUM(A1:B1)")
    public string? RawText { get; set; }       // Lo que se ve en pantalla
    public bool IsMerged { get; set; }
}
