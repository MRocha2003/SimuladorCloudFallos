using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias del reporte de impacto (IT-5 · HU-09): contenido
/// estructurado, nombres legibles y versión en texto para copiar.
/// </summary>
public class ReportServiceTests
{
    /// <summary>Ejecuta el mismo flujo que la interfaz: cascada, SPOF, rutas y reporte.</summary>
    private static (GraphService graph, ImpactReport report) SimulateAndReport(int scenario, string origin)
    {
        var graph  = new GraphService();
        graph.LoadPredefinedTopology(scenario);
        var result = new CascadeSimulationService(graph).SimulateCascade(TestGraphs.ByName(graph, origin).Id);
        var spof   = new SpofDetectionService(graph);
        spof.DetectSpofs();
        var impacts = spof.AnalyzeAllImpacts();
        var paths   = new CriticalPathService(graph).FindCriticalPaths();
        return (graph, new ReportService(graph).BuildReport(result, impacts, paths));
    }

    [Fact]
    public void Reporte_TrasCascada_IncluyeOrigenAfectadosSpofsYRutas()
    {
        var (_, report) = SimulateAndReport(0, "Product DB");

        Assert.Equal("Product DB", report.OriginName);
        Assert.Equal(5, report.AffectedCount);
        Assert.Equal(62.5, report.AffectedPercent, 1);
        Assert.True(report.IsCriticalImpact);
        Assert.Equal(8, report.TotalNodes);
        var spof = Assert.Single(report.Spofs);
        Assert.Equal("Cache Service", spof.Name);
        Assert.Equal(new[] { "Storage" }, spof.DisconnectedNames);
        Assert.Equal(2, report.CriticalPaths.Count);
        Assert.Equal(3, report.CriticalPaths[0].Length);
    }

    [Fact]
    public void Reporte_AfectadosConNombresOrdenados()
    {
        var (_, report) = SimulateAndReport(0, "Load Balancer");

        Assert.Equal(new[] { "API Gateway", "Auth Service", "Product Service" }, report.AffectedNames);
        Assert.False(report.IsCriticalImpact);
    }

    [Fact]
    public void TextoDelReporte_MuestraLosDatosPrincipales()
    {
        var (_, report) = SimulateAndReport(0, "Product DB");

        var text = report.ToText();

        Assert.Contains("NODO INICIAL DEL FALLO: Product DB", text);
        Assert.Contains("NODOS AFECTADOS: 5 de 8 (62.5% del sistema)", text);
        Assert.Contains("NIVEL DE IMPACTO: CRÍTICO", text);
        Assert.Contains("SPOFs DETECTADOS: 1", text);
        Assert.Contains("Cache Service (Service): desconectaría 1 nodo(s) [Storage]", text);
        Assert.Contains("RUTAS CRÍTICAS: 2", text);
        Assert.Contains("API Gateway → ", text);
    }

    [Fact]
    public void TextoDelReporte_NoMuestraIdentificadoresInternos()
    {
        var (graph, report) = SimulateAndReport(1, "Order DB");

        var text = report.ToText();

        Assert.All(graph.Graph.Vertices, v => Assert.DoesNotContain(v.Id, text));
    }

    [Fact]
    public void ReporteDeAnalisisDeSpof_NoIncluyeSeccionDeCascada()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(0);
        var spof = new SpofDetectionService(graph);
        spof.DetectSpofs();
        var report = new ReportService(graph).BuildReport(
            new SimulationResult { Type = SimulationType.SpofAnalysis },
            spof.AnalyzeAllImpacts(),
            new CriticalPathService(graph).FindCriticalPaths());

        var text = report.ToText();

        Assert.Equal(string.Empty, report.OriginName);
        Assert.DoesNotContain("NODO INICIAL DEL FALLO", text);
        Assert.Contains("SPOFs DETECTADOS: 1", text);
    }

    [Fact]
    public void Reporte_SeActualizaEnCadaSimulacion()
    {
        var (_, first)  = SimulateAndReport(0, "Product DB");
        var (_, second) = SimulateAndReport(0, "Load Balancer");

        Assert.NotEqual(first.AffectedCount, second.AffectedCount);
        Assert.Equal("Load Balancer", second.OriginName);
    }
}
