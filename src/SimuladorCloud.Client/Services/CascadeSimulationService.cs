using SimuladorCloud.Client.Models;
using System.Diagnostics;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Servicio de simulación de fallos en cascada mediante algoritmo BFS/DFS.
/// Implementa RF-04, RF-05 y RF-12.
/// Opera íntegramente en WebAssembly — sin conexión a nubes reales.
/// Complejidad temporal: O(V+E).
/// HU-05: Inyección controlada de fallos.
/// HU-06: Propagación BFS/DFS de fallos en cascada.
/// </summary>
public class CascadeSimulationService
{
    private readonly GraphService _graphService;

    public CascadeSimulationService(GraphService graphService)
    {
        _graphService = graphService;
    }

    /// <summary>
    /// Ejecuta la simulación de fallo en cascada a partir del nodo inicial.
    /// </summary>
    /// <param name="originVertexId">ID del nodo donde se inyecta el fallo.</param>
    /// <returns>Resultado de la simulación con nodos afectados y métricas.</returns>
    public SimulationResult SimulateCascade(string originVertexId)
    {
        var graph  = _graphService.Graph;
        var origin = graph.GetVertex(originVertexId);

        if (origin is null)
            return new SimulationResult { FailureOrigin = originVertexId };

        var stopwatch = Stopwatch.StartNew();

        // Reiniciar estado de simulación anterior
        graph.ResetSimulation();

        // Marcar nodo inicial como fallido (HU-05)
        origin.Status = NodeStatus.Failed;
        graph.FailedNodes.Add(origin.Id);

        // BFS para propagar el fallo en cascada (HU-06)
        var affected = new HashSet<string>();
        var queue    = new Queue<string>();
        queue.Enqueue(originVertexId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            // Propagar hacia todos los nodos que dependen del nodo actual.
            // Una arista e(u,v) significa que u depende de v: si v falla,
            // el fallo se propaga a u (origen de las aristas entrantes de v).
            foreach (var edge in graph.GetIncomingEdges(currentId).ToList())
            {
                var dependent = graph.GetVertex(edge.SourceId);
                if (dependent is null || dependent.Status == NodeStatus.Failed)
                    continue;

                // Marcar arista como parte de la cascada
                edge.IsCascadePath = true;

                // Marcar nodo dependiente como fallido
                dependent.Status = NodeStatus.Failed;
                affected.Add(dependent.Id);
                graph.FailedNodes.Add(dependent.Id);

                queue.Enqueue(dependent.Id);
            }
        }

        stopwatch.Stop();

        int    totalNodes      = graph.NodeCount;
        int    affectedCount   = affected.Count;
        double affectedPercent = totalNodes > 0
            ? (affectedCount / (double)totalNodes) * 100.0
            : 0.0;

        var result = new SimulationResult
        {
            Type            = SimulationType.CascadeFailure,
            FailureOrigin   = originVertexId,
            AffectedNodes   = affected.ToList(),
            AffectedPercent = affectedPercent,
            ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
        };

        return result;
    }

    /// <summary>
    /// Calcula los nodos que fallarían en cascada sin modificar el estado
    /// del grafo (útil para estimar el impacto de un nodo).
    /// </summary>
    public HashSet<string> ComputeAffected(string originVertexId) =>
        _graphService.Graph.GetTransitiveDependents(originVertexId);

    /// <summary>
    /// Inyecta un fallo en un nodo sin propagar la cascada.
    /// Útil para marcar múltiples nodos fallidos antes de iniciar la simulación.
    /// HU-05 — Criterio de aceptación 2.
    /// </summary>
    public void InjectFault(string vertexId)
    {
        var vertex = _graphService.Graph.GetVertex(vertexId);
        if (vertex is null) return;
        vertex.Status = NodeStatus.Failed;
        _graphService.Graph.FailedNodes.Add(vertexId);
    }
}
