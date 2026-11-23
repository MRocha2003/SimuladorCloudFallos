namespace SimuladorCloud.Client.Models;

/// <summary>
/// Ruta crítica: cadena de dependencias que parte de un punto de entrada
/// (componente del que ningún otro depende, p. ej. un API Gateway) y llega
/// hasta una dependencia final (componente que no depende de otro, p. ej.
/// una base de datos). Una falla en cualquier nodo de la ruta interrumpe
/// todo el flujo que pasa por ella.
/// IT-4 · HU-08 y IT-5 · HU-10.
/// </summary>
public class CriticalPath
{
    /// <summary>IDs de los nodos de la ruta, desde el punto de entrada hasta la dependencia final.</summary>
    public List<string> NodeIds { get; set; } = new();

    /// <summary>Longitud de la ruta en número de aristas (saltos de dependencia).</summary>
    public int Length => Math.Max(0, NodeIds.Count - 1);

    /// <summary>Suma de los pesos de las aristas de la ruta.</summary>
    public double TotalWeight { get; set; }

    /// <summary>Número de SPOF que atraviesa la ruta.</summary>
    public int SpofCount { get; set; }
}
