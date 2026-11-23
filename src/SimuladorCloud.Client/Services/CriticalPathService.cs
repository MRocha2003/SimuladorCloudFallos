using SimuladorCloud.Client.Models;
using System.Diagnostics;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Servicio de identificación de rutas críticas sobre G=(V,E).
/// Una ruta crítica es la cadena de dependencias más larga que parte de un
/// punto de entrada (nodo del que ningún otro depende) y llega a una
/// dependencia final (nodo que no depende de otro). Entre las rutas
/// candidatas se priorizan las que atraviesan más SPOF y, a igualdad, las
/// más largas.
/// Algoritmo: (1) un DFS descarta las aristas de retroceso para romper los
/// ciclos; (2) sobre el grafo acíclico resultante se calcula por programación
/// dinámica, en orden topológico inverso, el camino más largo desde cada nodo;
/// (3) desde cada punto de entrada se obtiene una ruta candidata por cada una
/// de sus dependencias directas, y se seleccionan las mejores.
/// Complejidad temporal: O(V+E).
/// IT-4 · HU-08 (resaltado de rutas críticas) e IT-5 · HU-10 (longitud).
/// </summary>
public class CriticalPathService
{
    private readonly GraphService _graphService;

    public CriticalPathService(GraphService graphService)
    {
        _graphService = graphService;
    }

    /// <summary>Tiempo de ejecución del último análisis, en milisegundos.</summary>
    public double LastExecutionTimeMs { get; private set; }

    /// <summary>
    /// Identifica las rutas críticas del grafo actual, las guarda en
    /// <see cref="GraphModel.CriticalPaths"/> y marca sus aristas para
    /// resaltarlas en el canvas. Los SPOF se toman de la marca
    /// <see cref="Vertex.IsSpof"/> del último análisis de SPOF.
    /// </summary>
    /// <param name="maxPaths">Número máximo de rutas a devolver.</param>
    public List<CriticalPath> FindCriticalPaths(int maxPaths = 3)
    {
        var graph = _graphService.Graph;
        foreach (var e in graph.Edges) e.IsCriticalPath = false;
        graph.CriticalPaths.Clear();

        int n = graph.Vertices.Count;
        if (n == 0 || maxPaths <= 0) return new List<CriticalPath>();

        var stopwatch = Stopwatch.StartNew();

        var index = new Dictionary<string, int>(n);
        for (int i = 0; i < n; i++) index[graph.Vertices[i].Id] = i;

        // Aristas activas de dependencia u → v (u depende de v)
        var outgoing = new List<Edge>[n];
        for (int i = 0; i < n; i++) outgoing[i] = new List<Edge>();
        foreach (var e in graph.Edges)
        {
            if (!e.IsActive) continue;
            if (index.TryGetValue(e.SourceId, out int u) && index.ContainsKey(e.TargetId))
                outgoing[u].Add(e);
        }

        // (1) DFS iterativo: orden posterior y descarte de aristas de retroceso
        var state     = new byte[n];               // 0 = sin visitar, 1 = en pila, 2 = terminado
        var dagEdges  = new List<Edge>[n];
        for (int i = 0; i < n; i++) dagEdges[i] = new List<Edge>();
        var postOrder = new List<int>(n);

        for (int root = 0; root < n; root++)
        {
            if (state[root] != 0) continue;
            var stack = new Stack<(int node, int next)>();
            stack.Push((root, 0));
            state[root] = 1;
            while (stack.Count > 0)
            {
                var (u, next) = stack.Pop();
                if (next < outgoing[u].Count)
                {
                    stack.Push((u, next + 1));
                    var edge = outgoing[u][next];
                    int v    = index[edge.TargetId];
                    if (state[v] == 1) continue;   // arista de retroceso: rompería el ciclo
                    dagEdges[u].Add(edge);
                    if (state[v] == 0)
                    {
                        state[v] = 1;
                        stack.Push((v, 0));
                    }
                }
                else
                {
                    state[u] = 2;
                    postOrder.Add(u);
                }
            }
        }

        // (2) Camino más largo desde cada nodo (el orden posterior ya es topológico inverso)
        var length = new int[n];
        var weight = new double[n];
        var nextEdge = new Edge?[n];
        foreach (int u in postOrder)
        {
            foreach (var edge in dagEdges[u])
            {
                int v = index[edge.TargetId];
                int candLen    = 1 + length[v];
                double candW   = edge.Weight + weight[v];
                if (candLen > length[u] || (candLen == length[u] && candW > weight[u]))
                {
                    length[u]   = candLen;
                    weight[u]   = candW;
                    nextEdge[u] = edge;
                }
            }
        }

        // Puntos de entrada: nodos con dependencias y sin dependientes en el grafo acíclico
        var hasIncoming = new bool[n];
        for (int u = 0; u < n; u++)
            foreach (var edge in dagEdges[u]) hasIncoming[index[edge.TargetId]] = true;

        // Candidatas: desde cada punto de entrada, la ruta más larga que sale
        // por cada una de sus dependencias directas (rutas alternativas).
        var candidates = new List<(CriticalPath path, List<Edge> edges, string key)>();
        for (int start = 0; start < n; start++)
        {
            if (hasIncoming[start]) continue;
            foreach (var firstEdge in dagEdges[start])
            {
                var path  = new CriticalPath();
                var edges = new List<Edge> { firstEdge };
                path.NodeIds.Add(graph.Vertices[start].Id);
                int cur = index[firstEdge.TargetId];
                path.NodeIds.Add(graph.Vertices[cur].Id);
                while (nextEdge[cur] is { } edge)
                {
                    edges.Add(edge);
                    cur = index[edge.TargetId];
                    path.NodeIds.Add(graph.Vertices[cur].Id);
                }
                path.TotalWeight = edges.Sum(e => e.Weight);
                path.SpofCount   = path.NodeIds.Count(id => graph.GetVertex(id)!.IsSpof);
                candidates.Add((path, edges, string.Join(">", path.NodeIds)));
            }
        }

        var selected = candidates
            .GroupBy(c => c.key).Select(g => g.First())
            .OrderByDescending(c => c.path.SpofCount)
            .ThenByDescending(c => c.path.Length)
            .ThenByDescending(c => c.path.TotalWeight)
            .ThenBy(c => graph.GetVertex(c.path.NodeIds[0])!.Name, StringComparer.Ordinal)
            .Take(maxPaths)
            .ToList();

        foreach (var c in selected)
        {
            foreach (var e in c.edges) e.IsCriticalPath = true;
            graph.CriticalPaths.Add(new List<string>(c.path.NodeIds));
        }

        stopwatch.Stop();
        LastExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        return selected.Select(c => c.path).ToList();
    }
}
