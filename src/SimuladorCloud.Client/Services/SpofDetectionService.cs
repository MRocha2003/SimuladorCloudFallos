using SimuladorCloud.Client.Models;
using System.Diagnostics;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Servicio de detección de Puntos Únicos de Fallo (SPOFs) mediante el
/// algoritmo de Tarjan para puntos de articulación: un DFS que registra
/// tiempos de descubrimiento y valores low-link sobre el grafo no dirigido
/// subyacente a G=(V,E).
/// Implementa RF-06.
/// Complejidad temporal: O(V+E), gracias a listas de adyacencia construidas
/// una sola vez por análisis.
/// HU-07: Detección automática de SPOFs.
/// HU-08: Información de impacto de cada SPOF.
/// </summary>
public class SpofDetectionService
{
    private readonly GraphService _graphService;

    // Estado del algoritmo DFS
    private int          _timer;
    private int[]        _discovery = Array.Empty<int>();
    private int[]        _low       = Array.Empty<int>();
    private bool[]       _visited   = Array.Empty<bool>();
    private bool[]       _isSpof    = Array.Empty<bool>();
    private List<int>[]  _adjacency = Array.Empty<List<int>>();

    public SpofDetectionService(GraphService graphService)
    {
        _graphService = graphService;
    }

    /// <summary>Tiempo de ejecución del último análisis, en milisegundos.</summary>
    public double LastExecutionTimeMs { get; private set; }

    /// <summary>
    /// Ejecuta el algoritmo de detección de SPOFs sobre el grafo actual.
    /// HU-07 — Criterios de aceptación 1, 2 y 4.
    /// </summary>
    /// <returns>Lista de vértices identificados como SPOFs.</returns>
    public List<Vertex> DetectSpofs()
    {
        var graph    = _graphService.Graph;
        var vertices = graph.Vertices;
        int n        = vertices.Count;

        // Limpiar el resultado de un análisis anterior
        foreach (var vertex in vertices) vertex.IsSpof = false;
        graph.Spofs.Clear();

        if (n == 0) return new List<Vertex>();

        var stopwatch = Stopwatch.StartNew();

        _adjacency = BuildUndirectedAdjacency(graph, excludedId: null, out _);
        _timer     = 0;
        _discovery = new int[n];
        _low       = new int[n];
        _visited   = new bool[n];
        _isSpof    = new bool[n];
        Array.Fill(_discovery, -1);

        // DFS desde cada vértice no visitado (el grafo puede no ser conexo)
        for (int i = 0; i < n; i++)
            if (!_visited[i])
                DfsArticulation(i, -1);

        // Marcar SPOFs en el grafo y recopilar resultado
        var spofs = new List<Vertex>();
        for (int i = 0; i < n; i++)
        {
            if (!_isSpof[i]) continue;
            vertices[i].IsSpof = true;
            spofs.Add(vertices[i]);
            graph.Spofs.Add(vertices[i].Id);
        }

        stopwatch.Stop();
        LastExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return spofs;
    }

    /// <summary>
    /// Calcula el impacto de la caída de un nodo: nodos que quedarían
    /// desconectados del resto de la arquitectura y nodos que fallarían
    /// en cascada. No modifica el estado del grafo.
    /// HU-08 — Criterio de aceptación 3; HU-12 — Criterio de aceptación 2.
    /// </summary>
    public SpofImpact AnalyzeImpact(string vertexId)
    {
        var graph  = _graphService.Graph;
        var vertex = graph.GetVertex(vertexId);
        if (vertex is null) return new SpofImpact { VertexId = vertexId };

        var impact = new SpofImpact
        {
            VertexId        = vertex.Id,
            VertexName      = vertex.Name,
            NodeType        = vertex.NodeType,
            CascadeAffected = graph.GetTransitiveDependents(vertexId)
                                   .OrderBy(id => graph.GetVertex(id)?.Name)
                                   .ToList()
        };

        // Componente del nodo en el grafo completo
        var fullAdjacency = BuildUndirectedAdjacency(graph, excludedId: null, out var index);
        var component     = Reach(fullAdjacency, index[vertexId], blocked: -1);

        // Componentes que quedan dentro de ese mismo componente sin el nodo
        int removed   = index[vertexId];
        var seen      = new bool[graph.Vertices.Count];
        var pieces    = new List<List<int>>();
        foreach (int start in component)
        {
            if (start == removed || seen[start]) continue;
            var piece = Reach(fullAdjacency, start, blocked: removed);
            foreach (int p in piece) seen[p] = true;
            pieces.Add(piece);
        }

        int otherComponents = CountComponents(fullAdjacency) - 1; // sin contar el del nodo
        impact.ComponentsAfterRemoval = otherComponents + pieces.Count;

        // El fragmento más grande se considera el sistema principal;
        // los demás quedan desconectados.
        var main = pieces.OrderByDescending(p => p.Count).FirstOrDefault() ?? new List<int>();
        impact.DisconnectedNodes = pieces.Where(p => !ReferenceEquals(p, main))
                                         .SelectMany(p => p)
                                         .Select(i => graph.Vertices[i])
                                         .OrderBy(v => v.Name)
                                         .Select(v => v.Id)
                                         .ToList();
        return impact;
    }

    /// <summary>
    /// Calcula el impacto de todos los SPOF del último análisis (o ejecuta
    /// el análisis si aún no se hizo) y los ordena de mayor a menor criticidad.
    /// </summary>
    public List<SpofImpact> AnalyzeAllImpacts()
    {
        var graph = _graphService.Graph;
        if (graph.Spofs.Count == 0) DetectSpofs();
        return graph.Spofs
                    .Select(AnalyzeImpact)
                    .OrderByDescending(i => i.CriticalityScore)
                    .ThenBy(i => i.VertexName)
                    .ToList();
    }

    // ════════════════════════════════════════════════════════════════════════
    // ALGORITMO DE TARJAN
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// DFS recursivo para detección de puntos de articulación.
    /// Un vértice u es punto de articulación si:
    /// 1. Es raíz del árbol DFS y tiene 2+ hijos.
    /// 2. No es raíz y tiene un hijo v tal que low[v] >= discovery[u].
    /// </summary>
    private void DfsArticulation(int u, int parent)
    {
        _visited[u]   = true;
        _discovery[u] = _low[u] = _timer++;
        int children  = 0;

        foreach (int v in _adjacency[u])
        {
            if (!_visited[v])
            {
                children++;
                DfsArticulation(v, u);
                _low[u] = Math.Min(_low[u], _low[v]);

                if (parent == -1 && children > 1)
                    _isSpof[u] = true;
                if (parent != -1 && _low[v] >= _discovery[u])
                    _isSpof[u] = true;
            }
            else if (v != parent)
            {
                // Arista de retroceso
                _low[u] = Math.Min(_low[u], _discovery[v]);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // UTILIDADES DE GRAFO NO DIRIGIDO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Construye listas de adyacencia del grafo no dirigido subyacente,
    /// sin vecinos repetidos. Coste O(V+E).
    /// </summary>
    private static List<int>[] BuildUndirectedAdjacency(GraphModel graph, string? excludedId,
                                                        out Dictionary<string, int> index)
    {
        int n = graph.Vertices.Count;
        index = new Dictionary<string, int>(n);
        for (int i = 0; i < n; i++) index[graph.Vertices[i].Id] = i;

        var sets = new HashSet<int>[n];
        for (int i = 0; i < n; i++) sets[i] = new HashSet<int>();

        foreach (var e in graph.Edges)
        {
            if (e.SourceId == excludedId || e.TargetId == excludedId) continue;
            if (!index.TryGetValue(e.SourceId, out int a) || !index.TryGetValue(e.TargetId, out int b) || a == b)
                continue;
            sets[a].Add(b);
            sets[b].Add(a);
        }

        var adjacency = new List<int>[n];
        for (int i = 0; i < n; i++) adjacency[i] = sets[i].OrderBy(x => x).ToList();
        return adjacency;
    }

    /// <summary>Nodos alcanzables desde start sin pasar por el nodo bloqueado (BFS).</summary>
    private static List<int> Reach(List<int>[] adjacency, int start, int blocked)
    {
        var result  = new List<int> { start };
        var visited = new bool[adjacency.Length];
        visited[start] = true;
        if (blocked >= 0) visited[blocked] = true;
        var queue = new Queue<int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int u = queue.Dequeue();
            foreach (int v in adjacency[u])
            {
                if (visited[v]) continue;
                visited[v] = true;
                result.Add(v);
                queue.Enqueue(v);
            }
        }
        return result;
    }

    /// <summary>Número de componentes conexas del grafo no dirigido.</summary>
    private static int CountComponents(List<int>[] adjacency)
    {
        var seen  = new bool[adjacency.Length];
        int count = 0;
        for (int i = 0; i < adjacency.Length; i++)
        {
            if (seen[i]) continue;
            count++;
            foreach (int v in Reach(adjacency, i, blocked: -1)) seen[v] = true;
        }
        return count;
    }
}
