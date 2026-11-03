using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using SimuladorCloud.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using SimuladorCloud.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Registrar servicios del simulador ─────────────────────────────────────────

// Persistencia en localStorage (RNF-03, RNF-09)
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IKeyValueStore, LocalStorageKeyValueStore>();

// Servicio principal de gestión del grafo G=(V,E) (RF-01, RF-02, RF-03)
builder.Services.AddSingleton<GraphService>();

// Motor de simulación de cascadas BFS/DFS (RF-04, RF-05, RF-12)
builder.Services.AddSingleton<CascadeSimulationService>();

// Detección de SPOFs mediante algoritmo de Tarjan (RF-06)
builder.Services.AddSingleton<SpofDetectionService>();

// Persistencia de topologías y resultados (RF-10, RF-11)
builder.Services.AddScoped<TopologyStorageService>();

await builder.Build().RunAsync();
