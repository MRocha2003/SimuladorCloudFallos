namespace SimuladorCloud.Client.Models;

/// <summary>
/// Tipos de componentes de infraestructura Cloud representados como nodos del grafo G=(V,E).
/// </summary>
public enum NodeType
{
    /// <summary>Microservicio o servicio web.</summary>
    Service,

    /// <summary>Base de datos (relacional o NoSQL).</summary>
    Database,

    /// <summary>Interfaz de programación de aplicaciones (API Gateway o REST API).</summary>
    API,

    /// <summary>Balanceador de carga.</summary>
    LoadBalancer,

    /// <summary>Almacenamiento de objetos o archivos.</summary>
    Storage
}

/// <summary>
/// Estado operativo de un nodo en el grafo durante la simulación.
/// </summary>
public enum NodeStatus
{
    /// <summary>El componente funciona con normalidad.</summary>
    Operative,

    /// <summary>El componente funciona con degradación de rendimiento.</summary>
    Degraded,

    /// <summary>El componente ha fallado y está fuera de servicio.</summary>
    Failed
}

/// <summary>
/// Tipo de simulación o análisis ejecutado sobre el grafo G=(V,E).
/// </summary>
public enum SimulationType
{
    /// <summary>Simulación de propagación de fallos en cascada mediante BFS/DFS.</summary>
    CascadeFailure,

    /// <summary>Análisis de Puntos Únicos de Fallo (SPOFs) mediante algoritmo de Tarjan.</summary>
    SpofAnalysis,

    /// <summary>Identificación de rutas críticas por centralidad de intermediación.</summary>
    CriticalPath
}

/// <summary>
/// Resultado de validar la creación de una arista dirigida (HU-02).
/// </summary>
public enum EdgeValidation
{
    /// <summary>La arista puede crearse.</summary>
    Valid,

    /// <summary>El nodo origen o el destino no existe.</summary>
    MissingVertex,

    /// <summary>El origen y el destino son el mismo nodo (auto-bucle).</summary>
    SelfLoop,

    /// <summary>Ya existe una arista con el mismo origen y destino.</summary>
    Duplicate
}
