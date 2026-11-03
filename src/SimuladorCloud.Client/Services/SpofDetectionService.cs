using SimuladorCloud.Client.Models;
using System.Diagnostics;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Servicio de detección de Puntos Únicos de Fallo (SPOFs) mediante el
/// algoritmo de puntos de articulación basado en DFS con tiempos de
/// descubrimiento y valores low-link.
/// Implementa RF-06.
/// Complejidad temporal: O(V+E).
/// HU-07: Detección automática de SPOFs.
/// </summary>
public class SpofDetectionService
{
    private readonly GraphService _graphService;

    // Estado del algoritmo DFS
    private int              _timer       = 0;
    private int[]?           _discovery;
    private int[]?           _low;
    private bool[]?          _visited;
    private bool[]?          _isSpof;
    private List<Vertex>?    _vertices;
    private List<Edge>?      _edges;
    private Dictionary<string, int>? _indexMap;

    public SpofDetectionService(GraphService graphService)
    {
        _graphService = graphService;
    }

    /// <summary>
    /// Ejecuta el algoritmo de detección de SPOFs sobre el grafo actual.
    /// </summary>
    /// <returns>Lista de vértices identificados como SPOFs.</returns>
    public List<Vertex> DetectSpofs()
    {
        var graph    = _graphService.Graph;
        _vertices    = graph.Vertices;
        _edges       = graph.Edges;
        int n        = _vertices.Count;

        if (n == 0) return new List<Vertex>();

        // Limpiar el resultado de un análisis anterior
        foreach (var vertex in _vertices) vertex.IsSpof = false;
        graph.Spofs.Clear();

        var stopwatch = Stopwatch.StartNew();

        // Inicializar estructuras del algoritmo
        _timer     = 0;
        _discovery = new int[n];
        _low       = new int[n];
        _visited   = new bool[n];
        _isSpof    = new bool[n];
        _indexMap  = new Dictionary<string, int>();

        for (int i = 0; i < n; i++)
        {
            _discovery[i] = -1;
            _indexMap[_vertices[i].Id] = i;
        }

        // Ejecutar DFS desde cada vértice no visitado (grafo posiblemente no conexo)
        for (int i = 0; i < n; i++)
        {
            if (!_visited[i])
                DfsArticulation(i, -1);
        }

        stopwatch.Stop();

        // Marcar SPOFs en el grafo y recopilar resultado
        var spofs = new List<Vertex>();
        for (int i = 0; i < n; i++)
        {
            if (_isSpof![i])
            {
                _vertices[i].IsSpof = true;
                spofs.Add(_vertices[i]);
                if (!graph.Spofs.Contains(_vertices[i].Id))
                    graph.Spofs.Add(_vertices[i].Id);
            }
        }

        return spofs;
    }

    /// <summary>
    /// DFS recursivo para detección de puntos de articulación.
    /// Un vértice u es punto de articulación si:
    /// 1. Es raíz del árbol DFS y tiene 2+ hijos.
    /// 2. No es raíz y tiene un hijo v tal que low[v] >= discovery[u].
    /// </summary>
    private void DfsArticulation(int u, int parent)
    {
        _visited![u]   = true;
        _discovery![u] = _low![u] = _timer++;
        int children   = 0;

        // Obtener vecinos de salida del vértice u
        var neighbors = GetNeighborIndices(u);

        foreach (int v in neighbors)
        {
            if (!_visited[v])
            {
                children++;
                DfsArticulation(v, u);

                // Actualizar low[u]
                _low[u] = Math.Min(_low[u], _low[v]);

                // Condición 1: raíz con 2+ hijos
                if (parent == -1 && children > 1)
                    _isSpof![u] = true;

                // Condición 2: no es raíz y low[v] >= discovery[u]
                if (parent != -1 && _low[v] >= _discovery[u])
                    _isSpof![u] = true;
            }
            else if (v != parent)
            {
                // Arista de retroceso — actualizar low[u]
                _low[u] = Math.Min(_low[u], _discovery[v]);
            }
        }
    }

    /// <summary>
    /// Obtiene los índices de los vecinos del vértice con índice u.
    /// Considera el grafo como no dirigido para la detección de articulaciones.
    /// </summary>
    private IEnumerable<int> GetNeighborIndices(int u)
    {
        var vertexId  = _vertices![u].Id;
        var neighbors = new List<int>();

        foreach (var edge in _edges!)
        {
            if (edge.SourceId == vertexId && _indexMap!.TryGetValue(edge.TargetId, out int vi))
                neighbors.Add(vi);
            else if (edge.TargetId == vertexId && _indexMap!.TryGetValue(edge.SourceId, out int ui))
                neighbors.Add(ui);
        }
        return neighbors.Distinct();
    }
}
