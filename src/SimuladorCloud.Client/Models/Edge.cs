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

    /// <summary>
    /// Indica si esta arista forma parte de una ruta crítica identificada
    /// en el último análisis (IT-4 · HU-08).
    /// </summary>
    public bool IsCriticalPath { get; set; } = false;

    // ── Propiedades de conveniencia ────────────────────────────────────────────

    /// <summary>
    /// Color de la arista según su estado en la simulación: rojo si propaga
    /// la cascada, violeta si pertenece a una ruta crítica y gris en otro caso.
    /// </summary>
    public string StrokeColor => IsCascadePath  ? "#E53E3E"  // rojo — en cascada
                               : IsCriticalPath ? "#805AD5"  // violeta — ruta crítica
                                                : "#718096"; // gris — normal

    /// <summary>Grosor de la arista según su estado.</summary>
    public int StrokeWidth => IsCascadePath ? 2 : IsCriticalPath ? 3 : 1;

    /// <summary>Marcador de flecha SVG que corresponde al estado de la arista.</summary>
    public string MarkerId => IsCascadePath ? "arrow-red" : IsCriticalPath ? "arrow-purple" : "arrow";

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
        IsCascadePath  = IsCascadePath,
        IsCriticalPath = IsCriticalPath
    };
}
