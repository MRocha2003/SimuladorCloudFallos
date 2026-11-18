using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;
using SimuladorCloud.Tests.Helpers;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Pruebas unitarias de GraphService: CRUD del grafo G=(V,E) (IT-1, HU-01 y HU-02)
/// y actualización de posición para el arrastre de nodos (IT-2, HU-03).
/// </summary>
public class GraphServiceTests
{
    [Fact]
    public void AddVertex_CreaNodoOperativoConNombreYTipo()
    {
        var service = new GraphService();

        var v = service.AddVertex("API Gateway", NodeType.API, 120, 80);

        Assert.Single(service.Graph.Vertices);
        Assert.Equal("API Gateway", v.Name);
        Assert.Equal(NodeType.API, v.NodeType);
        Assert.Equal(NodeStatus.Operative, v.Status);
        Assert.Equal(120, v.PositionX);
        Assert.Equal(80, v.PositionY);
    }

    [Fact]
    public void AddVertex_DisparaEventoDeCambio()
    {
        var service = new GraphService();
        int eventos = 0;
        service.OnGraphChanged += () => eventos++;

        service.AddVertex("Auth", NodeType.Service);

        Assert.Equal(1, eventos);
    }

    [Fact]
    public void UpdateVertex_ModificaNombreYTipo()
    {
        var service = new GraphService();
        var v = service.AddVertex("DB", NodeType.Service);

        var ok = service.UpdateVertex(v.Id, "Base de Datos Principal", NodeType.Database);

        Assert.True(ok);
        Assert.Equal("Base de Datos Principal", v.Name);
        Assert.Equal(NodeType.Database, v.NodeType);
    }

    [Fact]
    public void UpdateVertex_NodoInexistente_DevuelveFalse()
    {
        var service = new GraphService();

        Assert.False(service.UpdateVertex("no-existe", "X", NodeType.API));
    }

    [Fact]
    public void RemoveVertex_EliminaNodoYSusAristas()
    {
        var (service, a, b, c) = TestGraphs.Chain();

        var ok = service.RemoveVertex(b.Id);

        Assert.True(ok);
        Assert.Equal(2, service.Graph.NodeCount);
        Assert.Empty(service.Graph.Edges);   // A→B y B→C dependían de B
        Assert.Null(service.Graph.GetVertex(b.Id));
    }

    [Fact]
    public void RemoveVertex_NodoInexistente_DevuelveFalse()
    {
        var service = new GraphService();

        Assert.False(service.RemoveVertex("no-existe"));
    }

    [Fact]
    public void AddEdge_CreaAristaDirigida()
    {
        var service = new GraphService();
        var a = service.AddVertex("A", NodeType.Service);
        var b = service.AddVertex("B", NodeType.Database);

        var edge = service.AddEdge(a.Id, b.Id);

        Assert.NotNull(edge);
        Assert.Equal(a.Id, edge!.SourceId);
        Assert.Equal(b.Id, edge.TargetId);
        Assert.Equal(1.0, edge.Weight);
        Assert.True(service.Graph.EdgeExists(a.Id, b.Id));
        Assert.False(service.Graph.EdgeExists(b.Id, a.Id));
    }

    [Fact]
    public void AddEdge_Duplicada_SeRechaza()
    {
        var service = new GraphService();
        var a = service.AddVertex("A", NodeType.Service);
        var b = service.AddVertex("B", NodeType.Service);
        service.AddEdge(a.Id, b.Id);

        var duplicada = service.AddEdge(a.Id, b.Id);

        Assert.Null(duplicada);
        Assert.Single(service.Graph.Edges);
    }

    [Fact]
    public void AddEdge_AutoBucle_SeRechaza()
    {
        var service = new GraphService();
        var a = service.AddVertex("A", NodeType.Service);

        Assert.Null(service.AddEdge(a.Id, a.Id));
        Assert.Empty(service.Graph.Edges);
    }

    [Fact]
    public void AddEdge_NodoInexistente_SeRechaza()
    {
        var service = new GraphService();
        var a = service.AddVertex("A", NodeType.Service);

        Assert.Null(service.AddEdge(a.Id, "no-existe"));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("self")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    public void ValidateEdge_InformaElMotivo(string caso)
    {
        var service = new GraphService();
        var a = service.AddVertex("A", NodeType.Service);
        var b = service.AddVertex("B", NodeType.Service);
        if (caso == "duplicate") service.AddEdge(a.Id, b.Id);

        var resultado = caso switch
        {
            "self"    => service.ValidateEdge(a.Id, a.Id),
            "missing" => service.ValidateEdge(a.Id, ""),
            _         => service.ValidateEdge(a.Id, b.Id)
        };

        var esperado = caso switch
        {
            "self"      => EdgeValidation.SelfLoop,
            "missing"   => EdgeValidation.MissingVertex,
            "duplicate" => EdgeValidation.Duplicate,
            _           => EdgeValidation.Valid
        };
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void RemoveEdge_EliminaSoloEsaArista()
    {
        var (service, a, b, c) = TestGraphs.Chain();
        var ab = service.Graph.Edges.Single(e => e.SourceId == a.Id);

        Assert.True(service.RemoveEdge(ab.Id));
        Assert.Single(service.Graph.Edges);
        Assert.False(service.RemoveEdge(ab.Id));
    }

    [Fact]
    public void UpdateVertexPosition_ActualizaCoordenadas()
    {
        var service = new GraphService();
        var v = service.AddVertex("A", NodeType.Service, 10, 10);

        Assert.True(service.UpdateVertexPosition(v.Id, 300, 250));
        Assert.Equal(300, v.PositionX);
        Assert.Equal(250, v.PositionY);
        Assert.False(service.UpdateVertexPosition("no-existe", 1, 1));
    }

    [Fact]
    public void ClearGraph_DejaElGrafoVacio()
    {
        var (service, _, _, _) = TestGraphs.Chain();

        service.ClearGraph();

        Assert.Equal(0, service.Graph.NodeCount);
        Assert.Equal(0, service.Graph.EdgeCount);
    }

    [Theory]
    [InlineData(0, 8, 10)]
    [InlineData(1, 20, 28)]
    public void LoadPredefinedTopology_CargaNodosYAristasEsperados(int escenario, int nodos, int aristas)
    {
        var service = new GraphService();

        service.LoadPredefinedTopology(escenario);

        Assert.Equal(nodos, service.Graph.NodeCount);
        Assert.Equal(aristas, service.Graph.EdgeCount);
    }

    [Fact]
    public void LoadGraph_ReemplazaElGrafoActual()
    {
        var service = new GraphService();
        var otro = new GraphModel { Name = "Externa" };
        otro.Vertices.Add(new Vertex { Name = "X" });

        service.LoadGraph(otro);

        Assert.Same(otro, service.Graph);
        Assert.Equal("Externa", service.Graph.Name);
    }

    [Fact]
    public void GraphModel_DependentsYDependencies_RespetanLaDireccion()
    {
        var (service, a, b, c) = TestGraphs.Chain();
        var g = service.Graph;

        // B depende de C; A depende de B
        Assert.Equal(new[] { a.Id }, g.GetDependents(b.Id).Select(v => v.Id));
        Assert.Equal(new[] { c.Id }, g.GetDependencies(b.Id).Select(v => v.Id));
        Assert.Empty(g.GetDependents(a.Id));
        Assert.Empty(g.GetDependencies(c.Id));
    }
}
