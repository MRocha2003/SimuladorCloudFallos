# Sistema Simulador Web de Fallos en Cascada
### Basado en la Teoría de Grafos para la Gestión Preventiva de Riesgos en Ecosistemas Cloud Públicos

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://blazor.net)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Universidad](https://img.shields.io/badge/UAB-Ingeniería%20de%20Sistemas-blue)](https://uab.edu.bo)
[![Desplegar en GitHub Pages](https://github.com/MRocha2003/simuladorcloudfallos/actions/workflows/deploy-pages.yml/badge.svg)](https://github.com/MRocha2003/simuladorcloudfallos/actions/workflows/deploy-pages.yml)

**▶ Demo en línea:** https://mrocha2003.github.io/simuladorcloudfallos/

---

## 📋 Descripción

Proyecto de Grado — Carrera de Ingeniería de Sistemas
**Universidad Adventista de Bolivia (UAB)** · **Autor:** David Manuel Rocha Crespo · **Tutor:** Mario Rolando Chura Ortiz · **Año:** 2025–2026

Simulador web **preventivo** que modela una infraestructura Cloud pública como un grafo
dirigido **G = (V, E)** —una arista *u → v* indica que *u depende de v*— y, sin conectarse
a ninguna nube real, permite:

- **Propagar un fallo en cascada** desde cualquier componente (BFS, O(V+E)).
- **Detectar Puntos Únicos de Fallo (SPOF)** con el algoritmo de Tarjan (O(V+E)) y estimar su impacto.
- **Identificar rutas críticas**: las cadenas de dependencias más largas, priorizando las que atraviesan SPOF.
- **Generar un reporte de impacto** copiable y recibir **alertas visuales** no bloqueantes.
- **Validar los algoritmos** sobre cuatro topologías de referencia (8, 20, 50 y 100 nodos).

Todo se ejecuta en el navegador con **Blazor WebAssembly**; una vez cargada, la aplicación
funciona **sin conexión**.

---

## 🚀 Tecnologías

| Tecnología | Versión | Uso |
|---|---|---|
| .NET / C# | 8.0 / 12 | Plataforma y lenguaje |
| Blazor WebAssembly | 8.0 | Interfaz y ejecución de los algoritmos en el cliente |
| Blazored.LocalStorage | 4.5 | Persistencia de topologías en el navegador |
| SVG nativo | — | Visualización interactiva del grafo |
| Bootstrap | 5.3 (copia local) | Estilos base de la interfaz |
| xUnit · bUnit · coverlet | 2.9 · 1.31 · 6.0 | Pruebas unitarias, de componentes y cobertura |

`QuikGraph 2.5` sigue referenciado, pero los algoritmos están implementados directamente
(véase [decisiones de diseño](docs/DECISIONES_DE_DISENO.md)).

---

## 🏗️ Arquitectura

Arquitectura en **tres capas** dentro de una única aplicación cliente:

```
┌──────────────────────── Navegador (sandbox) ────────────────────────┐
│ PRESENTACIÓN  Pages/: Simulator · Topologies · Validation · About   │
│               (formularios, canvas SVG, panel, alertas)             │
├──────────────────────────────────────────────────────────────────────┤
│ LÓGICA        Services/: GraphService · CascadeSimulationService    │
│               SpofDetectionService · CriticalPathService            │
│               ReportService · AlertService · AlgorithmValidation…   │
├──────────────────────────────────────────────────────────────────────┤
│ DATOS         Models/: Vertex · Edge · GraphModel · SimulationResult│
│               TopologyStorageService → IKeyValueStore → localStorage│
└──────────────────────────────────────────────────────────────────────┘
          ✕ sin conexión con AWS · Azure · Google Cloud
```

```
SimuladorCloudFallos/
├── src/SimuladorCloud.Client/          ← Blazor WebAssembly (.NET 8)
│   ├── Models/                         ← Vertex, Edge, GraphModel, SimulationResult, SpofImpact,
│   │                                     CriticalPath, ImpactReport, VisualAlert, ValidationResult, Enums
│   ├── Services/                       ← lógica y algoritmos (ver tabla siguiente)
│   ├── Pages/                          ← Simulator, Topologies, Validation, About, Index
│   ├── Shared/MainLayout.razor
│   └── wwwroot/                        ← index.html, css/, lib/bootstrap/, service workers, manifest
├── tests/SimuladorCloud.Tests/         ← xUnit (Unit/) y bUnit (Integration/)
├── docs/DECISIONES_DE_DISENO.md
├── run-tests.ps1 · run-tests.sh
└── SimuladorCloud.sln
```

| Servicio | Responsabilidad | Iteración |
|---|---|---|
| `GraphService` | CRUD y validación del grafo; escenarios predefinidos | IT-1, IT-6 |
| `TopologyStorageService` + `IKeyValueStore` | Guardar, listar, cargar y eliminar topologías | IT-2 |
| `CascadeSimulationService` | Propagación del fallo (BFS por aristas entrantes) | IT-3 |
| `SpofDetectionService` | Puntos de articulación (Tarjan) e impacto de cada SPOF | IT-4 |
| `CriticalPathService` | Rutas críticas | IT-4, IT-5 |
| `ReportService` | Reporte de impacto | IT-5 |
| `AlertService` | Alertas visuales | IT-6 |
| `AlgorithmValidationService` | Validación contra la verificación manual y tiempos | IT-7, IT-8 |

---

## ⚙️ Algoritmos

| Algoritmo | Complejidad | Descripción |
|---|---|---|
| **Cascada (BFS)** | O(V+E) | Desde el nodo fallido recorre las aristas activas **entrantes**: fallan los componentes que dependen de él, directa o indirectamente. Impacto crítico si afecta a **más del 50 %** de los nodos. |
| **SPOF (Tarjan)** | O(V+E) | DFS con tiempos de descubrimiento y valores *low-link* sobre el grafo no dirigido. Para cada SPOF se calcula qué nodos quedarían desconectados y cuáles fallarían en cascada. |
| **Rutas críticas** | O(V+E) | Un DFS descarta las aristas de retroceso (rompe ciclos); sobre el grafo acíclico se obtiene por programación dinámica la ruta más larga desde cada punto de entrada. Se priorizan las rutas con más SPOF. |

---

## 🧪 Escenarios de prueba predefinidos

| Escenario | Nodos | Aristas | SPOF |
|---|---|---|---|
| Pequeño | 8 | 10 | 1 |
| Mediano | 20 | 28 | 3 |
| Grande (comercio electrónico) | 50 | 72 | 14 |
| Complejo (multirregión) | 100 | 150 | 33 |

Los SPOF y el alcance de las cascadas se verificaron con una implementación independiente
(NetworkX). La página **✔ Validación** del simulador repite esa verificación en el propio
navegador y mide los tiempos (objetivo: **< 120 ms** por operación con 100 nodos).

---

## 🛠️ Instalación y ejecución

**Prerrequisitos:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) y un navegador
actual (Chrome, Edge o Firefox).

```bash
git clone https://github.com/MRocha2003/simuladorcloudfallos.git
cd simuladorcloudfallos
dotnet restore
dotnet run --project src/SimuladorCloud.Client
# Abrir http://localhost:5200
```

### Uso rápido
1. **Simulador** → *Topologías de prueba* → cargue una topología (o cree nodos y conéctelos).
2. Arrastre los nodos para reorganizar el diagrama y guarde la topología con un nombre.
3. Elija un nodo de origen y pulse **Simular cascada**: los nodos afectados se marcan en rojo,
   y el panel muestra el reporte, los SPOF y las rutas críticas.
4. **Detectar SPOFs y rutas críticas** analiza la arquitectura sin inyectar fallos;
   haga clic en un SPOF para ver su impacto.
5. **✔ Validación** ejecuta las comprobaciones sobre los cuatro escenarios.

### Funcionamiento sin conexión
El simulador es una aplicación web progresiva. Tras abrirlo una vez con conexión, puede
desactivar la red (DevTools → *Network* → *Offline*) y recargar: sigue funcionando.
Con `dotnet publish -c Release` se usa el *service worker* de publicación, que guarda en
caché todos los archivos de la aplicación al instalarse.

---

## 🧪 Pruebas

Proyecto `tests/SimuladorCloud.Tests` (xUnit + bUnit + coverlet):

- `Unit/` — servicios y modelos: grafo, cascada, SPOF e impacto, rutas críticas, reporte,
  alertas, escenarios, validación y cobertura de la documentación XML.
- `Integration/` — persistencia con almacén en memoria y páginas `Simulator` y `Validation` con bUnit.

```bash
dotnet test             # todas las pruebas
./run-tests.ps1         # Windows: pruebas + reporte de cobertura HTML
./run-tests.sh          # Linux/macOS
```

El reporte de cobertura queda en `TestResults/CoverageReport/index.html`.

---

## 🔄 Iteraciones XP

| Iteración | Semanas | Estado | Entregable |
|---|---|---|---|
| **IT-1** | 1–2 | ✅ | Modelo G=(V,E) — CRUD de nodos y aristas |
| **IT-2** | 3–4 | ✅ | Canvas SVG interactivo + localStorage |
| **IT-3** | 5–6 | ✅ | Motor BFS de propagación en cascada |
| **IT-4** | 7–8 | ✅ | Tarjan: SPOF, impacto y rutas críticas |
| **IT-5** | 9–10 | ✅ | Reporte de impacto + panel de resultados |
| **IT-6** | 11–12 | ✅ | Escenarios de 50 y 100 nodos + alertas visuales |
| **IT-7** | 13–14 | ✅ | Validación funcional + limpieza con confirmación |
| **IT-8** | 15–16 | ✅ | Sin conexión (PWA), rendimiento y documentación |

Las decisiones de diseño se documentan en [`docs/DECISIONES_DE_DISENO.md`](docs/DECISIONES_DE_DISENO.md).

---

## 🔒 Sandbox — Aislamiento de producción

> ⚠️ **El simulador NO se conecta a AWS, Azure ni Google Cloud.**
> Las simulaciones se ejecutan sobre modelos en memoria en el navegador; las topologías se
> guardan en el `localStorage` del propio equipo. No hay riesgo operativo para infraestructuras reales.

---

## 📄 Licencia

MIT — ver [LICENSE](LICENSE). Bootstrap se distribuye bajo licencia MIT (`wwwroot/lib/bootstrap/LICENSE`).

## 👤 Autor

**David Manuel Rocha Crespo** — Ingeniería de Sistemas, Universidad Adventista de Bolivia (UAB), Vinto, Cochabamba, Bolivia.
