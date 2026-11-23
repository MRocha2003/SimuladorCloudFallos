using SimuladorCloud.Client.Services;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias de la validación funcional de los algoritmos
/// (IT-7 · HU-13: PT-26 a PT-29; IT-8 · HU-15: PT-30 y PT-34).
/// </summary>
public class AlgorithmValidationServiceTests
{
    private readonly AlgorithmValidationService _service = new();

    [Fact]
    public void Validacion_CubreLasCuatroTopologias()
    {
        var results = _service.RunAll();

        Assert.Equal(new[] { 8, 20, 50, 100 }, results.Select(r => r.NodeCount));
        Assert.Equal(new[] { 10, 28, 72, 150 }, results.Select(r => r.EdgeCount));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(2, 14)]
    [InlineData(3, 33)]
    public void Spofs_CoincidenConLaVerificacionManual(int scenario, int expectedSpofs)
    {
        var r = _service.Run(scenario);

        Assert.True(r.SpofsMatch, $"No detectados: {string.Join(", ", r.MissingSpofs)}; no esperados: {string.Join(", ", r.UnexpectedSpofs)}");
        Assert.Equal(expectedSpofs, r.DetectedSpofCount);
        Assert.Equal(expectedSpofs, r.ExpectedSpofCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Cascadas_AlcanzanLosNodosEsperadosYSonReproducibles(int scenario)
    {
        var r = _service.Run(scenario);

        Assert.Equal(3, r.CascadeChecks.Count);
        Assert.All(r.CascadeChecks, c => Assert.True(c.Passed, $"{c.Origin}: {c.ActualAffected} en lugar de {c.ExpectedAffected}"));
        Assert.True(r.Reproducible);
    }

    [Fact]
    public void Tiempos_TopologiaDe100Nodos_PorDebajoDe120ms()
    {
        var r = _service.Run(3);

        Assert.True(r.CascadeMs < AlgorithmValidationService.TimeLimitMs, $"Cascada: {r.CascadeMs:F2} ms");
        Assert.True(r.SpofMs < AlgorithmValidationService.TimeLimitMs, $"SPOF: {r.SpofMs:F2} ms");
        Assert.True(r.CriticalPathMs < AlgorithmValidationService.TimeLimitMs, $"Rutas: {r.CriticalPathMs:F2} ms");
        Assert.True(r.WithinTimeLimit);
    }

    [Fact]
    public void TodasLasTopologias_SuperanLaValidacion()
    {
        Assert.All(_service.RunAll(), r => Assert.True(r.Passed, r.TopologyName));
    }

    [Fact]
    public void Validacion_NoModificaLaTopologiaAbiertaEnElSimulador()
    {
        var abierta = new GraphService();
        abierta.LoadPredefinedTopology(1);
        var nodos = abierta.Graph.Vertices.Select(v => v.Id).ToList();

        _service.RunAll();

        Assert.Equal(nodos, abierta.Graph.Vertices.Select(v => v.Id));
        Assert.Empty(abierta.Graph.Spofs);
    }

    [Fact]
    public void FueraDelNavegador_NoSeReportaWebAssembly()
    {
        Assert.False(AlgorithmValidationService.RunningInBrowser);
    }
}
