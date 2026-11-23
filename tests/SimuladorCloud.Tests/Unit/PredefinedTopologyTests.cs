using System.Diagnostics;
using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias de los escenarios predefinidos grande (50 nodos) y
/// complejo (100 nodos) (IT-6 · HU-11). Los valores esperados de la cascada
/// se verificaron con una implementación independiente (NetworkX).
/// </summary>
public class PredefinedTopologyTests
{
    private static GraphService Load(int scenario)
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(scenario);
        return graph;
    }

    [Theory]
    [InlineData(2, 50, 72)]
    [InlineData(3, 100, 150)]
    public void Escenario_TieneLosNodosYAristasEsperados(int scenario, int nodes, int edges)
    {
        var graph = Load(scenario);

        Assert.Equal(nodes, graph.Graph.NodeCount);
        Assert.Equal(edges, graph.Graph.EdgeCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Escenario_NombresYPosicionesUnicos(int scenario)
    {
        var g = Load(scenario).Graph;

        Assert.Equal(g.NodeCount, g.Vertices.Select(v => v.Name).Distinct().Count());
        Assert.Equal(g.NodeCount, g.Vertices.Select(v => (v.PositionX, v.PositionY)).Distinct().Count());
        Assert.All(g.Vertices, v => Assert.True(v.PositionX >= 28 && v.PositionY >= 28));
        Assert.DoesNotContain(g.Edges, e => e.SourceId == e.TargetId);
    }

    [Theory]
    [InlineData(2, "Product DB", 8)]
    [InlineData(2, "Session Cache", 9)]
    [InlineData(2, "Event Bus", 7)]
    [InlineData(3, "Identity Provider", 11)]
    [InlineData(3, "Disaster Recovery Storage", 19)]
    [InlineData(3, "Global Billing DB", 12)]
    public void Cascada_CoincideConLaVerificacionManual(int scenario, string origin, int expectedAffected)
    {
        var graph = Load(scenario);

        var result = new CascadeSimulationService(graph).SimulateCascade(TestGraphs.ByName(graph, origin).Id);

        Assert.Equal(expectedAffected, result.AffectedCount);
    }

    [Fact]
    public void Cascada_TopologiaCompleja_EsReproducibleEnTresEjecuciones()
    {
        var graph = Load(3);
        var sim = new CascadeSimulationService(graph);
        var origin = TestGraphs.ByName(graph, "Disaster Recovery Storage").Id;

        var runs = Enumerable.Range(0, 3)
            .Select(_ => string.Join(",", sim.SimulateCascade(origin).AffectedNodes.OrderBy(x => x)))
            .ToList();

        Assert.Single(runs.Distinct());
    }

    [Fact]
    public void Cascada_TopologiaDe100Nodos_TardaMenosDe120ms()
    {
        var graph = Load(3);
        var sim = new CascadeSimulationService(graph);
        var origin = TestGraphs.ByName(graph, "Disaster Recovery Storage").Id;
        sim.SimulateCascade(origin);   // calentamiento (compilación JIT)

        var sw = Stopwatch.StartNew();
        sim.SimulateCascade(origin);
        sw.Stop();

        Assert.True(sw.Elapsed.TotalMilliseconds < 120, $"Tardó {sw.Elapsed.TotalMilliseconds:F1} ms");
    }

    [Fact]
    public void IndiceDesconocido_CargaLaTopologiaPequena()
    {
        Assert.Equal(8, Load(99).Graph.NodeCount);
    }

    [Fact]
    public void DependientesTransitivos_NoModificanElGrafo()
    {
        var graph = Load(0);
        var db = TestGraphs.ByName(graph, "Product DB");

        var affected = graph.Graph.GetTransitiveDependents(db.Id);

        Assert.Equal(5, affected.Count);
        Assert.DoesNotContain(db.Id, affected);
        Assert.All(graph.Graph.Vertices, v => Assert.Equal(NodeStatus.Operative, v.Status));
    }
}
