using SimuladorCloud.Client.Models;
using System.Text.Json;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Servicio de persistencia de topologías y resultados en localStorage
/// del navegador mediante Blazored.LocalStorage.
/// Implementa RF-10, RNF-03 y RNF-09.
/// HU-03: Persistencia de topologías en sesión.
/// HU-10: Persistencia entre sesiones mediante localStorage.
/// </summary>
public class TopologyStorageService
{
    private readonly IKeyValueStore _store;
    private const string TopologyListKey  = "simulador_topologies";
    private const string ResultListKey    = "simulador_results";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented               = false
    };

    public TopologyStorageService(IKeyValueStore store)
    {
        _store = store;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TOPOLOGÍAS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda la topología actual en localStorage.
    /// HU-03 — Criterio de aceptación 1, 2.
    /// </summary>
    public async Task SaveTopologyAsync(GraphModel graph)
    {
        graph.UpdatedAt = DateTime.UtcNow;
        var topologies  = await GetTopologyListAsync();

        // Reemplazar si ya existe, agregar si es nueva
        var existing = topologies.FindIndex(t => t.TopologyId == graph.TopologyId);
        if (existing >= 0)
            topologies[existing] = graph.DeepClone();
        else
            topologies.Add(graph.DeepClone());

        await _store.SetStringAsync(TopologyListKey,
            JsonSerializer.Serialize(topologies, JsonOptions));
    }

    /// <summary>
    /// Carga una topología guardada por su ID.
    /// HU-03 — Criterio de aceptación 3, 4.
    /// </summary>
    public async Task<GraphModel?> LoadTopologyAsync(string topologyId)
    {
        var topologies = await GetTopologyListAsync();
        return topologies.FirstOrDefault(t => t.TopologyId == topologyId);
    }

    /// <summary>
    /// Obtiene la lista de todas las topologías guardadas.
    /// </summary>
    public async Task<List<GraphModel>> GetTopologyListAsync()
    {
        try
        {
            var json = await _store.GetStringAsync(TopologyListKey);
            if (string.IsNullOrEmpty(json)) return new List<GraphModel>();
            return JsonSerializer.Deserialize<List<GraphModel>>(json, JsonOptions)
                   ?? new List<GraphModel>();
        }
        catch
        {
            return new List<GraphModel>();
        }
    }

    /// <summary>
    /// Elimina una topología guardada por su ID.
    /// </summary>
    public async Task<bool> DeleteTopologyAsync(string topologyId)
    {
        var topologies = await GetTopologyListAsync();
        var removed    = topologies.RemoveAll(t => t.TopologyId == topologyId);
        if (removed == 0) return false;

        await _store.SetStringAsync(TopologyListKey,
            JsonSerializer.Serialize(topologies, JsonOptions));
        return true;
    }

    // ════════════════════════════════════════════════════════════════════════
    // RESULTADOS DE SIMULACIÓN
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda un resultado de simulación en localStorage.
    /// </summary>
    public async Task SaveResultAsync(SimulationResult result)
    {
        var results = await GetResultListAsync();
        results.Add(result);

        // Mantener solo los últimos 50 resultados para no saturar localStorage
        if (results.Count > 50)
            results = results.OrderByDescending(r => r.Timestamp).Take(50).ToList();

        await _store.SetStringAsync(ResultListKey,
            JsonSerializer.Serialize(results, JsonOptions));
    }

    /// <summary>
    /// Obtiene todos los resultados de simulación guardados.
    /// </summary>
    public async Task<List<SimulationResult>> GetResultListAsync()
    {
        try
        {
            var json = await _store.GetStringAsync(ResultListKey);
            if (string.IsNullOrEmpty(json)) return new List<SimulationResult>();
            return JsonSerializer.Deserialize<List<SimulationResult>>(json, JsonOptions)
                   ?? new List<SimulationResult>();
        }
        catch
        {
            return new List<SimulationResult>();
        }
    }

    /// <summary>
    /// Elimina todos los resultados guardados en localStorage.
    /// </summary>
    public async Task ClearResultsAsync() =>
        await _store.RemoveAsync(ResultListKey);
}
