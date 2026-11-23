namespace SimuladorCloud.Client.Models;

/// <summary>Nivel de severidad de una alerta visual.</summary>
public enum AlertLevel
{
    /// <summary>Advertencia (p. ej., SPOF detectado).</summary>
    Warning,

    /// <summary>Peligro (p. ej., cascada crítica).</summary>
    Danger
}

/// <summary>
/// Alerta visual no bloqueante que se muestra sobre el canvas y puede
/// descartarse. IT-6 · HU-12.
/// </summary>
public class VisualAlert
{
    /// <summary>Identificador único de la alerta.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Severidad de la alerta.</summary>
    public AlertLevel Level { get; set; }

    /// <summary>Título breve de la alerta.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detalle de la alerta.</summary>
    public string Message { get; set; } = string.Empty;
}
