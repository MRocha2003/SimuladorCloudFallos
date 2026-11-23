// Service worker de DESARROLLO (dotnet run) — IT-8 · HU-15.
// Estrategia "red primero": cada recurso descargado se guarda en caché y,
// si no hay conexión, se sirve desde la caché. Permite comprobar el
// funcionamiento sin conexión (PT-32) también durante el desarrollo.
// En la publicación (dotnet publish) se usa service-worker.published.js.

const CACHE = 'simuladorcloud-dev-v1';

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(self.location.origin)) return;

    event.respondWith((async () => {
        const cache = await caches.open(CACHE);
        try {
            const response = await fetch(request);
            if (response.ok) cache.put(request, response.clone());
            return response;
        } catch (error) {
            const cached = await cache.match(request)
                ?? (request.mode === 'navigate' ? await cache.match('index.html') ?? await cache.match('./') : undefined);
            if (cached) return cached;
            throw error;
        }
    })());
});
