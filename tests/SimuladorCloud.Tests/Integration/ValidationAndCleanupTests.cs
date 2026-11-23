using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SimuladorCloud.Client.Pages;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Integration;

/// <summary>
/// Pruebas de integración con bUnit de la iteración 7: página de validación
/// funcional (HU-13) y limpieza del entorno con confirmación (HU-14).
/// </summary>
public class ValidationAndCleanupTests : TestContext
{
    private readonly GraphService _graph = new();

    public ValidationAndCleanupTests()
    {
        Services.AddSingleton(_graph);
        Services.AddSingleton<CascadeSimulationService>();
        Services.AddSingleton<SpofDetectionService>();
        Services.AddSingleton<IKeyValueStore>(new InMemoryKeyValueStore());
        Services.AddSingleton<TopologyStorageService>();
        Services.AddSingleton<CriticalPathService>();
        Services.AddSingleton<ReportService>();
        Services.AddSingleton<AlertService>();
        Services.AddSingleton<AlgorithmValidationService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ── HU-13: validación funcional ─────────────────────────────────────────

    [Fact]
    public void Validacion_MuestraLasCuatroTopologiasSuperadas()
    {
        var cut = RenderComponent<Validation>();

        cut.Find("#btn-run-validation").Click();

        cut.WaitForAssertion(() => Assert.Equal(4, cut.FindAll("#validation-table tbody tr").Count), TimeSpan.FromSeconds(10));
        Assert.Contains("4 de 4 escenarios", cut.Find("#validation-summary").TextContent);
        Assert.Equal(4, cut.FindAll("#validation-table .badge.bg-success").Count);
    }

    [Fact]
    public void Validacion_IndicaElEntornoDeEjecucion()
    {
        var cut = RenderComponent<Validation>();

        Assert.Contains(".NET fuera del navegador", cut.Find("#validation-environment").TextContent);
        Assert.Empty(cut.FindAll("#validation-table"));   // aún no se ejecutó
    }

    // ── HU-14: limpieza y reinicio del entorno ─────────────────────────────

    [Fact]
    public void Limpiar_ConTopologiaActiva_PideConfirmacion()
    {
        _graph.LoadPredefinedTopology(0);
        var cut = RenderComponent<Simulator>();

        cut.Find("#btn-clear").Click();

        Assert.Contains("8 nodos", cut.Find("#clear-confirm").TextContent);
        Assert.Equal(8, cut.FindAll("g.graph-node").Count);   // todavía no se borró nada
    }

    [Fact]
    public void Limpiar_Cancelar_ConservaLaTopologia()
    {
        _graph.LoadPredefinedTopology(0);
        var cut = RenderComponent<Simulator>();
        cut.Find("#btn-clear").Click();

        cut.Find("#btn-clear-cancel").Click();

        Assert.Empty(cut.FindAll("#clear-confirm"));
        Assert.Equal(8, cut.FindAll("g.graph-node").Count);
    }

    [Fact]
    public void Limpiar_Confirmar_DejaElEntornoEnEstadoInicial()
    {
        _graph.LoadPredefinedTopology(0);
        var cut = RenderComponent<Simulator>();
        cut.Find("#sim-origin").Change(TestGraphs.ByName(_graph, "Product DB").Id);
        cut.Find("#btn-simulate").Click();

        cut.Find("#btn-clear").Click();
        cut.Find("#btn-clear-confirm").Click();

        Assert.Empty(cut.FindAll("g.graph-node"));
        Assert.Empty(cut.FindAll("line.graph-edge"));
        Assert.Empty(cut.FindAll("#results-panel"));
        Assert.Empty(cut.FindAll(".visual-alert"));
        Assert.Empty(_graph.Graph.Vertices);
        Assert.NotNull(cut.Find("#btn-clear"));   // mismo componente: sin recargar la página
    }

    [Fact]
    public void Limpiar_CanvasVacio_NoPideConfirmacion()
    {
        var cut = RenderComponent<Simulator>();

        cut.Find("#btn-clear").Click();

        Assert.Empty(cut.FindAll("#clear-confirm"));
    }
}
