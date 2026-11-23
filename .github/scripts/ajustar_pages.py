"""Prepara la carpeta publicada de Blazor WebAssembly para GitHub Pages.

1. Cambia <base href="/"> por la ruta del repositorio (p. ej. /simuladorcloudfallos/).
2. Copia index.html como 404.html para que las rutas internas (/validation…) funcionen al recargar.
3. Crea .nojekyll para que Pages sirva la carpeta _framework.
4. Actualiza el hash de index.html en service-worker-assets.js (uso sin conexión).
Uso: python3 ajustar_pages.py <carpeta wwwroot> <ruta base>
"""
import base64, hashlib, pathlib, re, sys

root = pathlib.Path(sys.argv[1])
base = sys.argv[2]
index = root / "index.html"
html = index.read_text(encoding="utf-8").replace('<base href="/" />', f'<base href="{base}" />')
index.write_text(html, encoding="utf-8")
for ext in (".br", ".gz"):
    (root / f"index.html{ext}").unlink(missing_ok=True)
(root / "404.html").write_text(html, encoding="utf-8")
(root / ".nojekyll").write_text("", encoding="utf-8")

digest = "sha256-" + base64.b64encode(hashlib.sha256(index.read_bytes()).digest()).decode()
assets = root / "service-worker-assets.js"
if assets.exists():
    text = assets.read_text(encoding="utf-8")
    text = re.sub(r'("hash":\s*")[^"]*(",\s*"url":\s*"index\.html")', rf'\g<1>{digest}\g<2>', text)
    assets.write_text(text, encoding="utf-8")
print(f"Ruta base: {base}  ·  hash de index.html: {digest}")
