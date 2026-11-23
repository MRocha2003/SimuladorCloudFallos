using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Pages;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Integration;

/// <summary>
/// Pruebas de integración de la página Simulator con bUnit: la interfaz,
/// los servicios reales del simulador y un almacén en memoria en lugar del
/// localStorage. Cubren IT-1 (CRUD desde la interfaz), IT-2 (arrastre y
/// guardado) e IT-3 (simulación de cascada).
/// </summary>
public class SimulatorPageTests : TestContext
{
    private readonly GraphService _graph = new();
    private readonly InMemoryKeyValueStore _store = new();

    public SimulatorPageTests()
    {
        Services.AddSingleton(_graph);
        Services.AddSingleton<CascadeSimulationService>();
        Services.AddSingleton<SpofDetectionService>();
        Services.AddSingleton<IKeyValueStore>(_store);
        Services.AddSingleton<TopologyStorageService>();
        Services.AddSingleton<CriticalPathService>();   // IT-4
        Services.AddSingleton<ReportService>();         // IT-5
        Services.AddSingleton<AlertService>();          // IT-6
        JSInterop.Mode = JSRuntimeMode.Loose;   // navigator.clipboard en "Copiar reporte"
    }

    private IRenderedComponent<Simulator> Render() => RenderComponent<Simulator>();

    [Fact]
    public void AgregarNodo_DesdeLaInterfaz_ApareceEnElCanvas()
    {
        var cut = Render();

        cut.Find("#new-node-name").Change("API Gateway");
        cut.Find("#new-node-type").Change(NodeType.API.ToString());
        cut.Find("#btn-add-node").Click();

        Assert.Single(_graph.Graph.Vertices);
        Assert.Equal(NodeType.API, _graph.Graph.Vertices[0].NodeType);
        Assert.Single(cut.FindAll("g.graph-node"));
        Assert.Contains("API Gateway", cut.Find(".node-label").TextContent);
    }

    [Fact]
    public void AgregarNodo_SinNombre_NoCreaNada()
    {
        var cut = Render();

        cut.Find("#btn-add-node").Click();

        Assert.Empty(_graph.Graph.Vertices);
    }

    [Fact]
    public void Conectar_CreaAristaYLaDibuja()
    {
        var a = _graph.AddVertex("Web", NodeType.Service);
        var b = _graph.AddVertex("DB", NodeType.Database, 300, 300);
        var cut = Render();

        cut.Find("#edge-source").Change(a.Id);
        cut.Find("#edge-target").Change(b.Id);
        cut.Find("#btn-add-edge").Click();

        Assert.True(_graph.Graph.EdgeExists(a.Id, b.Id));
        Assert.Single(cut.FindAll("line.graph-edge"));
        Assert.Contains("Dependencia creada", cut.Find("#edge-message").TextContent);
    }

    [Fact]
    public void Conectar_Duplicada_MuestraMensajeYNoDuplica()
    {
        var a = _graph.AddVertex("Web", NodeType.Service);
        var b = _graph.AddVertex("DB", NodeType.Database, 300, 300);
        _graph.AddEdge(a.Id, b.Id);
        var cut = Render();

        cut.Find("#edge-source").Change(a.Id);
        cut.Find("#edge-target").Change(b.Id);
        cut.Find("#btn-add-edge").Click();

        Assert.Single(_graph.Graph.Edges);
        Assert.Contains("ya existe", cut.Find("#edge-message").TextContent);
    }

    [Fact]
    public void Conectar_ConsigoMismo_MuestraMensaje()
    {
        var a = _graph.AddVertex("Web", NodeType.Service);
        var cut = Render();

        cut.Find("#edge-source").Change(a.Id);
        cut.Find("#edge-target").Change(a.Id);
        cut.Find("#btn-add-edge").Click();

        Assert.Empty(_graph.Graph.Edges);
        Assert.Contains("sí mismo", cut.Find("#edge-message").TextContent);
    }

    [Fact]
    public void EditarNodoSeleccionado_ActualizaNombreYTipo()
    {
        var a = _graph.AddVertex("DB", NodeType.Service);
        var cut = Render();

        cut.Find($"g[data-node-id='{a.Id}']").Click();
        cut.Find("#edit-node-name").Change("Base de Datos Principal");
        cut.Find("#edit-node-type").Change(NodeType.Database.ToString());
        cut.Find("#btn-edit-node").Click();

        Assert.Equal("Base de Datos Principal", a.Name);
        Assert.Equal(NodeType.Database, a.NodeType);
    }

    [Fact]
    public void EliminarNodoSeleccionado_BorraNodoYAristas()
    {
        var a = _graph.AddVertex("Web", NodeType.Service);
        var b = _graph.AddVertex("DB", NodeType.Database, 300, 300);
        _graph.AddEdge(a.Id, b.Id);
        var cut = Render();

        cut.Find($"g[data-node-id='{b.Id}']").Click();
        cut.Find("#btn-delete-node").Click();

        Assert.Single(_graph.Graph.Vertices);
        Assert.Empty(_graph.Graph.Edges);
        Assert.Empty(cut.FindAll("line.graph-edge"));
        Assert.Empty(cut.FindAll("#selected-node-card"));
    }

    [Fact]
    public void EliminarArista_DesdeElListado()
    {
        var a = _graph.AddVertex("Web", NodeType.Service);
        var b = _graph.AddVertex("DB", NodeType.Database, 300, 300);
        _graph.AddEdge(a.Id, b.Id);
        var cut = Render();

        cut.Find("button.btn-delete-edge").Click();

        Assert.Empty(_graph.Graph.Edges);
    }

    [Fact]
    public async Task ArrastrarNodo_ActualizaSuPosicion()
    {
        var a = _graph.AddVertex("Web", NodeType.Service, 100, 100);
        var cut = Render();

        await cut.Find($"g[data-node-id='{a.Id}']")
                 .TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 500, ClientY = 400 });
        await cut.Find("#graph-canvas")
                 .TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 650, ClientY = 480 });
        await cut.Find("#graph-canvas")
                 .TriggerEventAsync("onpointerup", new PointerEventArgs());

        Assert.Equal(250, a.PositionX);
        Assert.Equal(180, a.PositionY);
    }

    [Fact]
    public void GuardarTopologia_LaPersisteEnElAlmacen()
    {
        _graph.LoadPredefinedTopology(0);
        var cut = Render();

        cut.Find("#topology-name").Change("Prod-v1");
        cut.Find("#btn-save-topology").Click();

        cut.WaitForAssertion(() => Assert.Contains("guardada", cut.Find("#save-message").TextContent));
        Assert.Contains("Prod-v1", _store.Data["simulador_topologies"]);
    }

    [Fact]
    public void SimularCascada_MuestraPanelConNodosAfectados()
    {
        _graph.LoadPredefinedTopology(0);
        var db = TestGraphs.ByName(_graph, "Product DB");
        var cut = Render();

        cut.Find("#sim-origin").Change(db.Id);
        cut.Find("#btn-simulate").Click();

        var panel = cut.Find("#results-panel");
        Assert.Contains("5 de 8", cut.Find("#affected-count").TextContent);
        Assert.Contains("Impacto crítico", panel.TextContent);
        Assert.Equal(5, cut.FindAll("#affected-list li").Count);
    }

    [Fact]
    public void DetectarSpofs_MuestraLaLista()
    {
        _graph.LoadPredefinedTopology(0);
        var cut = Render();

        cut.Find("#btn-spof").Click();

        Assert.Contains("Cache Service", cut.Find("#spof-list").TextContent);
    }

    [Fact]
    public void Reiniciar_OcultaResultadosYRestableceEstados()
    {
        _graph.LoadPredefinedTopology(0);
        var db = TestGraphs.ByName(_graph, "Product DB");
        var cut = Render();
        cut.Find("#sim-origin").Change(db.Id);
        cut.Find("#btn-simulate").Click();

        cut.Find("#btn-reset").Click();

        Assert.Empty(cut.FindAll("#results-panel"));
        Assert.All(_graph.Graph.Vertices, v => Assert.Equal(NodeStatus.Operative, v.Status));
    }

    [Fact]
    public void CargarTopologiaPequena_DibujaOchoNodosYDiezAristas()
    {
        var cut = Render();

        cut.Find("#btn-load-small").Click();

        Assert.Equal(8, cut.FindAll("g.graph-node").Count);
        Assert.Equal(10, cut.FindAll("line.graph-edge").Count);
    }

    [Fact]
    public void LimpiarTodo_VaciaElCanvas()
    {
        _graph.LoadPredefinedTopology(1);
        var cut = Render();

        cut.Find("#btn-clear").Click();
        cut.Find("#btn-clear-confirm").Click();   // IT-7 · HU-14: confirmación

        Assert.Empty(cut.FindAll("g.graph-node"));
    }

    [Fact]
    public void CopiarReporte_LlamaAlPortapapeles()
    {
        _graph.LoadPredefinedTopology(0);
        var db = TestGraphs.ByName(_graph, "Product DB");
        var cut = Render();
        cut.Find("#sim-origin").Change(db.Id);
        cut.Find("#btn-simulate").Click();

        cut.FindAll("#results-panel button").Last().Click();

        JSInterop.VerifyInvoke("navigator.clipboard.writeText");
    }
}
