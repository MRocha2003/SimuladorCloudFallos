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
  eliminación incrementa el número de componentes conexas. Para cada SPOF
  calcula su impacto: nodos que quedarían desconectados y nodos que fallarían
  en cascada.
- **Archivo:** `Services/SpofDetectionService.cs`

### 3. Rutas críticas (IT-4 / IT-5)
- **Complejidad:** O(V+E)
- **Descripción:** Cadena de dependencias más larga desde un punto de entrada
  (nodo del que nadie depende) hasta una dependencia final. Se rompen los
  ciclos descartando aristas de retroceso (DFS) y se calcula el camino más
  largo por programación dinámica. Se priorizan las rutas que atraviesan más SPOF.
- **Archivo:** `Services/CriticalPathService.cs`

### 4. Reporte de impacto y alertas (IT-5 / IT-6)
- `Services/ReportService.cs`: reporte estructurado (nodo inicial, afectados, SPOFs y rutas críticas) copiable en texto.
- `Services/AlertService.cs`: alertas no bloqueantes por SPOF y por cascada crítica (más del 50% de los nodos).

---

## 🔄 Estado del Proyecto — Iteraciones XP

| Iteración | Semanas | Estado | Entregable |
|---|---|---|---|
| **IT-1** | 1–2 | ✅ Completada | Modelo G=(V,E) — CRUD nodos y aristas |
| **IT-2** | 3–4 | ✅ Completada | Canvas SVG + localStorage |
| **IT-3** | 5–6 | ✅ Completada | Motor BFS de cascadas |
| **IT-4** | 7–8 | ✅ Completada | Algoritmo de Tarjan — SPOFs, impacto y rutas críticas |
| **IT-5** | 9–10 | ✅ Completada | Reporte de impacto + panel de resultados colapsable |
| **IT-6** | 11–12 | ✅ Completada | Escenarios de 50 y 100 nodos + alertas visuales |
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

| Escenario | Nodos | Aristas | SPOFs |
|---|---|---|---|
| Pequeño | 8 | 10 | 1 |
| Mediano | 20 | 28 | 3 |
| Grande | 50 | 72 | 14 |
| Complejo | 100 | 150 | 33 |

Los SPOF y el alcance de la cascada de cada escenario se verificaron con una
implementación independiente (NetworkX) y están cubiertos por pruebas unitarias.

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
