using System.Diagnostics;
using SimuladorCloud.Client.Models;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Validación funcional de los algoritmos (BFS, Tarjan y rutas críticas)
/// sobre las cuatro topologías predefinidas, contra valores de referencia
/// verificados manualmente con una implementación independiente (NetworkX).
/// Trabaja sobre copias propias de las topologías: no modifica el grafo que
/// el usuario tiene abierto en el simulador.
/// IT-7 · HU-13 (PT-26 a PT-29) e IT-8 · HU-15 (PT-30 y PT-34).
/// </summary>
public class AlgorithmValidationService
{
    /// <summary>Límite de tiempo de respuesta por operación, en milisegundos (HU-15).</summary>
    public const double TimeLimitMs = 120.0;

    /// <summary>Número de ejecuciones usadas para comprobar la reproducibilidad.</summary>
    public const int ReproducibilityRuns = 3;

    /// <summary>Número de mediciones de las que se toma la mediana del tiempo.</summary>
    public const int TimingRuns = 5;

    /// <summary>Valores de referencia de la verificación manual para un escenario.</summary>
    private sealed record Reference(int Scenario, string[] Spofs, (string Origin, int Affected)[] Cascades);

    private static readonly Reference[] References =
    {
        new(0, new[] { "Cache Service" },
            new[] { ("Product DB", 5), ("Load Balancer", 3), ("API Gateway", 0) }),
        new(1, new[] { "Load Balancer B", "Order Service", "Payment Service" },
            new[] { ("Cache Redis", 6), ("Monitoring", 6), ("Product DB", 5) }),
        new(2, new[]
            {
                "Analytics Service", "Auth Service", "Cart Service", "Email Queue", "Inventory Service",
                "Log Service", "Notification Service", "Order Service", "Payment Service", "Product Service",
                "Promotion Service", "Review Service", "Shipping Service", "User Service"
            },
            new[] { ("Product DB", 8), ("Session Cache", 9), ("Event Bus", 7) }),
        new(3, RegionalSpofs().Concat(new[] { "Global API Gateway", "Global DNS", "Global Monitoring" }).ToArray(),
            new[] { ("Identity Provider", 11), ("Disaster Recovery Storage", 19), ("Global Billing DB", 12) }),
    };

    private static IEnumerable<string> RegionalSpofs()
    {
        var services = new[] { "Account", "Audit", "Auth", "Catalog", "Inventory", "Payment", "Report", "Search", "Shipping" };
        foreach (var r in new[] { "R1", "R2", "R3" })
        {
            foreach (var s in services) yield return $"{s} Service ({r})";
            yield return $"Load Balancer ({r})";
        }
    }

    /// <summary>Indica si el código se está ejecutando en WebAssembly dentro del navegador.</summary>
    public static bool RunningInBrowser => OperatingSystem.IsBrowser();

    /// <summary>Valida los cuatro escenarios predefinidos.</summary>
    public List<ValidationResult> RunAll() => References.Select(r => Validate(r)).ToList();

    /// <summary>Valida un escenario predefinido (0 = pequeño … 3 = complejo).</summary>
    public ValidationResult Run(int scenario) => Validate(References.Single(r => r.Scenario == scenario));

    private static ValidationResult Validate(Reference reference)
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(reference.Scenario);
        var cascade = new CascadeSimulationService(graph);
        var spof    = new SpofDetectionService(graph);
        var paths   = new CriticalPathService(graph);
        string IdOf(string name) => graph.Graph.Vertices.Single(v => v.Name == name).Id;

        var result = new ValidationResult
        {
            TopologyName = graph.Graph.Name,
            NodeCount    = graph.Graph.NodeCount,
            EdgeCount    = graph.Graph.EdgeCount,
            ExpectedSpofCount = reference.Spofs.Length
        };

        // PT-26 a PT-28: SPOF frente a la verificación manual
        var detected = spof.DetectSpofs().Select(v => v.Name).ToHashSet();
        result.DetectedSpofCount = detected.Count;
        result.MissingSpofs      = reference.Spofs.Where(s => !detected.Contains(s)).OrderBy(s => s).ToList();
        result.UnexpectedSpofs   = detected.Where(s => !reference.Spofs.Contains(s)).OrderBy(s => s).ToList();

        // HU-13 · criterio 3: la propagación alcanza los nodos esperados
        foreach (var (origin, affected) in reference.Cascades)
        {
            result.CascadeChecks.Add(new CascadeCheck
            {
                Origin           = origin,
                ExpectedAffected = affected,
                ActualAffected   = cascade.SimulateCascade(IdOf(origin)).AffectedCount
            });
        }

        // PT-29: reproducibilidad de la cascada y de la detección de SPOF
        var mainOrigin = IdOf(reference.Cascades.OrderByDescending(c => c.Affected).First().Origin);
        var cascadeRuns = Enumerable.Range(0, ReproducibilityRuns)
            .Select(_ => string.Join(",", cascade.SimulateCascade(mainOrigin).AffectedNodes.OrderBy(x => x)));
        var spofRuns = Enumerable.Range(0, ReproducibilityRuns)
            .Select(_ => string.Join(",", spof.DetectSpofs().Select(v => v.Id).OrderBy(x => x)));
        result.Reproducible = cascadeRuns.Distinct().Count() == 1 && spofRuns.Distinct().Count() == 1;

        // PT-30 y PT-34: tiempos (mediana de varias mediciones)
        result.CascadeMs      = Median(() => cascade.SimulateCascade(mainOrigin));
        result.SpofMs         = Median(() => { spof.DetectSpofs(); spof.AnalyzeAllImpacts(); });
        result.CriticalPathMs = Median(() => paths.FindCriticalPaths());
        result.WithinTimeLimit = result.CascadeMs < TimeLimitMs && result.SpofMs < TimeLimitMs && result.CriticalPathMs < TimeLimitMs;

        graph.ResetSimulation();
        return result;
    }

    /// <summary>Mediana del tiempo de ejecución de una acción, tras una ejecución de calentamiento.</summary>
    private static double Median(Action action)
    {
        action();
        var times = new double[TimingRuns];
        for (int i = 0; i < TimingRuns; i++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            times[i] = sw.Elapsed.TotalMilliseconds;
        }
        Array.Sort(times);
        return times[TimingRuns / 2];
    }
}
