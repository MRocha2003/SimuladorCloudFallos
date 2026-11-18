using System.Diagnostics;
using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias de la detección de SPOF sobre los escenarios predefinidos
/// y del análisis de impacto de cada SPOF (IT-4, HU-07 y HU-08).
/// Los resultados esperados se verificaron con una implementación
/// independiente de puntos de articulación (biblioteca NetworkX).
/// </summary>
public class SpofImpactTests
{
    private static GraphService Load(int scenario)
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(scenario);
        return graph;
    }

    [Fact]
    public void TopologiaMediana_SpofsCoincidenConLaVerificacionManual()
    {
        var graph = Load(1);

        var spofs = new SpofDetectionService(graph).DetectSpofs().Select(v => v.Name).OrderBy(n => n);

        Assert.Equal(new[] { "Load Balancer B", "Order Service", "Payment Service" }, spofs);
    }

    [Fact]
    public void TopologiaGrande_Detecta14Spofs()
    {
        var graph = Load(2);

        var spofs = new SpofDetectionService(graph).DetectSpofs().Select(v => v.Name).OrderBy(n => n).ToList();

        Assert.Equal(new[]
        {
            "Analytics Service", "Auth Service", "Cart Service", "Email Queue", "Inventory Service",
            "Log Service", "Notification Service", "Order Service", "Payment Service", "Product Service",
            "Promotion Service", "Review Service", "Shipping Service", "User Service"
        }, spofs);
    }

    [Fact]
    public void TopologiaCompleja_Detecta33Spofs()
    {
        var graph = Load(3);

        var spofs = new SpofDetectionService(graph).DetectSpofs().Select(v => v.Name).ToList();

        Assert.Equal(33, spofs.Count);
        Assert.Contains("Global DNS", spofs);
        Assert.Contains("Global API Gateway", spofs);
        Assert.Contains("Global Monitoring", spofs);
        Assert.Contains("Load Balancer (R2)", spofs);
    }

    [Fact]
    public void Impacto_CacheService_DesconectaStorageYAfectaADosNodos()
    {
        var graph = Load(0);
        var cache = TestGraphs.ByName(graph, "Cache Service");

        var impact = new SpofDetectionService(graph).AnalyzeImpact(cache.Id);

        Assert.Equal("Cache Service", impact.VertexName);
        Assert.Equal(new[] { "Storage" }, impact.DisconnectedNodes.Select(id => graph.Graph.GetVertex(id)!.Name));
        Assert.Equal(new[] { "API Gateway", "Product Service" }, impact.CascadeAffected.Select(id => graph.Graph.GetVertex(id)!.Name));
        Assert.Equal(2, impact.ComponentsAfterRemoval);
        Assert.Equal(3, impact.CriticalityScore);
    }

    [Fact]
    public void Impacto_NodoCentralDeCadena_DivideEnDosPartes()
    {
        var (graph, a, b, c) = TestGraphs.Chain();

        var impact = new SpofDetectionService(graph).AnalyzeImpact(b.Id);

        Assert.Equal(2, impact.ComponentsAfterRemoval);
        Assert.Single(impact.DisconnectedNodes);
        Assert.Equal(new[] { a.Id }, impact.CascadeAffected);
    }

    [Fact]
    public void Impacto_NoModificaElEstadoDelGrafo()
    {
        var graph = Load(0);
        var db = TestGraphs.ByName(graph, "Product DB");

        new SpofDetectionService(graph).AnalyzeImpact(db.Id);

        Assert.All(graph.Graph.Vertices, v => Assert.Equal(NodeStatus.Operative, v.Status));
        Assert.Empty(graph.Graph.FailedNodes);
    }

    [Fact]
    public void Impacto_NodoInexistente_DevuelveImpactoVacio()
    {
        var impact = new SpofDetectionService(new GraphService()).AnalyzeImpact("no-existe");

        Assert.Empty(impact.DisconnectedNodes);
        Assert.Empty(impact.CascadeAffected);
    }

    [Fact]
    public void Impacto_GrafoConVariosComponentes_CuentaTodosLosComponentes()
    {
        var (graph, _, b, _) = TestGraphs.Chain();
        var x = graph.AddVertex("X", NodeType.Service);
        var y = graph.AddVertex("Y", NodeType.Service);
        graph.AddEdge(x.Id, y.Id);   // componente independiente

        var impact = new SpofDetectionService(graph).AnalyzeImpact(b.Id);

        Assert.Equal(3, impact.ComponentsAfterRemoval);   // {A}, {C} y {X, Y}
    }

    [Fact]
    public void TodosLosImpactos_SeOrdenanPorCriticidad()
    {
        var graph = Load(1);

        var impacts = new SpofDetectionService(graph).AnalyzeAllImpacts();

        Assert.Equal(new[] { "Load Balancer B", "Order Service", "Payment Service" }, impacts.Select(i => i.VertexName));
        Assert.Equal(9, impacts[0].CriticalityScore);
        Assert.True(impacts.Zip(impacts.Skip(1)).All(p => p.First.CriticalityScore >= p.Second.CriticalityScore));
    }

    [Fact]
    public void TodosLosImpactos_SinSpofs_DevuelveListaVacia()
    {
        var graph = new GraphService();
        var a = graph.AddVertex("A", NodeType.Service);
        var b = graph.AddVertex("B", NodeType.Service);
        var c = graph.AddVertex("C", NodeType.Service);
        graph.AddEdge(a.Id, b.Id);
        graph.AddEdge(b.Id, c.Id);
        graph.AddEdge(c.Id, a.Id);

        Assert.Empty(new SpofDetectionService(graph).AnalyzeAllImpacts());
    }

    [Fact]
    public void DeteccionDeSpofs_TopologiaDe100Nodos_TardaMenosDe120ms()
    {
        var graph = Load(3);
        var service = new SpofDetectionService(graph);
        service.DetectSpofs();                         // calentamiento (compilación JIT)

        var sw = Stopwatch.StartNew();
        service.DetectSpofs();
        service.AnalyzeAllImpacts();
        sw.Stop();

        Assert.True(sw.Elapsed.TotalMilliseconds < 120, $"Tardó {sw.Elapsed.TotalMilliseconds:F1} ms");
    }
}
