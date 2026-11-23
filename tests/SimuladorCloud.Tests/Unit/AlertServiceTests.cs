using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>Pruebas unitarias de las alertas visuales (IT-6 · HU-12).</summary>
public class AlertServiceTests
{
    private static (GraphService graph, List<VisualAlert> alerts) Run(int scenario, string? origin)
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(scenario);
        SimulationResult? result = origin is null
            ? new SimulationResult { Type = SimulationType.SpofAnalysis }
            : new CascadeSimulationService(graph).SimulateCascade(TestGraphs.ByName(graph, origin).Id);
        var spof = new SpofDetectionService(graph);
        spof.DetectSpofs();
        return (graph, new AlertService(graph).BuildAlerts(result, spof.AnalyzeAllImpacts()));
    }

    [Fact]
    public void CascadaCritica_GeneraAlertaDePeligro()
    {
        var (_, alerts) = Run(0, "Product DB");

        var danger = Assert.Single(alerts, a => a.Level == AlertLevel.Danger);
        Assert.Equal("Cascada crítica", danger.Title);
        Assert.Contains("Product DB", danger.Message);
        Assert.Contains("5 de 8 nodos (62.5% del sistema)", danger.Message);
    }

    [Fact]
    public void CascadaNoCritica_NoGeneraAlertaDePeligro()
    {
        var (_, alerts) = Run(0, "Load Balancer");

        Assert.DoesNotContain(alerts, a => a.Level == AlertLevel.Danger);
    }

    [Fact]
    public void Spof_GeneraAlertaConLosNodosQueDesconectaria()
    {
        var (_, alerts) = Run(0, null);

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertLevel.Warning, alert.Level);
        Assert.Equal("SPOF detectado: Cache Service", alert.Title);
        Assert.Contains("Storage", alert.Message);
    }

    [Fact]
    public void MuchosSpofs_SeAgrupanDespuesDelLimite()
    {
        var (_, alerts) = Run(2, null);   // 14 SPOF

        Assert.Equal(AlertService.MaxSpofAlerts + 1, alerts.Count);
        Assert.Equal("11 SPOF adicionales", alerts[^1].Title);
        Assert.Equal("SPOF detectado: Notification Service", alerts[0].Title);   // el más crítico primero
    }

    [Fact]
    public void CascadaDeExactamente50Porciento_NoEsCritica()
    {
        var graph = new GraphService();
        var a = graph.AddVertex("A", NodeType.Service);
        var b = graph.AddVertex("B", NodeType.Service);
        var c = graph.AddVertex("C", NodeType.Database);
        graph.AddVertex("D", NodeType.Storage);
        graph.AddEdge(a.Id, c.Id);
        graph.AddEdge(b.Id, c.Id);

        var result = new CascadeSimulationService(graph).SimulateCascade(c.Id);
        var alerts = new AlertService(graph).BuildAlerts(result, new List<SpofImpact>());

        Assert.Equal(50.0, result.AffectedPercent, 1);
        Assert.False(result.IsCriticalImpact);
        Assert.Empty(alerts);
    }

    [Fact]
    public void SinResultadoNiSpofs_NoHayAlertas()
    {
        Assert.Empty(new AlertService(new GraphService()).BuildAlerts(null, new List<SpofImpact>()));
    }
}
