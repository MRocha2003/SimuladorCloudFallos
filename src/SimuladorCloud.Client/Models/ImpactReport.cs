using System.Globalization;
using System.Text;

namespace SimuladorCloud.Client.Models;

/// <summary>
/// Reporte estructurado de impacto generado tras cada simulación o análisis:
/// nodo inicial, nodos afectados, SPOFs y rutas críticas, con nombres legibles.
/// IT-5 · HU-09.
/// </summary>
public class ImpactReport
{
    /// <summary>Nombre de la topología analizada.</summary>
    public string TopologyName { get; set; } = string.Empty;

    /// <summary>Tipo de simulación que originó el reporte.</summary>
    public SimulationType Type { get; set; }

    /// <summary>Fecha y hora de la simulación.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Número total de nodos de la topología.</summary>
    public int TotalNodes { get; set; }

    /// <summary>Número total de aristas de la topología.</summary>
    public int TotalEdges { get; set; }

    /// <summary>Nombre del nodo donde se inyectó el fallo (vacío en un análisis de SPOF).</summary>
    public string OriginName { get; set; } = string.Empty;

    /// <summary>Nombres de los nodos afectados por la cascada.</summary>
    public List<string> AffectedNames { get; set; } = new();

    /// <summary>Número de nodos afectados.</summary>
    public int AffectedCount => AffectedNames.Count;

    /// <summary>Porcentaje del sistema afectado.</summary>
    public double AffectedPercent { get; set; }

    /// <summary>Indica si la cascada supera el umbral de impacto crítico.</summary>
    public bool IsCriticalImpact { get; set; }

    /// <summary>SPOF detectados, ordenados por criticidad.</summary>
    public List<ReportSpof> Spofs { get; set; } = new();

    /// <summary>Rutas críticas identificadas.</summary>
    public List<ReportPath> CriticalPaths { get; set; } = new();

    /// <summary>Tiempo total de cálculo, en milisegundos.</summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Versión en texto plano del reporte, para copiarla al portapapeles.
    /// HU-09 — Criterio de aceptación 4.
    /// </summary>
    public string ToText()
    {
        var inv = CultureInfo.InvariantCulture;
        var sb  = new StringBuilder();
        sb.AppendLine("=== REPORTE DE IMPACTO — SIMULADOR DE FALLOS EN CASCADA ===");
        sb.AppendLine($"Topología: {TopologyName} ({TotalNodes} nodos, {TotalEdges} dependencias)");
        sb.AppendLine($"Fecha: {Timestamp.ToLocalTime():dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"Tiempo de cálculo: {ExecutionTimeMs.ToString("F2", inv)} ms");
        sb.AppendLine();

        if (Type == SimulationType.CascadeFailure)
        {
            sb.AppendLine($"NODO INICIAL DEL FALLO: {OriginName}");
            sb.AppendLine($"NODOS AFECTADOS: {AffectedCount} de {TotalNodes} ({AffectedPercent.ToString("F1", inv)}% del sistema)");
            sb.AppendLine(IsCriticalImpact ? "NIVEL DE IMPACTO: CRÍTICO (más del 50% del sistema)" : "NIVEL DE IMPACTO: no crítico");
            foreach (var name in AffectedNames) sb.AppendLine($"  - {name}");
            sb.AppendLine();
        }

        sb.AppendLine($"SPOFs DETECTADOS: {Spofs.Count}");
        foreach (var s in Spofs)
        {
            sb.Append($"  - {s.Name} ({s.Type}): desconectaría {s.DisconnectedNames.Count} nodo(s)");
            if (s.DisconnectedNames.Count > 0) sb.Append($" [{string.Join(", ", s.DisconnectedNames)}]");
            sb.AppendLine($"; su fallo afectaría en cascada a {s.CascadeCount} nodo(s)");
        }
        sb.AppendLine();

        sb.AppendLine($"RUTAS CRÍTICAS: {CriticalPaths.Count}");
        for (int i = 0; i < CriticalPaths.Count; i++)
        {
            var p = CriticalPaths[i];
            sb.AppendLine($"  {i + 1}. {string.Join(" → ", p.NodeNames)} (longitud {p.Length}, {p.SpofCount} SPOF)");
        }
        return sb.ToString();
    }
}

/// <summary>SPOF tal como aparece en el reporte.</summary>
public class ReportSpof
{
    /// <summary>Nombre del SPOF.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tipo de componente del SPOF.</summary>
    public NodeType Type { get; set; }

    /// <summary>Nombres de los nodos que quedarían desconectados.</summary>
    public List<string> DisconnectedNames { get; set; } = new();

    /// <summary>Número de nodos que fallarían en cascada.</summary>
    public int CascadeCount { get; set; }
}

/// <summary>Ruta crítica tal como aparece en el reporte.</summary>
public class ReportPath
{
    /// <summary>Nombres de los nodos de la ruta, en orden.</summary>
    public List<string> NodeNames { get; set; } = new();

    /// <summary>Longitud de la ruta en número de aristas.</summary>
    public int Length => Math.Max(0, NodeNames.Count - 1);

    /// <summary>Número de SPOF que atraviesa la ruta.</summary>
    public int SpofCount { get; set; }
}
