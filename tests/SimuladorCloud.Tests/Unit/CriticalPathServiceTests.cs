using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias de la identificación de rutas críticas (IT-4 · HU-08;
/// IT-5 · HU-10: longitud de las rutas).
/// </summary>
public class CriticalPathServiceTests
{
    private static string Names(GraphService g, CriticalPath p) =>
        string.Join(">", p.NodeIds.Select(id => g.Graph.GetVertex(id)!.Name));

    [Fact]
    public void Cadena_DevuelveLaRutaCompletaYMarcaSusAristas()
    {
        var (graph, a, b, c) = TestGraphs.Chain();

        var paths = new CriticalPathService(graph).FindCriticalPaths();

        var path = Assert.Single(paths);
        Assert.Equal(new[] { a.Id, b.Id, c.Id }, path.NodeIds);
        Assert.Equal(2, path.Length);
        Assert.Equal(2.0, path.TotalWeight, 2);
        Assert.All(graph.Graph.Edges, e => Assert.True(e.IsCriticalPath));
        Assert.Single(graph.Graph.CriticalPaths);
    }

    [Fact]
    public void GrafoVacio_DevuelveListaVacia()
    {
        Assert.Empty(new CriticalPathService(new GraphService()).FindCriticalPaths());
    }

    [Fact]
    public void RutaQueAtraviesaUnSpof_TienePrioridadSobreUnaMasLarga()
    {
        var graph = new GraphService();
        var e = graph.AddVertex("Entrada", NodeType.API);
        var x = graph.AddVertex("X", NodeType.Service);
        var y = graph.AddVertex("Y", NodeType.Database);
        var p = graph.AddVertex("P", NodeType.Service);
        var q = graph.AddVertex("Q", NodeType.Service);
        var r = graph.AddVertex("R", NodeType.Database);
        graph.AddEdge(e.Id, x.Id); graph.AddEdge(x.Id, y.Id);
        graph.AddEdge(e.Id, p.Id); graph.AddEdge(p.Id, q.Id); graph.AddEdge(q.Id, r.Id);
        x.IsSpof = true;

        var paths = new CriticalPathService(graph).FindCriticalPaths();

        Assert.Equal(new[] { "Entrada>X>Y", "Entrada>P>Q>R" }, paths.Select(path => Names(graph, path)));
        Assert.Equal(1, paths[0].SpofCount);
        Assert.Equal(3, paths[1].Length);
    }

    [Fact]
    public void GrafoConCiclo_TerminaYDevuelveRutasValidas()
    {
        var graph = new GraphService();
        var a = graph.AddVertex("A", NodeType.Service);
        var b = graph.AddVertex("B", NodeType.Service);
        var c = graph.AddVertex("C", NodeType.Service);
        var d = graph.AddVertex("D", NodeType.Database);
        var entry = graph.AddVertex("Entrada", NodeType.API);
        graph.AddEdge(entry.Id, a.Id);
        graph.AddEdge(a.Id, b.Id); graph.AddEdge(b.Id, c.Id); graph.AddEdge(c.Id, a.Id);
        graph.AddEdge(c.Id, d.Id);

        var paths = new CriticalPathService(graph).FindCriticalPaths();

        Assert.NotEmpty(paths);
        foreach (var path in paths)
        {
            Assert.Equal(path.NodeIds.Count, path.NodeIds.Distinct().Count());   // sin repetir nodos
            foreach (var (u, v) in path.NodeIds.Zip(path.NodeIds.Skip(1)))
                Assert.True(graph.Graph.EdgeExists(u, v));
        }
        Assert.Equal("Entrada>A>B>C>D", Names(graph, paths[0]));
    }

    [Fact]
    public void AristaInactiva_NoFormaParteDeLasRutas()
    {
        var (graph, a, b, _) = TestGraphs.Chain();
        graph.Graph.Edges.Single(e => e.SourceId == b.Id).IsActive = false;   // B→C inactiva

        var path = Assert.Single(new CriticalPathService(graph).FindCriticalPaths());

        Assert.Equal(new[] { a.Id, b.Id }, path.NodeIds);
    }

    [Fact]
    public void LimiteDeRutas_SeRespeta()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(1);

        Assert.Single(new CriticalPathService(graph).FindCriticalPaths(maxPaths: 1));
        Assert.Empty(new CriticalPathService(graph).FindCriticalPaths(maxPaths: 0));
    }

    [Fact]
    public void TopologiaGrande_RutaPrincipalTieneLongitudMaxima()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(2);
        new SpofDetectionService(graph).DetectSpofs();

        var paths = new CriticalPathService(graph).FindCriticalPaths();

        Assert.Equal(3, paths.Count);
        Assert.Equal(9, paths[0].Length);                       // camino más largo del grafo
        Assert.Equal(5, paths[0].SpofCount);
        Assert.StartsWith("API Gateway>", Names(graph, paths[0]));
        Assert.EndsWith(">Email Service", Names(graph, paths[0]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RutasDeLosEscenarios_VanDeUnaEntradaAUnaDependenciaFinal(int scenario)
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(scenario);
        var g = graph.Graph;

        var paths = new CriticalPathService(graph).FindCriticalPaths();

        Assert.NotEmpty(paths);
        foreach (var path in paths)
        {
            Assert.DoesNotContain(g.Edges, e => e.TargetId == path.NodeIds[0]);    // nadie depende de la entrada
            Assert.DoesNotContain(g.Edges, e => e.SourceId == path.NodeIds[^1]);   // el final no depende de nadie
            foreach (var (u, v) in path.NodeIds.Zip(path.NodeIds.Skip(1)))
                Assert.True(g.EdgeExists(u, v));
        }
    }

    [Fact]
    public void ReiniciarSimulacion_LimpiaLasRutasCriticas()
    {
        var (graph, _, _, _) = TestGraphs.Chain();
        new CriticalPathService(graph).FindCriticalPaths();

        graph.ResetSimulation();

        Assert.DoesNotContain(graph.Graph.Edges, e => e.IsCriticalPath);
        Assert.Empty(graph.Graph.CriticalPaths);
    }

    [Fact]
    public void AristaDeRutaCritica_UsaColorYMarcadorPropios()
    {
        var edge = new Edge { IsCriticalPath = true };

        Assert.Equal("#805AD5", edge.StrokeColor);
        Assert.Equal("arrow-purple", edge.MarkerId);

        edge.IsCascadePath = true;   // la cascada tiene prioridad visual
        Assert.Equal("#E53E3E", edge.StrokeColor);
    }
}
