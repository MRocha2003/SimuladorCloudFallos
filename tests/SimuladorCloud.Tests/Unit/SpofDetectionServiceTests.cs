using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias de la detección de SPOF por puntos de articulación
/// (adelanto de IT-4; se incluyen porque el servicio ya forma parte del código).
/// </summary>
public class SpofDetectionServiceTests
{
    [Fact]
    public void Cadena_ElNodoCentralEsSpof()
    {
        var (graph, _, b, _) = TestGraphs.Chain();

        var spofs = new SpofDetectionService(graph).DetectSpofs();

        Assert.Equal(new[] { b.Id }, spofs.Select(v => v.Id));
        Assert.True(b.IsSpof);
    }

    [Fact]
    public void Triangulo_NoTieneSpof()
    {
        var graph = new GraphService();
        var a = graph.AddVertex("A", NodeType.Service);
        var b = graph.AddVertex("B", NodeType.Service);
        var c = graph.AddVertex("C", NodeType.Service);
        graph.AddEdge(a.Id, b.Id);
        graph.AddEdge(b.Id, c.Id);
        graph.AddEdge(c.Id, a.Id);

        Assert.Empty(new SpofDetectionService(graph).DetectSpofs());
    }

    [Fact]
    public void Estrella_ElCentroEsSpof()
    {
        var graph = new GraphService();
        var centro = graph.AddVertex("Load Balancer", NodeType.LoadBalancer);
        for (int i = 0; i < 3; i++)
        {
            var s = graph.AddVertex($"S{i}", NodeType.Service);
            graph.AddEdge(s.Id, centro.Id);
        }

        var spofs = new SpofDetectionService(graph).DetectSpofs();

        Assert.Equal(new[] { centro.Id }, spofs.Select(v => v.Id));
    }

    [Fact]
    public void TopologiaPequena_DetectaCacheServiceComoUnicoSpof()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(0);

        var spofs = new SpofDetectionService(graph).DetectSpofs();

        // Storage solo se conecta a través de Cache Service
        Assert.Equal(new[] { "Cache Service" }, spofs.Select(v => v.Name));
    }

    [Fact]
    public void AnalisisRepetido_NoAcumulaResultadosAnteriores()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        var service = new SpofDetectionService(graph);
        service.DetectSpofs();

        graph.AddEdge(c.Id, a.Id);   // cierra el ciclo: ya no hay SPOF
        var spofs = service.DetectSpofs();

        Assert.Empty(spofs);
        Assert.False(b.IsSpof);
        Assert.Empty(graph.Graph.Spofs);
    }

    [Fact]
    public void GrafoVacio_DevuelveListaVacia()
    {
        Assert.Empty(new SpofDetectionService(new GraphService()).DetectSpofs());
    }
}
