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

    /// <summary>Crea el servicio.</summary>
    /// <param name="graphService">Servicio que contiene el grafo actual.</param>
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
        var graph = _graphService.Graph;
        if (graph.GetVertex(vertexId) is null) return new SpofImpact { VertexId = vertexId };
        return AnalyzeImpact(new ImpactContext(graph), vertexId);
    }

    /// <summary>
    /// Calcula el impacto de todos los SPOF del último análisis (o ejecuta
    /// el análisis si aún no se hizo) y los ordena de mayor a menor criticidad.
    /// Las estructuras auxiliares se construyen una sola vez para todos los SPOF.
    /// </summary>
    public List<SpofImpact> AnalyzeAllImpacts()
    {
        var graph = _graphService.Graph;
        if (graph.Spofs.Count == 0) DetectSpofs();
        if (graph.Spofs.Count == 0) return new List<SpofImpact>();

        var context = new ImpactContext(graph);
        return graph.Spofs
                    .Select(id => AnalyzeImpact(context, id))
                    .OrderByDescending(i => i.CriticalityScore)
                    .ThenBy(i => i.VertexName)
                    .ToList();
    }

    private static SpofImpact AnalyzeImpact(ImpactContext ctx, string vertexId)
    {
        var graph   = ctx.Graph;
        int removed = ctx.Index[vertexId];
        var vertex  = graph.Vertices[removed];

        // Cascada: nodos que dependen (directa o indirectamente) del nodo
        var cascade = ctx.TransitiveDependents(removed)
                         .Select(i => graph.Vertices[i])
                         .OrderBy(v => v.Name)
                         .Select(v => v.Id)
                         .ToList();

        // Desconexión: fragmentos en que se divide el componente del nodo al quitarlo
        var pieces = new List<List<int>>();
        int stamp  = ctx.NewStamp();
        ctx.Mark[removed] = stamp;
        foreach (int start in ctx.Adjacency[removed])
        {
            if (ctx.Mark[start] == stamp) continue;
            pieces.Add(ctx.Reach(start, stamp));
        }

        var main = pieces.OrderByDescending(p => p.Count).FirstOrDefault() ?? new List<int>();
        return new SpofImpact
        {
            VertexId               = vertex.Id,
            VertexName             = vertex.Name,
            NodeType               = vertex.NodeType,
            CascadeAffected        = cascade,
            ComponentsAfterRemoval = ctx.ComponentCount - 1 + pieces.Count,
            DisconnectedNodes      = pieces.Where(p => !ReferenceEquals(p, main))
                                           .SelectMany(p => p)
                                           .Select(i => graph.Vertices[i])
                                           .OrderBy(v => v.Name)
                                           .Select(v => v.Id)
                                           .ToList()
        };
    }

    /// <summary>
    /// Estructuras auxiliares para el análisis de impacto: índice de nodos,
    /// adyacencia no dirigida, dependientes directos y número de componentes.
    /// Se reutilizan marcas por "sello" para evitar asignar memoria en cada recorrido.
    /// </summary>
    private sealed class ImpactContext
    {
        public GraphModel Graph { get; }
        public Dictionary<string, int> Index { get; }
        public List<int>[] Adjacency { get; }
        public List<int>[] Dependents { get; }
        public int ComponentCount { get; }
        public int[] Mark { get; }
        private int _stamp;
        private readonly Queue<int> _queue = new();

        public ImpactContext(GraphModel graph)
        {
            Graph     = graph;
            Adjacency = BuildUndirectedAdjacency(graph, excludedId: null, out var index);
            Index     = index;
            int n     = graph.Vertices.Count;
            Mark      = new int[n];

            Dependents = new List<int>[n];
            for (int i = 0; i < n; i++) Dependents[i] = new List<int>();
            foreach (var e in graph.Edges)
                if (e.IsActive && index.TryGetValue(e.SourceId, out int s) && index.TryGetValue(e.TargetId, out int t) && s != t)
                    Dependents[t].Add(s);

            int stamp = NewStamp(), count = 0;
            for (int i = 0; i < n; i++)
            {
                if (Mark[i] == stamp) continue;
                count++;
                Mark[i] = stamp;
                Reach(i, stamp);
            }
            ComponentCount = count;
        }

        public int NewStamp() => ++_stamp;

        /// <summary>BFS no dirigido desde start sobre los nodos aún no marcados con el sello.</summary>
        public List<int> Reach(int start, int stamp)
        {
            var result = new List<int> { start };
            Mark[start] = stamp;
            _queue.Clear();
            _queue.Enqueue(start);
            while (_queue.Count > 0)
            {
                int u = _queue.Dequeue();
                foreach (int v in Adjacency[u])
                {
                    if (Mark[v] == stamp) continue;
                    Mark[v] = stamp;
                    result.Add(v);
                    _queue.Enqueue(v);
                }
            }
            return result;
        }

        /// <summary>BFS por las aristas activas entrantes: dependientes transitivos del nodo.</summary>
        public List<int> TransitiveDependents(int origin)
        {
            int stamp  = NewStamp();
            var result = new List<int>();
            Mark[origin] = stamp;
            _queue.Clear();
            _queue.Enqueue(origin);
            while (_queue.Count > 0)
            {
                int u = _queue.Dequeue();
                foreach (int d in Dependents[u])
                {
                    if (Mark[d] == stamp) continue;
                    Mark[d] = stamp;
                    result.Add(d);
                    _queue.Enqueue(d);
                }
            }
            return result;
        }
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
}
