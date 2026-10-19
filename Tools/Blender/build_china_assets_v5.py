"""
PUEBLO LOCO — Calle de piedra estilo Frostpunk (tematica China antigua).

Genera SM_Prop_Street: segmento de camino de piedra de 1x1 m (encaja en la
grilla de decor) con bordes de piedra, losas y dos faroles rojos a los lados,
mas una variante larga de 2 m. Inspirado en los caminos que conectan los
edificios con el generador central en Frostpunk, pero con faroles chinos.
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


def build_street():
    bc.reset_scene()
    o = []
    # base de tierra apisonada
    o.append(v2.box("base", (0, 0, 0.04), (1.0, 1.0, 0.08), "Stone"))
    # losas de piedra (patron de piedra del camino)
    for i in range(3):
        y = -0.30 + i * 0.30
        for j in range(2):
            x = -0.22 + j * 0.44
            o.append(v2.box("slab", (x, y, 0.09), (0.38, 0.24, 0.04), "Lime",
                            rot_z=0.12 if (i + j) % 2 else -0.10))
    # bordes de piedra a los lados
    for sy in (-1, 1):
        o.append(v2.box("curb", (0, sy * 0.46, 0.07), (1.0, 0.08, 0.14), "Stone"))
    # faroles chinos en las esquinas
    for sx in (-1, 1):
        o.append(v2.cyl("lampPost", (sx * 0.38, 0.42, 0.35), 0.03, 0.025, 0.70, "DarkWood", 8))
        o.append(v2.box("lampArm", (sx * 0.38, 0.36, 0.68), (0.16, 0.04, 0.04), "DarkWood"))
        o += v2.lantern("lamp", sx * 0.28, 0.36, 0.52, 0.11)
    body = v2.join(o, "Street")
    v2.uv_unwrap(body)
    v2.preview("Camino", 2.4, 0.8)
    path = os.path.join(OUT, "SM_Prop_Street.fbx")
    v2.export_static(path, body)
    v2.register("SM_Prop_Street", path, v2.tris_of(body), kind="prop")


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    print("[CHINA5] inicio", flush=True)
    build_street()
    print("[CHINA5] fin", flush=True)


if __name__ == "__main__":
    main()
