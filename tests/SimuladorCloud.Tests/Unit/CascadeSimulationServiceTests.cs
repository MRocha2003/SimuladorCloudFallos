using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias del motor de propagación en cascada (IT-3, HU-05 y HU-06).
/// Convención del modelo: la arista e(u,v) indica que u depende de v,
/// por lo que el fallo de v se propaga a u.
/// </summary>
public class CascadeSimulationServiceTests
{
    [Fact]
    public void FalloEnLaBase_SePropagaATodosLosQueDependen()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        var sim = new CascadeSimulationService(graph);

        var result = sim.SimulateCascade(c.Id);

        Assert.Equal(NodeStatus.Failed, c.Status);
        Assert.Equal(NodeStatus.Failed, b.Status);
        Assert.Equal(NodeStatus.Failed, a.Status);
        Assert.Equal(2, result.AffectedCount);          // el origen no se cuenta como afectado
        Assert.Equal(66.67, result.AffectedPercent, 2);
        Assert.All(graph.Graph.Edges, e => Assert.True(e.IsCascadePath));
    }

    [Fact]
    public void FalloEnElNodoSuperior_NoAfectaASusDependencias()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        var sim = new CascadeSimulationService(graph);

        var result = sim.SimulateCascade(a.Id);

        Assert.Equal(NodeStatus.Failed, a.Status);
        Assert.Equal(NodeStatus.Operative, b.Status);
        Assert.Equal(NodeStatus.Operative, c.Status);
        Assert.Equal(0, result.AffectedCount);
        Assert.False(result.IsCriticalImpact);
    }

    [Fact]
    public void OrigenInexistente_DevuelveResultadoVacio()
    {
        var sim = new CascadeSimulationService(new GraphService());

        var result = sim.SimulateCascade("no-existe");

        Assert.Equal("no-existe", result.FailureOrigin);
        Assert.Empty(result.AffectedNodes);
    }

    [Fact]
    public void TopologiaPequena_FalloEnLoadBalancer_Afecta3Nodos()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(0);
        var sim = new CascadeSimulationService(graph);
        var lb = TestGraphs.ByName(graph, "Load Balancer");

        var result = sim.SimulateCascade(lb.Id);

        var afectados = result.AffectedNodes.Select(id => graph.Graph.GetVertex(id)!.Name).OrderBy(n => n);
        Assert.Equal(new[] { "API Gateway", "Auth Service", "Product Service" }, afectados);
        Assert.Equal(37.5, result.AffectedPercent, 1);
        Assert.Equal(NodeStatus.Operative, TestGraphs.ByName(graph, "User DB").Status);
    }

    [Fact]
    public void TopologiaPequena_FalloEnProductDB_EsImpactoCritico()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(0);
        var sim = new CascadeSimulationService(graph);
        var db = TestGraphs.ByName(graph, "Product DB");

        var result = sim.SimulateCascade(db.Id);

        Assert.Equal(5, result.AffectedCount);
        Assert.Equal(62.5, result.AffectedPercent, 1);
        Assert.True(result.IsCriticalImpact);
        Assert.Equal(NodeStatus.Operative, TestGraphs.ByName(graph, "User DB").Status);
        Assert.Equal(NodeStatus.Operative, TestGraphs.ByName(graph, "Storage").Status);
    }

    [Fact]
    public void SimulacionesSucesivas_SonReproducibles()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(1);
        var sim = new CascadeSimulationService(graph);
        var origen = TestGraphs.ByName(graph, "User DB");

        var r1 = sim.SimulateCascade(origen.Id).AffectedNodes.OrderBy(x => x).ToList();
        var r2 = sim.SimulateCascade(origen.Id).AffectedNodes.OrderBy(x => x).ToList();

        Assert.Equal(r1, r2);
    }

    [Fact]
    public void NuevaSimulacion_ReiniciaElEstadoAnterior()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        var sim = new CascadeSimulationService(graph);
        sim.SimulateCascade(c.Id);

        sim.SimulateCascade(a.Id);

        Assert.Equal(NodeStatus.Operative, c.Status);
        Assert.Equal(NodeStatus.Operative, b.Status);
        Assert.DoesNotContain(graph.Graph.Edges, e => e.IsCascadePath);
    }

    [Fact]
    public void AristaInactiva_NoPropagaElFallo()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        graph.Graph.Edges.Single(e => e.SourceId == b.Id).IsActive = false; // B→C inactiva
        var sim = new CascadeSimulationService(graph);

        var result = sim.SimulateCascade(c.Id);

        Assert.Equal(0, result.AffectedCount);
        Assert.Equal(NodeStatus.Operative, b.Status);
    }

    [Fact]
    public void GrafoConCiclo_TerminaYMarcaCadaNodoUnaVez()
    {
        var graph = new GraphService();
        var a = graph.AddVertex("A", NodeType.Service);
        var b = graph.AddVertex("B", NodeType.Service);
        graph.AddEdge(a.Id, b.Id);
        graph.AddEdge(b.Id, a.Id);
        var sim = new CascadeSimulationService(graph);

        var result = sim.SimulateCascade(a.Id);

        Assert.Equal(new[] { b.Id }, result.AffectedNodes);
    }

    [Fact]
    public void InjectFault_MarcaElNodoSinPropagar()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        var sim = new CascadeSimulationService(graph);

        sim.InjectFault(c.Id);
        sim.InjectFault("no-existe");

        Assert.Equal(NodeStatus.Failed, c.Status);
        Assert.Equal(NodeStatus.Operative, b.Status);
        Assert.Contains(c.Id, graph.Graph.FailedNodes);
    }

    [Fact]
    public void ResetSimulation_DevuelveTodosLosNodosAOperativo()
    {
        var (graph, a, b, c) = TestGraphs.Chain();
        new CascadeSimulationService(graph).SimulateCascade(c.Id);

        graph.ResetSimulation();

        Assert.All(graph.Graph.Vertices, v => Assert.Equal(NodeStatus.Operative, v.Status));
        Assert.Empty(graph.Graph.FailedNodes);
    }

    [Fact]
    public void Resumen_IncluyeNodosAfectados()
    {
        var (graph, _, _, c) = TestGraphs.Chain();
        var result = new CascadeSimulationService(graph).SimulateCascade(c.Id);

        var resumen = result.GenerateSummary();

        Assert.Contains("NODOS AFECTADOS: 2", resumen);
    }
}
