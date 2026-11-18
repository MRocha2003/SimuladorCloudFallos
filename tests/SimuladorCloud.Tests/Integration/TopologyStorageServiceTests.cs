using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Integration;

/// <summary>
/// Pruebas de integración de la persistencia (IT-2, HU-04): GraphService +
/// TopologyStorageService + serialización JSON sobre un almacén clave–valor
/// en memoria que sustituye al localStorage del navegador.
/// </summary>
public class TopologyStorageServiceTests
{
    private readonly InMemoryKeyValueStore _store = new();
    private readonly TopologyStorageService _storage;

    public TopologyStorageServiceTests()
    {
        _storage = new TopologyStorageService(_store);
    }

    [Fact]
    public async Task ListaVacia_CuandoNoHayNadaGuardado()
    {
        Assert.Empty(await _storage.GetTopologyListAsync());
    }

    [Fact]
    public async Task GuardarYCargar_ConservaNodosAristasYPosiciones()
    {
        var graph = new GraphService();
        graph.LoadPredefinedTopology(0);
        graph.Graph.Name = "Prod-v1";
        var gateway = TestGraphs.ByName(graph, "API Gateway");
        graph.UpdateVertexPosition(gateway.Id, 333, 222);

        await _storage.SaveTopologyAsync(graph.Graph);
        var cargada = await _storage.LoadTopologyAsync(graph.Graph.TopologyId);

        Assert.NotNull(cargada);
        Assert.Equal("Prod-v1", cargada!.Name);
        Assert.Equal(8, cargada.NodeCount);
        Assert.Equal(10, cargada.EdgeCount);
        var gw = cargada.Vertices.Single(v => v.Name == "API Gateway");
        Assert.Equal(333, gw.PositionX);
        Assert.Equal(222, gw.PositionY);
        Assert.Equal(NodeType.API, gw.NodeType);
    }

    [Fact]
    public async Task ElContenidoSeGuardaComoJsonDeLista()
    {
        var graph = new GraphService();
        graph.AddVertex("A", NodeType.Service);

        await _storage.SaveTopologyAsync(graph.Graph);

        var raw = _store.Data["simulador_topologies"];
        Assert.StartsWith("[", raw);   // JSON de una lista, no un texto doblemente serializado
    }

    [Fact]
    public async Task GuardarDosVeces_ReemplazaLaMismaTopologia()
    {
        var graph = new GraphService();
        graph.AddVertex("A", NodeType.Service);
        await _storage.SaveTopologyAsync(graph.Graph);

        graph.AddVertex("B", NodeType.Service);
        await _storage.SaveTopologyAsync(graph.Graph);

        var lista = await _storage.GetTopologyListAsync();
        Assert.Single(lista);
        Assert.Equal(2, lista[0].NodeCount);
    }

    [Fact]
    public async Task VariasTopologias_SeListanTodas()
    {
        var g1 = new GraphModel { Name = "Uno" };
        var g2 = new GraphModel { Name = "Dos" };

        await _storage.SaveTopologyAsync(g1);
        await _storage.SaveTopologyAsync(g2);

        var nombres = (await _storage.GetTopologyListAsync()).Select(t => t.Name).OrderBy(n => n);
        Assert.Equal(new[] { "Dos", "Uno" }, nombres);
    }

    [Fact]
    public async Task Eliminar_QuitaSoloLaTopologiaIndicada()
    {
        var g1 = new GraphModel { Name = "Uno" };
        var g2 = new GraphModel { Name = "Dos" };
        await _storage.SaveTopologyAsync(g1);
        await _storage.SaveTopologyAsync(g2);

        Assert.True(await _storage.DeleteTopologyAsync(g1.TopologyId));
        Assert.False(await _storage.DeleteTopologyAsync("no-existe"));

        var lista = await _storage.GetTopologyListAsync();
        Assert.Equal("Dos", Assert.Single(lista).Name);
    }

    [Fact]
    public async Task CargarTopologiaGuardada_EnGraphService_PermiteSimular()
    {
        var original = new GraphService();
        original.LoadPredefinedTopology(0);
        await _storage.SaveTopologyAsync(original.Graph);

        var nuevaSesion = new GraphService();
        nuevaSesion.LoadGraph((await _storage.LoadTopologyAsync(original.Graph.TopologyId))!);
        var db = TestGraphs.ByName(nuevaSesion, "Product DB");
        var result = new CascadeSimulationService(nuevaSesion).SimulateCascade(db.Id);

        Assert.Equal(5, result.AffectedCount);
    }

    [Fact]
    public async Task JsonCorrupto_DevuelveListaVacia()
    {
        _store.Data["simulador_topologies"] = "{esto no es json";

        Assert.Empty(await _storage.GetTopologyListAsync());
    }

    [Fact]
    public async Task Resultados_SeConservanLosUltimos50()
    {
        for (int i = 0; i < 55; i++)
            await _storage.SaveResultAsync(new SimulationResult { Timestamp = DateTime.UtcNow.AddMinutes(i) });

        Assert.Equal(50, (await _storage.GetResultListAsync()).Count);

        await _storage.ClearResultsAsync();
        Assert.Empty(await _storage.GetResultListAsync());
    }
}
