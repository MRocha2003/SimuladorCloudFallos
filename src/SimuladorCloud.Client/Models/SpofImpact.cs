namespace SimuladorCloud.Client.Models;

/// <summary>
/// Impacto estimado de la caída de un Punto Único de Fallo (SPOF).
/// Combina dos efectos complementarios:
/// (1) desconexión: nodos que quedan separados del resto de la arquitectura
///     al eliminar el SPOF del grafo no dirigido subyacente (Tarjan); y
/// (2) cascada: nodos que dependen, directa o indirectamente, del SPOF y
///     fallarían por propagación (BFS sobre las aristas entrantes).
/// IT-4 · HU-08 (información de impacto) e IT-6 · HU-12 (alertas).
/// </summary>
public class SpofImpact
{
    /// <summary>ID del nodo identificado como SPOF.</summary>
    public string VertexId { get; set; } = string.Empty;

    /// <summary>Nombre del nodo identificado como SPOF.</summary>
    public string VertexName { get; set; } = string.Empty;

    /// <summary>Tipo de componente del SPOF.</summary>
    public NodeType NodeType { get; set; }

    /// <summary>
    /// IDs de los nodos que quedan desconectados del componente principal
    /// cuando se elimina el SPOF.
    /// </summary>
    public List<string> DisconnectedNodes { get; set; } = new();

    /// <summary>IDs de los nodos que fallarían en cascada si el SPOF falla.</summary>
    public List<string> CascadeAffected { get; set; } = new();

    /// <summary>Número de componentes conexas en que se divide el grafo sin el SPOF.</summary>
    public int ComponentsAfterRemoval { get; set; }

    /// <summary>
    /// Criticidad del SPOF: número de nodos distintos afectados por la
    /// desconexión o por la cascada. Se usa para ordenar los SPOF.
    /// </summary>
    public int CriticalityScore => DisconnectedNodes.Union(CascadeAffected).Count();
}
