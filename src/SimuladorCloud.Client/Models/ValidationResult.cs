namespace SimuladorCloud.Client.Models;

/// <summary>
/// Resultado de la validación funcional de los algoritmos sobre una
/// topología predefinida (IT-7 · HU-13; IT-8 · HU-15 para los tiempos).
/// </summary>
public class ValidationResult
{
    /// <summary>Nombre de la topología validada.</summary>
    public string TopologyName { get; set; } = string.Empty;

    /// <summary>Número de nodos de la topología.</summary>
    public int NodeCount { get; set; }

    /// <summary>Número de aristas de la topología.</summary>
    public int EdgeCount { get; set; }

    /// <summary>Número de SPOF esperados según la verificación manual.</summary>
    public int ExpectedSpofCount { get; set; }

    /// <summary>Número de SPOF detectados por el algoritmo de Tarjan.</summary>
    public int DetectedSpofCount { get; set; }

    /// <summary>SPOF esperados que el algoritmo no detectó.</summary>
    public List<string> MissingSpofs { get; set; } = new();

    /// <summary>SPOF detectados que no estaban en la verificación manual.</summary>
    public List<string> UnexpectedSpofs { get; set; } = new();

    /// <summary>Indica si los SPOF coinciden exactamente con la verificación manual.</summary>
    public bool SpofsMatch => MissingSpofs.Count == 0 && UnexpectedSpofs.Count == 0;

    /// <summary>Comprobaciones de la propagación en cascada.</summary>
    public List<CascadeCheck> CascadeChecks { get; set; } = new();

    /// <summary>Indica si todas las cascadas alcanzaron exactamente los nodos esperados.</summary>
    public bool CascadeMatch => CascadeChecks.All(c => c.Passed);

    /// <summary>Indica si ejecuciones sucesivas produjeron resultados idénticos.</summary>
    public bool Reproducible { get; set; }

    /// <summary>Mediana del tiempo de la propagación en cascada, en milisegundos.</summary>
    public double CascadeMs { get; set; }

    /// <summary>Mediana del tiempo de detección de SPOF y su impacto, en milisegundos.</summary>
    public double SpofMs { get; set; }

    /// <summary>Mediana del tiempo de identificación de rutas críticas, en milisegundos.</summary>
    public double CriticalPathMs { get; set; }

    /// <summary>Indica si todas las operaciones se mantienen bajo el límite de tiempo.</summary>
    public bool WithinTimeLimit { get; set; }

    /// <summary>Indica si la topología superó todas las comprobaciones.</summary>
    public bool Passed => SpofsMatch && CascadeMatch && Reproducible && WithinTimeLimit;
}

/// <summary>Comprobación de una cascada contra el valor de la verificación manual.</summary>
public class CascadeCheck
{
    /// <summary>Nombre del nodo de origen del fallo.</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>Número de nodos afectados esperado.</summary>
    public int ExpectedAffected { get; set; }

    /// <summary>Número de nodos afectados obtenido.</summary>
    public int ActualAffected { get; set; }

    /// <summary>Indica si coinciden.</summary>
    public bool Passed => ExpectedAffected == ActualAffected;
}
