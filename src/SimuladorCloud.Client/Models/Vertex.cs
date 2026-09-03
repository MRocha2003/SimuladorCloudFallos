namespace SimuladorCloud.Client.Models;

/// <summary>
/// Representa un nodo (vértice) en el grafo G=(V,E).
/// Cada vértice corresponde a un componente de infraestructura Cloud.
/// HU-01: Modelado de nodos de infraestructura.
/// </summary>
public class Vertex
{
    /// <summary>Identificador único del nodo.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Nombre descriptivo del componente (p. ej., "API Gateway", "DB Principal").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tipo de componente de infraestructura Cloud.</summary>
    public NodeType NodeType { get; set; } = NodeType.Service;

    /// <summary>Estado operativo actual del nodo.</summary>
    public NodeStatus Status { get; set; } = NodeStatus.Operative;

    /// <summary>Coordenada horizontal en el canvas SVG (píxeles).</summary>
    public double PositionX { get; set; } = 100;

    /// <summary>Coordenada vertical en el canvas SVG (píxeles).</summary>
    public double PositionY { get; set; } = 100;

    /// <summary>
    /// Indica si el nodo fue identificado como Punto Único de Fallo (SPOF)
    /// por el algoritmo de Tarjan.
    /// </summary>
    public bool IsSpof { get; set; } = false;

    /// <summary>Marca de tiempo de creación del nodo en la sesión actual.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Propiedades de conveniencia para la visualización SVG ──────────────────

    /// <summary>Color del nodo según su estado operativo.</summary>
    public string StatusColor => Status switch
    {
        NodeStatus.Operative => "#2F855A",  // verde
        NodeStatus.Degraded  => "#D69E2E",  // amarillo
        NodeStatus.Failed    => "#E53E3E",  // rojo
        _                    => "#718096"   // gris
    };

    /// <summary>Ícono SVG según el tipo de componente.</summary>
    public string TypeIcon => NodeType switch
    {
        NodeType.Service      => "⚙",
        NodeType.Database     => "🗄",
        NodeType.API          => "🔌",
        NodeType.LoadBalancer => "⚖",
        NodeType.Storage      => "💾",
        _                     => "□"
    };

    /// <summary>Crea una copia superficial del vértice.</summary>
    public Vertex Clone() => new()
    {
        Id        = Id,
        Name      = Name,
        NodeType  = NodeType,
        Status    = Status,
        PositionX = PositionX,
        PositionY = PositionY,
        IsSpof    = IsSpof,
        CreatedAt = CreatedAt
    };
}
