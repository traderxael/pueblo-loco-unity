"""
PUEBLO LOCO — Entorno de valle (v4)

  - SM_Env_River    : segmento de río de 8x8 m con orillas de piedra, juncos y rocas
  - SM_Env_Bridge   : puente de arco de piedra de 8 m con barandas
  - SM_Env_Mountain : grupo de montañas low-poly para el fondo

Uso:
  blender.exe --background --factory-startup --offline-mode --python build_china_assets_v4.py
"""

import json
import math
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import build_china_assets_v2 as v2  # noqa: E402

bc = v2.bc
OUT = v2.OUT
PREV = v2.PREV

bc.MAT_COLORS.update({
    "Water": (0.12, 0.30, 0.34, 1.0),
    "Rice": (0.42, 0.62, 0.20, 1.0),
    "Straw": (0.86, 0.76, 0.48, 1.0),
    "Thatch": (0.72, 0.62, 0.36, 1.0),
    "Ember": (1.0, 0.42, 0.10, 1.0),
    "Terracotta": (0.66, 0.33, 0.20, 1.0),
})


def build_river():
    bc.reset_scene()
    o = []
    o.append(v2.box("water", (0, 0, 0.02), (8.0, 8.0, 0.05), "Water"))
    for sy in (-1, 1):
        o.append(v2.box("bank", (0, sy * 4.25, 0.16), (8.6, 0.6, 0.32), "Stone"))
        o.append(v2.box("bankTop", (0, sy * 4.30, 0.30), (8.6, 0.5, 0.10), "Stone"))
    for i in range(7):
        o.append(v2.cyl("reed", (-3.0 + i * 1.0, 2.4, 0.38), 0.03, 0.02, 0.75, "Rice", 5))
        o.append(v2.cyl("reed2", (-2.6 + i * 1.0, 2.0, 0.30), 0.025, 0.018, 0.60, "Rice", 5))
    for i in range(5):
        o.append(v2.ball("rock", (-2.8 + i * 1.5, -2.4, 0.20), 0.28, "Stone",
                         subdiv=1, scale=(1.0, 0.8, 0.7)))
    body = v2.join(o, "River")
    v2.uv_unwrap(body)
    v2.preview("Rio", 7.0, 1.2)
    path = os.path.join(OUT, "SM_Env_River.fbx")
    v2.export_static(path, body)
    v2.register("SM_Env_River", path, v2.tris_of(body), kind="env")


def build_bridge():
    bc.reset_scene()
    o = []
    n = 7
    for i in range(n):
        t = (i + 0.5) / n
        x = (t - 0.5) * 8.0
        z = 0.55 + math.sin(t * math.pi) * 1.25
        pitch = -math.cos(t * math.pi) * 0.45
        o.append(v2.box("deck", (x, 0, z), (1.30, 2.3, 0.24), "Stone", rot_y=pitch))
    for i in range(5):
        t = (i + 0.5) / 5
        x = (t - 0.5) * 6.4
        z = 0.20 + math.sin(t * math.pi) * 0.95
        o.append(v2.box("arch", (x, 0, z), (1.45, 2.1, 0.55), "Stone"))
    # barandas
    for sy in (-1, 1):
        for i in range(6):
            t = i / 5.0
            x = (t - 0.5) * 7.6
            z = 0.55 + math.sin(t * math.pi) * 1.25
            o.append(v2.box("post", (x, sy * 1.02, z + 0.50), (0.16, 0.16, 0.95), "Lacquer"))
        for i in range(5):
            t = (i + 0.5) / 5.0
            x = (t - 0.5) * 7.2
            z = 0.55 + math.sin(t * math.pi) * 1.25
            o.append(v2.box("rail", (x, sy * 1.02, z + 0.95), (1.95, 0.10, 0.12), "Lacquer"))
    body = v2.join(o, "Bridge")
    v2.uv_unwrap(body)
    v2.preview("Puente", 7.5, 1.6)
    path = os.path.join(OUT, "SM_Env_Bridge.fbx")
    v2.export_static(path, body)
    v2.register("SM_Env_Bridge", path, v2.tris_of(body), kind="env")


def build_mountain():
    bc.reset_scene()
    o = []
    peaks = [(0.0, 0.0, 8.0, 9.5), (5.5, 2.0, 5.6, 7.0), (-5.0, 2.5, 4.6, 6.0),
             (2.0, -4.5, 4.0, 5.5)]
    for i, (x, y, h, r) in enumerate(peaks):
        o.append(v2.cyl("peak", (x, y, h * 0.5), r, r * 0.10, h, "Stone", 9))
        o.append(v2.cyl("snow", (x, y, h * 0.88), r * 0.16, r * 0.05, h * 0.24, "Lime", 9))
    o.append(v2.cyl("foot", (0, 0, 0.5), 11.0, 9.0, 1.0, "Rice", 16))
    body = v2.join(o, "Mountain")
    v2.uv_unwrap(body)
    v2.preview("Montana", 20.0, 5.0)
    path = os.path.join(OUT, "SM_Env_Mountain.fbx")
    v2.export_static(path, body)
    v2.register("SM_Env_Mountain", path, v2.tris_of(body), kind="env")


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    print("[CHINA4] inicio", flush=True)
    build_river()
    build_bridge()
    build_mountain()

    index_path = os.path.join(v2.STAGE, "index.json")
    merged = {"asset_count": 0, "assets": []}
    if os.path.exists(index_path):
        with open(index_path, "r", encoding="utf-8") as fh:
            merged = json.load(fh)
    known = {a["name"] for a in merged.get("assets", [])}
    for entry in v2.REGISTRY:
        if entry["name"] not in known:
            merged["assets"].append(entry)
    merged["asset_count"] = len(merged["assets"])
    with open(index_path, "w", encoding="utf-8") as fh:
        json.dump(merged, fh, ensure_ascii=False, indent=2)
    print("[CHINA4] fin: %d assets (total %d)" % (len(v2.REGISTRY), merged["asset_count"]), flush=True)


if __name__ == "__main__":
    main()
