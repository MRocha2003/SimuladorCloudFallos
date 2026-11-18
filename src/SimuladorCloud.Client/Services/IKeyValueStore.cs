using Blazored.LocalStorage;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Almacén clave–valor de texto usado por la capa de persistencia.
/// Desacopla TopologyStorageService de Blazored.LocalStorage para poder
/// probarlo con un almacén en memoria (pruebas de integración, IT-2).
/// </summary>
public interface IKeyValueStore
{
    /// <summary>Obtiene el texto almacenado en la clave, o null si no existe.</summary>
    Task<string?> GetStringAsync(string key);

    /// <summary>Guarda el texto indicado en la clave.</summary>
    Task SetStringAsync(string key, string value);

    /// <summary>Elimina la clave.</summary>
    Task RemoveAsync(string key);
}

/// <summary>
/// Implementación sobre el localStorage del navegador (Blazored.LocalStorage).
/// Guarda el JSON como texto plano con SetItemAsStringAsync para evitar una
/// doble serialización del contenido.
/// </summary>
public class LocalStorageKeyValueStore : IKeyValueStore
{
    private readonly ILocalStorageService _localStorage;

    public LocalStorageKeyValueStore(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<string?> GetStringAsync(string key) =>
        await _localStorage.GetItemAsStringAsync(key);

    public async Task SetStringAsync(string key, string value) =>
        await _localStorage.SetItemAsStringAsync(key, value);

    public async Task RemoveAsync(string key) =>
        await _localStorage.RemoveItemAsync(key);
}
