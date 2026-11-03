using SimuladorCloud.Client.Models;
using SimuladorCloud.Client.Services;

namespace SimuladorCloud.Tests.Helpers;

/// <summary>
/// Almacén clave–valor en memoria que reemplaza al localStorage del navegador
/// en las pruebas de integración de la persistencia.
/// </summary>
public class InMemoryKeyValueStore : IKeyValueStore
{
    public Dictionary<string, string> Data { get; } = new();

    public Task<string?> GetStringAsync(string key) =>
        Task.FromResult(Data.TryGetValue(key, out var value) ? value : null);

    public Task SetStringAsync(string key, string value)
    {
        Data[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        Data.Remove(key);
        return Task.CompletedTask;
    }
}

/// <summary>Constructores de grafos de prueba.</summary>
public static class TestGraphs
{
    /// <summary>
    /// Crea una cadena de dependencias A → B → C (A depende de B y B depende de C).
    /// </summary>
    public static (GraphService service, Vertex a, Vertex b, Vertex c) Chain()
    {
        var service = new GraphService();
        var a = service.AddVertex("A", NodeType.Service);
        var b = service.AddVertex("B", NodeType.Service);
        var c = service.AddVertex("C", NodeType.Database);
        service.AddEdge(a.Id, b.Id);
        service.AddEdge(b.Id, c.Id);
        return (service, a, b, c);
    }

    /// <summary>Busca un vértice por nombre en el grafo actual.</summary>
    public static Vertex ByName(GraphService service, string name) =>
        service.Graph.Vertices.Single(v => v.Name == name);
}
