# Sistema Simulador Web de Fallos en Cascada
### Basado en la Teoría de Grafos para la Gestión Preventiva de Riesgos en Ecosistemas Cloud Públicos

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://blazor.net)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Universidad](https://img.shields.io/badge/UAB-Ingeniería%20de%20Sistemas-blue)](https://uab.edu.bo)

---

## 📋 Descripción

Proyecto de Grado — Carrera de Ingeniería de Sistemas  
**Universidad Adventista de Bolivia (UAB)**  
**Autor:** David Manuel Rocha Crespo  
**Año:** 2025–2026

Simulador web preventivo que modela infraestructuras Cloud públicas como grafos
dirigidos G=(V,E) y analiza la propagación de fallos en cascada, detecta Puntos
Únicos de Fallo (SPOFs) e identifica rutas críticas, sin conexión a nubes reales.

---

## 🚀 Tecnologías

| Tecnología | Versión | Uso |
|---|---|---|
| .NET | 8.0 | Framework principal |
| Blazor WebAssembly | 8.0 | UI + ejecución WASM en cliente |
| C# | 12 | Lenguaje de implementación |
| Blazored.LocalStorage | 4.5 | Persistencia en navegador |
| QuikGraph | 2.5 | Librería de algoritmos de grafos |
| SVG nativo | — | Visualización interactiva del grafo |

---

## 🏗️ Arquitectura

```
SimuladorCloudFallos/
├── src/
│   ├── SimuladorCloud.Client/          ← Proyecto Blazor WASM
│   │   ├── Models/
│   │   │   ├── Vertex.cs               ← Entidad Nodo (componente Cloud)
│   │   │   ├── Edge.cs                 ← Entidad Arista (dependencia dirigida)
│   │   │   ├── GraphModel.cs           ← Grafo G=(V,E) completo
│   │   │   ├── SimulationResult.cs     ← Resultado de simulación
│   │   │   └── Enums.cs                ← NodeType, NodeStatus, SimulationType
│   │   ├── Services/
│   │   │   ├── GraphService.cs         ← CRUD de nodos y aristas
│   │   │   ├── CascadeSimulationService.cs ← Algoritmo BFS/DFS O(V+E)
│   │   │   ├── SpofDetectionService.cs ← Algoritmo de Tarjan O(V+E)
│   │   │   └── TopologyStorageService.cs   ← Persistencia localStorage
│   │   ├── Pages/
│   │   │   └── Simulator.razor         ← Página principal del simulador
│   │   └── wwwroot/css/
│   │       └── simulator.css           ← Estilos del simulador
├── tests/
│   └── SimuladorCloud.Tests/           ← Pruebas xUnit (unitarias) y bUnit (integración)
├── README.md
├── .gitignore
└── SimuladorCloud.sln
```

---

## ⚙️ Algoritmos Implementados

### 1. Propagación de fallos en cascada — BFS/DFS (IT-3)
- **Complejidad:** O(V+E)
- **Descripción:** A partir de un nodo inicial fallido, propaga el estado `Failed`
  a todos los nodos dependientes alcanzables mediante búsqueda en anchura.
- **Archivo:** `Services/CascadeSimulationService.cs`

### 2. Detección de SPOFs — Algoritmo de Tarjan (IT-4)
- **Complejidad:** O(V+E)
- **Descripción:** Identifica puntos de articulación del grafo — nodos cuya
  eliminación incrementa el número de componentes conexas.
- **Archivo:** `Services/SpofDetectionService.cs`

### 3. Análisis de rutas críticas (IT-5)
- **Descripción:** Identifica rutas de mayor profundidad ponderadas por
  centralidad de intermediación.
- **Estado:** En desarrollo (IT-5)

---

## 🔄 Estado del Proyecto — Iteraciones XP

| Iteración | Semanas | Estado | Entregable |
|---|---|---|---|
| **IT-1** | 1–2 | ✅ En progreso | Modelo G=(V,E) — CRUD nodos y aristas |
| **IT-2** | 3–4 | 🔲 Pendiente | Canvas SVG + localStorage |
| **IT-3** | 5–6 | 🔲 Pendiente | Motor BFS/DFS de cascadas |
| **IT-4** | 7–8 | 🔲 Pendiente | Algoritmo de Tarjan — SPOFs |
| **IT-5** | 9–10 | 🔲 Pendiente | Rutas críticas + Reportes |
| **IT-6** | 11–12 | 🔲 Pendiente | Interfaz completa + Alertas |
| **IT-7** | 13–14 | 🔲 Pendiente | Pruebas funcionales |
| **IT-8** | 15–16 | 🔲 Pendiente | Optimización WASM + Documentación |

---

## 🛠️ Cómo ejecutar el proyecto

### Prerrequisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Visual Studio 2022 / VS Code / Rider

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/TU_USUARIO/SimuladorCloudFallos.git
cd SimuladorCloudFallos

# 2. Restaurar dependencias
dotnet restore

# 3. Ejecutar el proyecto
cd src/SimuladorCloud.Client
dotnet run

# 4. Abrir en el navegador
# http://localhost:5000/simulator
```

---

## 📐 Modelo de datos

### Nodo (Vertex)
```csharp
public class Vertex {
    public string     Id        { get; set; }  // Guid único
    public string     Name      { get; set; }  // Nombre del componente
    public NodeType   NodeType  { get; set; }  // Service | Database | API | LoadBalancer | Storage
    public NodeStatus Status    { get; set; }  // Operative | Degraded | Failed
    public double     PositionX { get; set; }  // Coordenada X en canvas SVG
    public double     PositionY { get; set; }  // Coordenada Y en canvas SVG
    public bool       IsSpof    { get; set; }  // Punto Único de Fallo detectado
}
```

### Arista (Edge)
```csharp
public class Edge {
    public string SourceId      { get; set; }  // Nodo origen (dependiente)
    public string TargetId      { get; set; }  // Nodo destino (del que depende)
    public double Weight        { get; set; }  // Peso (1.0 por defecto)
    public bool   IsActive      { get; set; }  // Activa en la simulación
    public bool   IsCascadePath { get; set; }  // Parte de una cascada activa
}
```

---

## 🧪 Escenarios de prueba predefinidos

| Escenario | Nodos | Aristas | SPOFs esperados |
|---|---|---|---|
| Pequeño | 8 | 10 | 2 |
| Mediano | 20 | 28 | 4 |
| Grande | 50 | 73 | 7 |
| Complejo | 100 | 148 | 11 |

**Tiempo de respuesta objetivo:** < 120 ms en todos los escenarios (WASM, Chrome)

---

## 🔒 Sandbox — Aislamiento de producción

> ⚠️ **El simulador NO se conecta a AWS, Azure ni Google Cloud.**  
> Todas las simulaciones se ejecutan sobre modelos en memoria (localStorage del navegador).  
> No hay riesgo operativo para infraestructuras reales.

---

## 📄 Licencia

MIT License — ver archivo [LICENSE](LICENSE)

---

## 👤 Autor

**David Manuel Rocha Crespo**  
Estudiante — Ingeniería de Sistemas  
Universidad Adventista de Bolivia (UAB)  
Vinto, Cochabamba, Bolivia


---

## 🧪 Pruebas

Proyecto `tests/SimuladorCloud.Tests` (xUnit + bUnit + coverlet):

- `Unit/` — pruebas unitarias de `GraphService`, `CascadeSimulationService` y `SpofDetectionService`.
- `Integration/` — pruebas de integración de la persistencia (`TopologyStorageService` con almacén en memoria) y de la página `Simulator` con bUnit.

```bash
dotnet test                     # ejecutar todas las pruebas
./run-tests.ps1                 # Windows: pruebas + reporte de cobertura HTML
./run-tests.sh                  # Linux/macOS: pruebas + reporte de cobertura HTML
```

El reporte de cobertura queda en `TestResults/CoverageReport/index.html`.
