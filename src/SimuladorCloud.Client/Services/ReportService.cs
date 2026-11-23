using SimuladorCloud.Client.Models;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Genera el reporte estructurado de impacto a partir del resultado de una
/// simulación, el impacto de los SPOF y las rutas críticas.
/// IT-5 · HU-09: Generación de reporte de impacto.
/// </summary>
public class ReportService
{
    private readonly GraphService _graphService;

    public ReportService(GraphService graphService)
    {
        _graphService = graphService;
    }

    /// <summary>
    /// Construye el reporte con nombres legibles de los componentes.
    /// HU-09 — Criterios de aceptación 1 y 2.
    /// </summary>
    public ImpactReport BuildReport(SimulationResult result,
                                    IReadOnlyList<SpofImpact> spofImpacts,
                                    IReadOnlyList<CriticalPath> criticalPaths)
    {
        var graph = _graphService.Graph;
        string Name(string id) => graph.GetVertex(id)?.Name ?? id;

        return new ImpactReport
        {
            TopologyName     = graph.Name,
            Type             = result.Type,
            Timestamp        = result.Timestamp,
            TotalNodes       = graph.NodeCount,
            TotalEdges       = graph.EdgeCount,
            OriginName       = string.IsNullOrEmpty(result.FailureOrigin) ? string.Empty : Name(result.FailureOrigin),
            AffectedNames    = result.AffectedNodes.Select(Name).OrderBy(n => n).ToList(),
            AffectedPercent  = result.AffectedPercent,
            IsCriticalImpact = result.IsCriticalImpact,
            ExecutionTimeMs  = result.ExecutionTimeMs,
            Spofs = spofImpacts.Select(s => new ReportSpof
            {
                Name              = s.VertexName,
                Type              = s.NodeType,
                DisconnectedNames = s.DisconnectedNodes.Select(Name).ToList(),
                CascadeCount      = s.CascadeAffected.Count
            }).ToList(),
            CriticalPaths = criticalPaths.Select(p => new ReportPath
            {
                NodeNames = p.NodeIds.Select(Name).ToList(),
                SpofCount = p.SpofCount
            }).ToList()
        };
    }
}
