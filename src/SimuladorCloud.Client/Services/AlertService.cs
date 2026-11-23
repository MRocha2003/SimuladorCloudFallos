using System.Globalization;
using SimuladorCloud.Client.Models;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Genera las alertas visuales a partir de los resultados de una simulación
/// o de un análisis de SPOF.
/// IT-6 · HU-12: Alertas visuales.
/// </summary>
public class AlertService
{
    /// <summary>Número máximo de alertas individuales de SPOF antes de agruparlas.</summary>
    public const int MaxSpofAlerts = 3;

    private readonly GraphService _graphService;

    public AlertService(GraphService graphService)
    {
        _graphService = graphService;
    }

    /// <summary>
    /// Construye las alertas: una por SPOF (con los nodos que desconectaría) y
    /// una de impacto crítico si la cascada afecta a más del 50% de los nodos.
    /// HU-12 — Criterios de aceptación 1, 2 y 3.
    /// </summary>
    public List<VisualAlert> BuildAlerts(SimulationResult? result, IReadOnlyList<SpofImpact> spofImpacts)
    {
        var graph  = _graphService.Graph;
        var alerts = new List<VisualAlert>();
        string Name(string id) => graph.GetVertex(id)?.Name ?? id;

        if (result is { Type: SimulationType.CascadeFailure, IsCriticalImpact: true })
        {
            alerts.Add(new VisualAlert
            {
                Level   = AlertLevel.Danger,
                Title   = "Cascada crítica",
                Message = $"El fallo de {Name(result.FailureOrigin)} afecta a {result.AffectedCount} de {graph.NodeCount} nodos " +
                          $"({result.AffectedPercent.ToString("F1", CultureInfo.InvariantCulture)}% del sistema)."
            });
        }

        foreach (var s in spofImpacts.Take(MaxSpofAlerts))
        {
            var disconnected = s.DisconnectedNodes.Select(Name).ToList();
            alerts.Add(new VisualAlert
            {
                Level   = AlertLevel.Warning,
                Title   = $"SPOF detectado: {s.VertexName}",
                Message = disconnected.Count > 0
                    ? $"Su caída desconectaría {disconnected.Count} nodo(s): {string.Join(", ", disconnected)}."
                    : $"Su caída dividiría la arquitectura; {s.CascadeAffected.Count} nodo(s) dependen de él."
            });
        }

        if (spofImpacts.Count > MaxSpofAlerts)
        {
            alerts.Add(new VisualAlert
            {
                Level   = AlertLevel.Warning,
                Title   = $"{spofImpacts.Count - MaxSpofAlerts} SPOF adicionales",
                Message = "Consulte la lista completa en el panel de resultados."
            });
        }
        return alerts;
    }
}
