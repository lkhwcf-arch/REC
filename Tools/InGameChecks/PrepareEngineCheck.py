"""Copy only owned InGame dependencies into an isolated, disposable Unity project."""
from pathlib import Path
import json, re, shutil, os
root = Path(__file__).resolve().parents[2]
dest = root / 'Temp/InGameEngineCheck'
dest.mkdir(parents=True, exist_ok=True)
excluded = '0.Dark - Complete Horror UI'
assets = []
for directory, dirs, files in os.walk(root / 'Assets'):
    dirs[:] = [d for d in dirs if d != excluded]
    assets.extend(Path(directory) / f for f in files)
by_guid = {}
for p in assets:
    if p.suffix == '.meta':
        match = re.search(r'^guid: (\w+)', p.read_text(encoding='utf-8-sig'), re.M)
        if match: by_guid[match[1]] = Path(str(p)[:-5])
queue = [p for p in assets if '/3.Script/' in p.as_posix() and p.suffix == '.cs']
queue += [root / 'Assets/1.Scene/InGame.unity']
queue += list((root / 'ProjectSettings').glob('*'))
visited = set()
while queue:
    p = queue.pop()
    if p in visited or not p.is_file(): continue
    visited.add(p)
    target = dest / p.relative_to(root)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, target)
    meta = Path(str(p) + '.meta')
    if meta.exists(): queue.append(meta)
    if p.suffix.lower() in {'.meta', '.unity', '.prefab', '.asset', '.mat', '.controller', '.anim'}:
        for g in re.findall(r'guid: ([0-9a-f]{32})', p.read_text(encoding='utf-8-sig', errors='replace')):
            if g in by_guid: queue.append(by_guid[g])
packages = root / 'Library/PackageCache'
manifest = json.loads((root / 'Packages/manifest.json').read_text())
deps = {k:v for k,v in manifest['dependencies'].items() if k.startswith('com.unity.modules.')}
for p in packages.iterdir():
    if p.is_dir() and not any(x in p.name for x in ['collab-proxy', 'ide.', 'multiplayer.center', 'visualscripting']):
        deps[p.name.split('@')[0]] = 'file:' + p.as_posix()
(dest / 'Packages').mkdir(exist_ok=True)
(dest / 'Packages/manifest.json').write_text(json.dumps({'dependencies':deps}, indent=2))
print(f'{dest}: {len(visited)} files, {sum(p.stat().st_size for p in visited)/1e6:.1f} MB')
