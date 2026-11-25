// Service worker de PUBLICACIÓN (dotnet publish) — IT-8 · HU-15.
// Descarga y guarda en caché todos los archivos de la aplicación al
// instalarse (lista generada por el SDK en service-worker-assets.js), de
// modo que el simulador funciona sin conexión una vez cargado (PT-32).
// Basado en la plantilla de aplicación web progresiva de Blazor WebAssembly.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// Ruta base de la aplicación: la carpeta donde está este service worker.
// Funciona igual en localhost ("/") y en GitHub Pages ("/SimuladorCloudFallos/").
const baseUrl = new URL('./', self.location);
const indexUrl = new URL('index.html', baseUrl).href;
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: instalación');
    // index.html se descarga sin comprobación de integridad, porque el despliegue
    // en GitHub Pages ajusta su <base href> después de compilar.
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => asset.url === 'index.html'
            ? new Request(asset.url, { cache: 'no-cache' })
            : new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
    self.skipWaiting();
}

async function onActivate(event) {
    console.info('Service worker: activación');
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
    await self.clients.claim();
}

async function onFetch(event) {
    const request = event.request;
    if (request.method !== 'GET') {
        return fetch(request);
    }
    const cache = await caches.open(cacheName);
    // Las navegaciones a rutas de la aplicación (/simulator, /validation…) se resuelven con index.html
    const isAppRoute = request.mode === 'navigate' && !manifestUrlList.some(url => url === request.url);
    if (isAppRoute) {
        const cachedIndex = await cache.match(indexUrl);
        if (cachedIndex) {
            return cachedIndex;
        }
        try {
            return await fetch(indexUrl, { cache: 'no-cache' });
        } catch {
            return Response.error();
        }
    }
    const cachedResponse = await cache.match(request);
    if (cachedResponse) {
        return cachedResponse;
    }
    try {
        return await fetch(request);
    } catch {
        return Response.error();
    }
}
