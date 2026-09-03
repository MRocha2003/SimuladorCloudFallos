namespace SimuladorCloud.Client.Models;

/// <summary>
/// Representa una arista (dependencia dirigida) en el grafo G=(V,E).
/// Una arista e(u,v) indica que el componente u depende del componente v:
/// el fallo de v puede propagar un fallo en cascada hacia u.
/// HU-02: Modelado de aristas de dependencia.
/// </summary>
public class Edge
{
    /// <summary>Identificador único de la arista.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID del nodo de origen — el nodo que depende del destino.
    /// Si el nodo destino falla, este nodo puede verse afectado.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// ID del nodo destino — el nodo del que se depende.
    /// Su fallo puede desencadenar una cascada hacia el nodo origen.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Peso de la arista utilizado en el análisis de rutas críticas.
    /// Valor por defecto: 1.0
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Indica si la arista está activa en la simulación actual.
    /// Una arista inactiva no participa en la propagación de cascadas.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indica si esta arista forma parte de una ruta de propagación
    /// de cascada activa en la simulación actual.
    /// </summary>
    public bool IsCascadePath { get; set; } = false;

    // ── Propiedades de conveniencia ────────────────────────────────────────────

    /// <summary>Color de la arista según su estado en la simulación.</summary>
    public string StrokeColor => IsCascadePath ? "#E53E3E"  // rojo — en cascada
                                               : "#718096"; // gris — normal

    /// <summary>Grosor de la arista según su estado.</summary>
    public int StrokeWidth => IsCascadePath ? 2 : 1;

    /// <summary>
    /// Crea una copia superficial de la arista.
    /// </summary>
    public Edge Clone() => new()
    {
        Id            = Id,
        SourceId      = SourceId,
        TargetId      = TargetId,
        Weight        = Weight,
        IsActive      = IsActive,
        IsCascadePath = IsCascadePath
    };
}
