using SimuladorCloud.Client.Models;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Servicio principal para la gestión del grafo G=(V,E).
/// Implementa el CRUD de nodos y aristas (HU-01, HU-02).
/// Opera íntegramente en memoria — sin conexión a nubes reales (RF-12).
/// </summary>
public partial class GraphService
{
    private GraphModel _graph = new();

    /// <summary>Evento que se dispara cuando el grafo es modificado.</summary>
    public event Action? OnGraphChanged;

    /// <summary>Referencia al grafo actual.</summary>
    public GraphModel Graph => _graph;

    // ════════════════════════════════════════════════════════════════════════
    // NODOS (Vertices)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Agrega un nuevo nodo al grafo G=(V,E).
    /// HU-01 — Criterio de aceptación 1.
    /// </summary>
    /// <param name="name">Nombre descriptivo del componente.</param>
    /// <param name="type">Tipo de componente Cloud.</param>
    /// <param name="x">Posición X inicial en el canvas SVG.</param>
    /// <param name="y">Posición Y inicial en el canvas SVG.</param>
    /// <returns>El vértice creado.</returns>
    public Vertex AddVertex(string name, NodeType type,
                            double x = 100, double y = 100)
    {
        var vertex = new Vertex
        {
            Name      = name,
            NodeType  = type,
            Status    = NodeStatus.Operative,
            PositionX = x,
            PositionY = y
        };
        _graph.Vertices.Add(vertex);
        _graph.UpdatedAt = DateTime.UtcNow;
        NotifyChange();
        return vertex;
    }

    /// <summary>
    /// Edita los atributos de un nodo existente.
    /// HU-01 — Criterio de aceptación 3.
    /// </summary>
    /// <returns>True si el nodo fue encontrado y editado.</returns>
    public bool UpdateVertex(string id, string name, NodeType type)
    {
        var vertex = _graph.GetVertex(id);
        if (vertex is null) return false;

        vertex.Name     = name;
        vertex.NodeType = type;
        _graph.UpdatedAt = DateTime.UtcNow;
        NotifyChange();
        return true;
    }

    /// <summary>
    /// Actualiza la posición de un nodo en el canvas SVG (arrastrar y soltar).
    /// HU-04 — Criterio de aceptación 2.
    /// </summary>
    public bool UpdateVertexPosition(string id, double x, double y)
    {
        var vertex = _graph.GetVertex(id);
        if (vertex is null) return false;

        vertex.PositionX = x;
        vertex.PositionY = y;
        NotifyChange();
        return true;
    }

    /// <summary>
    /// Elimina un nodo y todas sus aristas asociadas del grafo.
    /// HU-01 — Criterio de aceptación 4.
    /// </summary>
    /// <returns>True si el nodo fue encontrado y eliminado.</returns>
    public bool RemoveVertex(string id)
    {
        var vertex = _graph.Vertices.FirstOrDefault(v => v.Id == id);
        if (vertex is null) return false;

        // Eliminar todas las aristas que involucran este nodo
        _graph.Edges.RemoveAll(e => e.SourceId == id || e.TargetId == id);
        _graph.Vertices.Remove(vertex);
        _graph.UpdatedAt = DateTime.UtcNow;
        NotifyChange();
        return true;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ARISTAS (Edges)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Valida si se puede crear la arista dirigida e(sourceId, targetId).
    /// HU-02 — Criterios de aceptación 4 y 5 (sin duplicados y grafo válido).
    /// </summary>
    public EdgeValidation ValidateEdge(string sourceId, string targetId)
    {
        if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId) ||
            _graph.GetVertex(sourceId) is null || _graph.GetVertex(targetId) is null)
            return EdgeValidation.MissingVertex;

        if (sourceId == targetId)
            return EdgeValidation.SelfLoop;

        if (_graph.EdgeExists(sourceId, targetId))
            return EdgeValidation.Duplicate;

        return EdgeValidation.Valid;
    }

    /// <summary>
    /// Agrega una arista dirigida e(sourceId, targetId) al grafo G=(V,E).
    /// HU-02 — Criterios de aceptación 1, 4.
    /// </summary>
    /// <returns>La arista creada, o null si la validación falla.</returns>
    public Edge? AddEdge(string sourceId, string targetId, double weight = 1.0)
    {
        if (ValidateEdge(sourceId, targetId) != EdgeValidation.Valid)
            return null;

        var edge = new Edge
        {
            SourceId = sourceId,
            TargetId = targetId,
            Weight   = weight,
            IsActive = true
        };
        _graph.Edges.Add(edge);
        _graph.UpdatedAt = DateTime.UtcNow;
        NotifyChange();
        return edge;
    }

    /// <summary>
    /// Elimina una arista del grafo por su ID.
    /// HU-02 — Criterio de aceptación 3.
    /// </summary>
    public bool RemoveEdge(string edgeId)
    {
        var edge = _graph.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null) return false;

        _graph.Edges.Remove(edge);
        _graph.UpdatedAt = DateTime.UtcNow;
        NotifyChange();
        return true;
    }

    // ════════════════════════════════════════════════════════════════════════
    // GESTIÓN DEL GRAFO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reinicia el estado de la simulación manteniendo la topología.
    /// HU-16 — Reinicio del entorno de simulación.
    /// </summary>
    public void ResetSimulation()
    {
        _graph.ResetSimulation();
        NotifyChange();
    }

    /// <summary>
    /// Limpia completamente el grafo — elimina todos los nodos y aristas.
    /// HU-16 — Limpieza total del entorno.
    /// </summary>
    public void ClearGraph()
    {
        _graph = new GraphModel();
        NotifyChange();
    }

    /// <summary>
    /// Reemplaza el grafo actual con un nuevo modelo (usado al cargar desde localStorage).
    /// HU-03 — Carga de topología guardada.
    /// </summary>
    public void LoadGraph(GraphModel graph)
    {
        _graph = graph;
        NotifyChange();
    }

    /// <summary>
    /// Carga una de las topologías predefinidas para pruebas:
    /// 0 = pequeña (8 nodos), 1 = mediana (20), 2 = grande (50), 3 = compleja (100).
    /// HU-11 — Escenarios de prueba predefinidos.
    /// </summary>
    public void LoadPredefinedTopology(int scenarioIndex)
    {
        _graph = scenarioIndex switch
        {
            0 => CreateSmallTopology(),
            1 => CreateMediumTopology(),
            2 => CreateLargeTopology(),
            3 => CreateComplexTopology(),
            _ => CreateSmallTopology()
        };
        NotifyChange();
    }

    // ════════════════════════════════════════════════════════════════════════
    // TOPOLOGÍAS PREDEFINIDAS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Topología pequeña: 8 nodos / 10 aristas — escenario de prueba IT-1.
    /// </summary>
    private static GraphModel CreateSmallTopology()
    {
        var graph = new GraphModel { Name = "Topología Pequeña (8 nodos)" };

        // Crear nodos
        var v = new[]
        {
            new Vertex { Name = "API Gateway",      NodeType = NodeType.API,          PositionX = 300, PositionY = 60  },
            new Vertex { Name = "Auth Service",     NodeType = NodeType.Service,      PositionX = 150, PositionY = 160 },
            new Vertex { Name = "Product Service",  NodeType = NodeType.Service,      PositionX = 450, PositionY = 160 },
            new Vertex { Name = "Load Balancer",    NodeType = NodeType.LoadBalancer, PositionX = 300, PositionY = 260 },
            new Vertex { Name = "User DB",          NodeType = NodeType.Database,     PositionX = 100, PositionY = 360 },
            new Vertex { Name = "Product DB",       NodeType = NodeType.Database,     PositionX = 300, PositionY = 360 },
            new Vertex { Name = "Cache Service",    NodeType = NodeType.Service,      PositionX = 500, PositionY = 260 },
            new Vertex { Name = "Storage",          NodeType = NodeType.Storage,      PositionX = 500, PositionY = 360 },
        };
        graph.Vertices.AddRange(v);

        // Crear aristas dirigidas
        var edges = new[]
        {
            new Edge { SourceId = v[0].Id, TargetId = v[1].Id },
            new Edge { SourceId = v[0].Id, TargetId = v[2].Id },
            new Edge { SourceId = v[1].Id, TargetId = v[3].Id },
            new Edge { SourceId = v[2].Id, TargetId = v[3].Id },
            new Edge { SourceId = v[3].Id, TargetId = v[4].Id },
            new Edge { SourceId = v[3].Id, TargetId = v[5].Id },
            new Edge { SourceId = v[2].Id, TargetId = v[6].Id },
            new Edge { SourceId = v[6].Id, TargetId = v[5].Id },
            new Edge { SourceId = v[6].Id, TargetId = v[7].Id },
            new Edge { SourceId = v[1].Id, TargetId = v[4].Id },
        };
        graph.Edges.AddRange(edges);
        return graph;
    }

    /// <summary>
    /// Topología mediana: 20 nodos / 28 aristas — escenario de prueba IT-2.
    /// </summary>
    private static GraphModel CreateMediumTopology()
    {
        var graph = new GraphModel { Name = "Topología Mediana (20 nodos)" };

        string[] names =
        [
            "API Gateway","Auth Service","User Service","Product Service",
            "Order Service","Payment Service","Notification Service","Search Service",
            "Load Balancer A","Load Balancer B","User DB","Product DB",
            "Order DB","Payment DB","Cache Redis","Message Queue",
            "File Storage","CDN","Log Service","Monitoring"
        ];
        NodeType[] types =
        [
            NodeType.API, NodeType.Service, NodeType.Service, NodeType.Service,
            NodeType.Service, NodeType.Service, NodeType.Service, NodeType.Service,
            NodeType.LoadBalancer, NodeType.LoadBalancer, NodeType.Database, NodeType.Database,
            NodeType.Database, NodeType.Database, NodeType.Service, NodeType.Service,
            NodeType.Storage, NodeType.Storage, NodeType.Service, NodeType.Service
        ];

        var vertices = names.Select((n, i) => new Vertex
        {
            Name      = n,
            NodeType  = types[i],
            PositionX = 80 + (i % 5) * 160,
            PositionY = 80 + (i / 5) * 120
        }).ToList();
        graph.Vertices.AddRange(vertices);

        // Aristas representativas de una arquitectura de microservicios real
        int[][] edgePairs =
        [
            [0,8],[0,9],[8,1],[8,2],[9,3],[9,4],[1,10],[2,10],
            [3,11],[4,12],[4,5],[5,13],[1,14],[3,14],[4,15],[6,15],
            [7,11],[2,14],[5,17],[6,16],[7,18],[18,19],[0,19],
            [9,6],[8,7],[4,6],[3,7],[15,16]
        ];
        foreach (var pair in edgePairs)
        {
            graph.Edges.Add(new Edge
            {
                SourceId = vertices[pair[0]].Id,
                TargetId = vertices[pair[1]].Id
            });
        }
        return graph;
    }

    // ════════════════════════════════════════════════════════════════════════
    // NOTIFICACIÓN
    // ════════════════════════════════════════════════════════════════════════

    private void NotifyChange() => OnGraphChanged?.Invoke();
}
