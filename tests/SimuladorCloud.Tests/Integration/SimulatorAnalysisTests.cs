using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SimuladorCloud.Client.Pages;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Integration;

/// <summary>
/// Pruebas de integración de la página Simulator con bUnit para las
/// iteraciones 4 a 6: visualización de SPOF y rutas críticas (HU-07, HU-08),
/// reporte y panel de resultados (HU-09, HU-10), escenarios grandes y
/// alertas visuales (HU-11, HU-12).
/// </summary>
public class SimulatorAnalysisTests : TestContext
{
    private readonly GraphService _graph = new();

    public SimulatorAnalysisTests()
    {
        Services.AddSingleton(_graph);
        Services.AddSingleton<CascadeSimulationService>();
        Services.AddSingleton<SpofDetectionService>();
        Services.AddSingleton<IKeyValueStore>(new InMemoryKeyValueStore());
        Services.AddSingleton<TopologyStorageService>();
        Services.AddSingleton<CriticalPathService>();
        Services.AddSingleton<ReportService>();
        Services.AddSingleton<AlertService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Simulator> RenderSmall()
    {
        _graph.LoadPredefinedTopology(0);
        return RenderComponent<Simulator>();
    }

    private static void Simulate(IRenderedComponent<Simulator> cut, string originId)
    {
        cut.Find("#sim-origin").Change(originId);
        cut.Find("#btn-simulate").Click();
    }

    // ── IT-4 · HU-07 / HU-08 ────────────────────────────────────────────────

    [Fact]
    public void AnalizarSpofs_MarcaElSpofYResaltaLasRutasCriticas()
    {
        var cut = RenderSmall();

        cut.Find("#btn-spof").Click();

        Assert.Single(cut.FindAll("circle.spof-marker"));
        Assert.NotEmpty(cut.FindAll("line.critical-edge"));
        Assert.Contains("Cache Service", cut.Find("#spof-list").TextContent);
        Assert.Equal(2, cut.FindAll("#critical-paths li").Count);
    }

    [Fact]
    public void ClicEnUnSpof_MuestraSuImpacto()
    {
        var cut = RenderSmall();
        var cache = TestGraphs.ByName(_graph, "Cache Service");
        cut.Find("#btn-spof").Click();

        cut.Find($"g[data-node-id='{cache.Id}']").Click();

        var info = cut.Find("#spof-impact").TextContent;
        Assert.Contains("desconectaría 1 nodo(s)", info);
        Assert.Contains("Storage", info);
        Assert.Contains("afectaría a 2 nodo(s)", info);
    }

    [Fact]
    public void Leyenda_ExplicaLosMarcadores()
    {
        var cut = RenderSmall();

        var legend = cut.Find("#legend").TextContent;

        Assert.Contains("SPOF", legend);
        Assert.Contains("Ruta crítica", legend);
        Assert.Contains("Propagación de la cascada", legend);
    }

    [Fact]
    public void Visualizacion_SeActualizaTrasCadaAnalisis()
    {
        var cut = RenderSmall();
        cut.Find("#btn-spof").Click();
        Assert.Single(cut.FindAll("circle.spof-marker"));

        cut.Find("#btn-load-medium").Click();
        Assert.Empty(cut.FindAll("circle.spof-marker"));   // al cambiar de topología se limpia

        cut.Find("#btn-spof").Click();
        Assert.Equal(3, cut.FindAll("circle.spof-marker").Count);
    }

    // ── IT-5 · HU-09 / HU-10 ────────────────────────────────────────────────

    [Fact]
    public void SimularCascada_GeneraReporteCompleto()
    {
        var cut = RenderSmall();

        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        var report = cut.Find("#impact-report").TextContent;
        Assert.Contains("NODO INICIAL DEL FALLO: Product DB", report);
        Assert.Contains("NODOS AFECTADOS: 5 de 8", report);
        Assert.Contains("SPOFs DETECTADOS: 1", report);
        Assert.Contains("RUTAS CRÍTICAS: 2", report);
    }

    [Fact]
    public void Panel_MuestraContadoresSpofsYRutasConLongitud()
    {
        var cut = RenderSmall();

        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        Assert.Equal("2", cut.Find("#count-operative").TextContent.Trim());
        Assert.Equal("0", cut.Find("#count-degraded").TextContent.Trim());
        Assert.Equal("6", cut.Find("#count-failed").TextContent.Trim());
        Assert.Contains("Cache Service", cut.Find("#spof-list").TextContent);
        Assert.Contains("(Service)", cut.Find("#spof-list").TextContent);
        Assert.Contains("longitud 3", cut.Find("#critical-paths").TextContent);
    }

    [Fact]
    public void Panel_SeColapsaYExpandeSinPerderDatos()
    {
        var cut = RenderSmall();
        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        cut.Find("#btn-toggle-panel").Click();
        Assert.Contains("collapsed", cut.Find("#results-panel").ClassList);
        Assert.Empty(cut.FindAll("#affected-list"));

        cut.Find("#btn-toggle-panel").Click();
        Assert.DoesNotContain("collapsed", cut.Find("#results-panel").ClassList);
        Assert.Contains("5 de 8", cut.Find("#affected-count").TextContent);
    }

    [Fact]
    public void CopiarReporte_EnviaElTextoDelReporte()
    {
        var cut = RenderSmall();
        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        cut.FindAll("#results-panel button").Last().Click();

        var invocation = JSInterop.VerifyInvoke("navigator.clipboard.writeText");
        Assert.Contains("NODO INICIAL DEL FALLO: Product DB", invocation.Arguments[0]?.ToString());
    }

    // ── IT-6 · HU-11 / HU-12 ────────────────────────────────────────────────

    [Fact]
    public void CargarEscenarioGrande_DibujaCincuentaNodos()
    {
        var cut = RenderComponent<Simulator>();

        cut.Find("#btn-load-large").Click();

        Assert.Equal(50, cut.FindAll("g.graph-node").Count);
        Assert.Equal(72, cut.FindAll("line.graph-edge").Count);
    }

    [Fact]
    public void CargarEscenarioComplejo_DibujaCienNodosYAmpliaElCanvas()
    {
        var cut = RenderComponent<Simulator>();

        cut.Find("#btn-load-complex").Click();

        Assert.Equal(100, cut.FindAll("g.graph-node").Count);
        Assert.True(double.Parse(cut.Find("#graph-canvas").GetAttribute("height")!,
                                 System.Globalization.CultureInfo.InvariantCulture) > 600);
    }

    [Fact]
    public void CascadaCriticaYSpof_MuestranAlertas()
    {
        var cut = RenderSmall();

        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        var stack = cut.Find("#alert-stack").TextContent;
        Assert.Contains("Cascada crítica", stack);
        Assert.Contains("SPOF detectado: Cache Service", stack);
        Assert.Equal(2, cut.FindAll(".visual-alert").Count);
    }

    [Fact]
    public void Alerta_SeDescartaSinInterrumpirLaSimulacion()
    {
        var cut = RenderSmall();
        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        cut.FindAll(".btn-dismiss-alert")[0].Click();

        Assert.Single(cut.FindAll(".visual-alert"));
        Assert.NotNull(cut.Find("#results-panel"));            // los resultados se mantienen
        Assert.Equal(8, cut.FindAll("g.graph-node").Count);    // el canvas sigue disponible
    }

    [Fact]
    public void DescartarTodas_VaciaLasAlertas()
    {
        var cut = RenderComponent<Simulator>();
        cut.Find("#btn-load-complex").Click();
        cut.Find("#btn-spof").Click();
        Assert.Equal(AlertService.MaxSpofAlerts + 1, cut.FindAll(".visual-alert").Count);   // 33 SPOF agrupados

        cut.Find("#btn-dismiss-all").Click();

        Assert.Empty(cut.FindAll(".visual-alert"));
        Assert.Equal(33, cut.FindAll("circle.spof-marker").Count);   // los marcadores siguen en el canvas
    }

    [Fact]
    public void Reiniciar_QuitaLasAlertas()
    {
        var cut = RenderSmall();
        Simulate(cut, TestGraphs.ByName(_graph, "Product DB").Id);

        cut.Find("#btn-reset").Click();

        Assert.Empty(cut.FindAll(".visual-alert"));
        Assert.Empty(cut.FindAll("circle.spof-marker"));
    }
}
