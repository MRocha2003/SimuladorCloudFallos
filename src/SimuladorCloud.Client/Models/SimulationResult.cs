namespace SimuladorCloud.Client.Models;

/// <summary>
/// Representa los resultados generados tras ejecutar una simulación
/// de cascada o análisis de SPOFs sobre el grafo G=(V,E).
/// </summary>
public class SimulationResult
{
    /// <summary>Identificador único del resultado de simulación.</summary>
    public string ResultId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Tipo de simulación ejecutada.</summary>
    public SimulationType Type { get; set; } = SimulationType.CascadeFailure;

    /// <summary>ID del nodo donde se inyectó el fallo inicial.</summary>
    public string FailureOrigin { get; set; } = string.Empty;

    /// <summary>Lista de IDs de nodos afectados por la propagación en cascada.</summary>
    public List<string> AffectedNodes { get; set; } = new();

    /// <summary>Número total de nodos afectados.</summary>
    public int AffectedCount => AffectedNodes.Count;

    /// <summary>
    /// Porcentaje del sistema afectado respecto al total de nodos del grafo.
    /// </summary>
    public double AffectedPercent { get; set; } = 0.0;

    /// <summary>Lista de IDs de nodos identificados como SPOFs.</summary>
    public List<string> SpofList { get; set; } = new();

    /// <summary>
    /// Lista de rutas críticas identificadas.
    /// Cada ruta es una secuencia de IDs de nodos.
    /// </summary>
    public List<List<string>> CriticalPaths { get; set; } = new();

    /// <summary>Tiempo de ejecución del algoritmo en milisegundos (en WASM).</summary>
    public double ExecutionTimeMs { get; set; } = 0.0;

    /// <summary>Fecha y hora en que se ejecutó la simulación.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ── Propiedades de conveniencia ────────────────────────────────────────────

    /// <summary>Umbral de impacto crítico: porcentaje de nodos afectados.</summary>
    public const double CriticalImpactThreshold = 50.0;

    /// <summary>Indica si la cascada afecta a más del 50% del sistema (HU-12).</summary>
    public bool IsCriticalImpact => AffectedPercent > CriticalImpactThreshold;

    /// <summary>Indica si se detectaron SPOFs en el análisis.</summary>
    public bool HasSpofs => SpofList.Count > 0;

    /// <summary>
    /// Genera un resumen textual del resultado para el reporte de impacto.
    /// </summary>
    public string GenerateSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== REPORTE DE SIMULACIÓN ===");
        sb.AppendLine($"Tipo: {Type}");
        sb.AppendLine($"Fecha: {Timestamp:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Tiempo de ejecución: {ExecutionTimeMs:F2} ms");
        sb.AppendLine();
        sb.AppendLine($"FALLO INICIAL: nodo ID {FailureOrigin}");
        sb.AppendLine($"NODOS AFECTADOS: {AffectedCount} ({AffectedPercent:F1}% del sistema)");
        sb.AppendLine();
        sb.AppendLine($"SPOFs DETECTADOS: {SpofList.Count}");
        foreach (var spof in SpofList)
            sb.AppendLine($"  - Nodo ID: {spof}");
        sb.AppendLine();
        sb.AppendLine($"RUTAS CRÍTICAS: {CriticalPaths.Count}");
        return sb.ToString();
    }
}
