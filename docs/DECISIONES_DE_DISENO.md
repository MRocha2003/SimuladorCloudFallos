# Decisiones de diseño

Registro de las decisiones de arquitectura y diseño del simulador (IT-8 · HU-16, criterio 3).
Cada decisión indica el contexto, la alternativa elegida, las alternativas descartadas y sus consecuencias.

---

## DD-01 · Blazor WebAssembly sin servidor de aplicaciones

- **Contexto:** el simulador debe operar como un *sandbox* aislado, sin tocar infraestructura real y sin costo de hosting.
- **Decisión:** aplicación Blazor WebAssembly *standalone*; toda la lógica en C# compilado a WebAssembly y ejecutado en el navegador.
- **Descartadas:** Blazor Server (requiere conexión permanente con un servidor), React + API (dos lenguajes y un backend que mantener).
- **Consecuencias:** el límite del navegador actúa como aislamiento; la aplicación puede servirse como archivos estáticos y funcionar sin conexión. La primera carga descarga el runtime de .NET (algunos MB).

## DD-02 · Modelo de grafo dirigido G = (V, E) con la semántica «u depende de v»

- **Contexto:** hay que representar la arquitectura y la dirección de la propagación del fallo.
- **Decisión:** una arista `Source → Target` significa que `Source` depende de `Target`. El fallo de `v` se propaga a los nodos con aristas **entrantes** hacia `v`.
- **Consecuencias:** al diseñar las pruebas se detectó que la primera versión recorría las aristas salientes; la corrección quedó cubierta por pruebas de regresión.

## DD-03 · Algoritmos implementados directamente en lugar de usar QuikGraph

- **Contexto:** QuikGraph ofrece estructuras y algoritmos genéricos.
- **Decisión:** implementar BFS, Tarjan y rutas críticas sobre el propio modelo (`GraphModel`) con listas de adyacencia.
- **Motivos:** control explícito de la complejidad O(V+E), resultados deterministas (orden estable de recorrido), cálculo del impacto de cada SPOF y menor tamaño de descarga en WebAssembly.
- **Consecuencias:** la referencia a QuikGraph puede retirarse en una versión futura sin afectar la funcionalidad.

## DD-04 · Persistencia en `localStorage` a través de `IKeyValueStore`

- **Contexto:** las topologías deben persistir entre sesiones sin base de datos ni servidor.
- **Decisión:** serializar el grafo a JSON y guardarlo en `localStorage` (Blazored.LocalStorage), detrás de la interfaz `IKeyValueStore`.
- **Descartadas:** SQLite en WebAssembly (complejidad y tamaño), IndexedDB (API asíncrona más compleja para el volumen previsto).
- **Consecuencias:** la interfaz permite probar la persistencia con un almacén en memoria y corrigió un defecto de doble serialización. Límite: cuota de algunos MB por sitio, suficiente para topologías de cientos de nodos.

## DD-05 · Rutas críticas como caminos más largos sobre el grafo acíclico

- **Contexto:** se necesita una noción de «ruta crítica» explicable y eficiente.
- **Decisión:** ruta crítica = cadena de dependencias más larga desde un punto de entrada (nodo del que nadie depende) hasta una dependencia final; se rompen los ciclos descartando aristas de retroceso del DFS y se calcula el camino más largo por programación dinámica. Se priorizan las rutas que atraviesan más SPOF.
- **Descartada:** centralidad de intermediación (O(V·E), más costosa y menos intuitiva para el usuario).
- **Consecuencias:** O(V+E); en grafos con ciclos, la ruta depende del orden del DFS, que es determinista.

## DD-06 · Impacto de un SPOF: desconexión + cascada

- **Decisión:** para cada SPOF se calculan (a) los nodos que quedan fuera del fragmento principal al quitarlo del grafo no dirigido y (b) los nodos que fallarían en cascada. La criticidad es el número de nodos distintos afectados.
- **Consecuencias:** ordena los SPOF por gravedad y alimenta las alertas y el reporte. Las estructuras auxiliares se construyen una sola vez para todos los SPOF.

## DD-07 · Umbral de impacto crítico: más del 50 % de los nodos

- **Decisión:** `IsCriticalImpact` es verdadero solo si el porcentaje afectado es **estrictamente mayor** que 50 %, como fija la historia HU-12 («más del 50 % de los nodos»).

## DD-08 · Alertas no bloqueantes y agrupadas

- **Decisión:** las alertas se superponen al canvas sin capturar el puntero (`pointer-events: none` en el contenedor); se muestran como máximo 3 alertas de SPOF y el resto se agrupa, con opción de descartarlas una a una o todas.
- **Consecuencias:** en topologías grandes (33 SPOF) el canvas sigue siendo utilizable.

## DD-09 · Escenarios predefinidos con listas explícitas

- **Decisión:** las topologías de 50 y 100 nodos se definen con listas explícitas de nodos y dependencias, sin generación aleatoria.
- **Consecuencias:** resultados reproducibles y verificables manualmente; los valores de referencia se comprobaron con NetworkX y se usan en las pruebas y en la página de validación.

## DD-10 · Aplicación web progresiva y recursos locales

- **Contexto:** HU-15 exige que la aplicación funcione sin conexión una vez cargada.
- **Decisión:** *service worker* (red primero en desarrollo; precarga de todos los archivos en publicación), `manifest.webmanifest` y copia local de Bootstrap en lugar de la CDN.
- **Consecuencias:** sin dependencias externas en tiempo de ejecución; tras actualizar la aplicación puede ser necesario recargar dos veces para que el nuevo *service worker* tome el control.

## DD-11 · Estrategia de pruebas

- **Decisión:** pruebas unitarias con xUnit sobre servicios y modelos; pruebas de integración con bUnit sobre las páginas reales con un almacén en memoria; cobertura con coverlet y ReportGenerator; validación funcional repetible desde la interfaz.
- **Consecuencias:** las pruebas permitieron detectar defectos antes de la ejecución (dirección de la cascada, doble serialización, comentario XML mal formado) y documentan los criterios de aceptación de cada historia.
