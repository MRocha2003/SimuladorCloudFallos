namespace SimuladorCloud.Client.Models;

/// <summary>
/// Representa el modelo completo del grafo dirigido G=(V,E) de la
/// infraestructura Cloud. Contiene la colección de vértices (nodos),
/// la colección de aristas (dependencias) y los metadatos de la topología.
/// </summary>
public class GraphModel
{
    /// <summary>Identificador único de la topología.</summary>
    public string TopologyId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Nombre descriptivo de la topología (asignado por el usuario).</summary>
    public string Name { get; set; } = "Nueva topología";

    /// <summary>Colección de vértices V del grafo G=(V,E).</summary>
    public List<Vertex> Vertices { get; set; } = new();

    /// <summary>Colección de aristas dirigidas E del grafo G=(V,E).</summary>
    public List<Edge> Edges { get; set; } = new();

    /// <summary>
    /// Lista de IDs de nodos identificados como SPOFs en el último análisis.
    /// </summary>
    public List<string> Spofs { get; set; } = new();

    /// <summary>
    /// Lista de rutas críticas identificadas en el último análisis.
    /// Cada ruta es una secuencia de IDs de nodos.
    /// </summary>
    public List<List<string>> CriticalPaths { get; set; } = new();

    /// <summary>Lista de IDs de nodos marcados como fallidos en la simulación actual.</summary>
    public List<string> FailedNodes { get; set; } = new();

    /// <summary>Marca de tiempo de creación de la topología.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Marca de tiempo de última modificación.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Propiedades de conveniencia ────────────────────────────────────────────

    /// <summary>Número total de nodos en el grafo.</summary>
    public int NodeCount => Vertices.Count;

    /// <summary>Número total de aristas en el grafo.</summary>
    public int EdgeCount => Edges.Count;

    /// <summary>Número de nodos operativos.</summary>
    public int OperativeCount => Vertices.Count(v => v.Status == NodeStatus.Operative);

    /// <summary>Número de nodos degradados.</summary>
    public int DegradedCount => Vertices.Count(v => v.Status == NodeStatus.Degraded);

    /// <summary>Número de nodos fallidos.</summary>
    public int FailedCount => Vertices.Count(v => v.Status == NodeStatus.Failed);

    /// <summary>
    /// Obtiene un vértice por su ID. Devuelve null si no existe.
    /// </summary>
    public Vertex? GetVertex(string id) =>
        Vertices.FirstOrDefault(v => v.Id == id);

    /// <summary>
    /// Verifica si existe una arista dirigida entre dos nodos.
    /// </summary>
    public bool EdgeExists(string sourceId, string targetId) =>
        Edges.Any(e => e.SourceId == sourceId && e.TargetId == targetId);

    /// <summary>
    /// Obtiene las aristas activas que salen de un nodo: e(vertexId, v),
    /// es decir, los componentes de los que el nodo depende.
    /// </summary>
    public IEnumerable<Edge> GetOutgoingEdges(string vertexId) =>
        Edges.Where(e => e.SourceId == vertexId && e.IsActive);

    /// <summary>
    /// Obtiene las aristas activas que llegan a un nodo: e(u, vertexId),
    /// es decir, las dependencias de otros componentes hacia este nodo.
    /// Son las aristas por las que se propaga un fallo del nodo (IT-3).
    /// </summary>
    public IEnumerable<Edge> GetIncomingEdges(string vertexId) =>
        Edges.Where(e => e.TargetId == vertexId && e.IsActive);

    /// <summary>
    /// Obtiene los nodos que dependen del nodo dado (orígenes de sus aristas
    /// entrantes). Si el nodo falla, estos nodos pueden verse afectados.
    /// </summary>
    public IEnumerable<Vertex> GetDependents(string vertexId) =>
        GetIncomingEdges(vertexId)
            .Select(e => GetVertex(e.SourceId))
            .Where(v => v is not null)
            .Cast<Vertex>();

    /// <summary>
    /// Obtiene los nodos de los que depende el nodo dado (destinos de sus
    /// aristas salientes).
    /// </summary>
    public IEnumerable<Vertex> GetDependencies(string vertexId) =>
        GetOutgoingEdges(vertexId)
            .Select(e => GetVertex(e.TargetId))
            .Where(v => v is not null)
            .Cast<Vertex>();

    /// <summary>
    /// Reinicia el estado de todos los nodos y aristas a su estado inicial.
    /// </summary>
    public void ResetSimulation()
    {
        foreach (var vertex in Vertices)
        {
            vertex.Status = NodeStatus.Operative;
            vertex.IsSpof = false;
        }
        foreach (var edge in Edges)
        {
            edge.IsCascadePath = false;
        }
        Spofs.Clear();
        CriticalPaths.Clear();
        FailedNodes.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Crea una copia profunda del grafo.
    /// </summary>
    public GraphModel DeepClone() => new()
    {
        TopologyId    = TopologyId,
        Name          = Name,
        Vertices      = Vertices.Select(v => v.Clone()).ToList(),
        Edges         = Edges.Select(e => e.Clone()).ToList(),
        Spofs         = new List<string>(Spofs),
        CriticalPaths = CriticalPaths.Select(p => new List<string>(p)).ToList(),
        FailedNodes   = new List<string>(FailedNodes),
        CreatedAt     = CreatedAt,
        UpdatedAt     = UpdatedAt
    };
}
