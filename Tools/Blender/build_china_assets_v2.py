import os
from pathlib import Path
"""
PUEBLO LOCO â€” Ciudad China Antigua (v2)
Assets rediseÃ±ados siguiendo el prompt maestro (Tang/Ming, semi-estilizado).

Mejoras v2:
  - Paleta del prompt: rojo laca, dorado, jade, madera oscura, techo azul-gris, blanco cal.
  - Techos con aleros levantados, tabla de alero, caballete y dougong (mÃ©nsulas).
  - CelosÃ­as geomÃ©tricas, farolillos, banderolas, leones guardianes, barandas.
  - Pagoda de 5 pisos con pinÃ¡culo de aros.
  - Personajes con nombres de hueso estilo Unity Humanoid y 8 animaciones
    (Idle, Walk, Run, Work, Wave, Bow, Cheer, Gather).
  - Nomenclatura SM_/SK_ y index.json.

Uso:
  blender.exe --background --factory-startup --offline-mode --python build_china_assets_v2.py
"""

import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector, Euler

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import build_characters as bc  # noqa: E402

PROJECT = str(Path(__file__).resolve().parents[2])
STAGE = os.environ.get("PUEBLO_STAGE_CHINA_V2", str(Path(PROJECT) / "_stage_china_v2"))
OUT = os.path.join(STAGE, "assets")
PREV = os.path.join(STAGE, "previews")

# Paleta del prompt
LACQUER = (0.702, 0.231, 0.180, 1.0)   # #B33B2E
GOLD = (0.851, 0.643, 0.255, 1.0)      # #D9A441
JADE = (0.243, 0.486, 0.349, 1.0)      # #3E7C59
DARKWOOD = (0.290, 0.216, 0.157, 1.0)  # #4A3728
ROOFBLUE = (0.216, 0.278, 0.310, 1.0)  # #37474F
LIME = (0.929, 0.902, 0.839, 1.0)      # #EDE6D6
STONEC = (0.545, 0.529, 0.494, 1.0)
BRONZE = (0.333, 0.435, 0.361, 1.0)
SKIN = (0.933, 0.741, 0.612, 1.0)
HAIRC = (0.118, 0.098, 0.086, 1.0)
INK = (0.078, 0.071, 0.067, 1.0)

bc.MAT_COLORS.update({
    "Lacquer": LACQUER, "Gold": GOLD, "Jade": JADE, "DarkWood": DARKWOOD,
    "RoofBlue": ROOFBLUE, "Lime": LIME, "Stone": STONEC, "Bronze": BRONZE,
    "Skin": SKIN, "Hair": HAIRC, "Ink": INK,
    # nombres que ya usa el tinte de Unity
    "Wall": LIME, "RoofTiles": ROOFBLUE, "Wood": DARKWOOD, "Red": LACQUER,
    "Paper": LIME, "Trim": GOLD, "Robe": LACQUER, "Boots": INK,
    "Armor": GOLD, "Trousers": INK, "Door": DARKWOOD, "Window": LIME,
    "Canvas": (0.85, 0.78, 0.62, 1.0), "CanvasRed": LACQUER,
})


# ---------------------------------------------------------------------------
# helpers
# ---------------------------------------------------------------------------

def mesh_obj(name, verts, faces, mat_name):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.validate()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(bc.get_material(mat_name))
    return obj


def box(name, center, size, mat, rot_x=0.0, rot_y=0.0, rot_z=0.0):
    cx, cy, cz = center
    sx, sy, sz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
    verts = [(-sx, -sy, -sz), (sx, -sy, -sz), (sx, sy, -sz), (-sx, sy, -sz),
             (-sx, -sy, sz), (sx, -sy, sz), (sx, sy, sz), (-sx, sy, sz)]
    e = Euler((rot_x, rot_y, rot_z), "XYZ")
    verts = [tuple((Vector(v)).to_tuple()) for v in [e.to_matrix() @ Vector(v) for v in verts]]
    verts = [(x + cx, y + cy, z + cz) for (x, y, z) in verts]
    faces = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    return mesh_obj(name, verts, faces, mat)


def cyl(name, center, r_bottom, r_top, height, mat, segments=12, rot_x=0.0):
    verts, faces = [], []
    cx, cy, cz = center
    hz = height * 0.5
    for i in range(segments):
        a = 2 * math.pi * i / segments
        x, y = math.cos(a), math.sin(a)
        verts.append((x * r_bottom, y * r_bottom, -hz))
        verts.append((x * r_top, y * r_top, hz))
    for i in range(segments):
        a0, a1 = i * 2, ((i + 1) % segments) * 2
        faces.append((a0, a1, a1 + 1, a0 + 1))
    faces.append(tuple(range(0, segments * 2, 2)))
    faces.append(tuple(range(segments * 2 - 1, 0, -2)))
    if rot_x:
        c, s = math.cos(rot_x), math.sin(rot_x)
        verts = [(x, y * c - z * s, y * s + z * c) for (x, y, z) in verts]
    verts = [(x + cx, y + cy, z + cz) for (x, y, z) in verts]
    return mesh_obj(name, verts, faces, mat)


def ball(name, center, radius, mat, subdiv=2, scale=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    mesh = bpy.data.meshes.new(name + "Mesh")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for v in mesh.vertices:
        v.co.x *= scale[0]
        v.co.y *= scale[1]
        v.co.z *= scale[2]
    obj.location = Vector(center)
    obj.data.materials.append(bc.get_material(mat))
    return obj


def join(objs, name):
    return bc.join_objects(objs, name)


def roof(name, w, d, h, ov, tip, mat="RoofTiles", ridge_mat="Lacquer", tiles=False):
    """Techo a cuatro aguas con alero levantado, tabla de alero, caballete y tejas."""
    objs = []
    c, e = w * 0.5 + ov, d * 0.5 + ov
    ring0 = [(c, e, tip), (-c, e, tip), (-c, -e, tip), (c, -e, tip)]
    ring1 = [(w * 0.47, d * 0.47, h * 0.40), (-w * 0.47, d * 0.47, h * 0.40),
             (-w * 0.47, -d * 0.47, h * 0.40), (w * 0.47, -d * 0.47, h * 0.40)]
    ring2 = [(w * 0.26, d * 0.26, h * 0.70), (-w * 0.26, d * 0.26, h * 0.70),
             (-w * 0.26, -d * 0.26, h * 0.70), (w * 0.26, -d * 0.26, h * 0.70)]
    ridge = [(w * 0.11, 0.0, h), (-w * 0.11, 0.0, h)]
    verts = ring0 + ring1 + ring2 + ridge + [(0, 0, 0)]
    cidx = len(verts) - 1
    faces = []
    for i in range(4):
        j = (i + 1) % 4
        faces.append((i, j, 4 + j, 4 + i))
        faces.append((4 + i, 4 + j, 8 + j, 8 + i))
    faces += [(8, 9, 13, 12), (10, 11, 12, 13), (9, 10, 13), (11, 8, 12)]
    for i in range(4):
        faces.append((cidx, (i + 1) % 4, i))
    objs.append(mesh_obj(name + "_slopes", verts, faces, mat))

    # tabla de alero (marco)
    board = "DarkWood"
    objs.append(box(name + "_eaveN", (0, e, tip * 0.5), (2 * c, 0.10, 0.10), board))
    objs.append(box(name + "_eaveS", (0, -e, tip * 0.5), (2 * c, 0.10, 0.10), board))
    objs.append(box(name + "_eaveE", (c, 0, tip * 0.5), (0.10, 2 * e, 0.10), board))
    objs.append(box(name + "_eaveW", (-c, 0, tip * 0.5), (0.10, 2 * e, 0.10), board))

    # caballete + remates
    objs.append(box(name + "_ridge", (0, 0, h + 0.04), (w * 0.34, 0.13, 0.12), ridge_mat))
    for sx in (-1, 1):
        objs.append(box(name + "_ridgeEnd", (sx * w * 0.17, 0, h + 0.16), (0.08, 0.10, 0.16),
                        ridge_mat, rot_y=sx * 0.35))

    # esquinas levantadas (cuernos)
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(box(name + "_horn", (sx * c * 0.98, sy * e * 0.98, tip + 0.14),
                            (0.12, 0.12, 0.26), ridge_mat, rot_x=sy * 0.4, rot_y=sx * 0.4))

    # tejas: tiras en las cuatro aguas
    if tiles:
        for side in range(4):
            for k in range(1, 5):
                t = k / 5.0
                z = tip + (h - tip) * t
                span = (w * 0.5 + ov) * (1 - 0.55 * t)
                dep = (d * 0.5 + ov) * (1 - 0.55 * t)
                if side == 0:
                    objs.append(box(name + "_tile", (0, dep, z), (2 * span * 0.98, 0.05, 0.04), "RoofTiles"))
                elif side == 1:
                    objs.append(box(name + "_tile", (0, -dep, z), (2 * span * 0.98, 0.05, 0.04), "RoofTiles"))
                elif side == 2:
                    objs.append(box(name + "_tile", (span, 0, z), (0.05, 2 * dep * 0.98, 0.04), "RoofTiles"))
                else:
                    objs.append(box(name + "_tile", (-span, 0, z), (0.05, 2 * dep * 0.98, 0.04), "RoofTiles"))
    return objs


def dougong(name, w, d, z, count=4, mat="DarkWood"):
    """MÃ©nsulas entrelazadas bajo el alero (bloques en dos niveles)."""
    objs = []
    hw, hd = w * 0.5, d * 0.5
    for i in range(count):
        t = (i + 0.5) / count
        x = -hw + 2 * hw * t
        objs.append(box(name + "_dgN", (x, hd - 0.06, z), (0.22, 0.16, 0.10), mat))
        objs.append(box(name + "_dgS", (x, -hd + 0.06, z), (0.22, 0.16, 0.10), mat))
        y = -hd + 2 * hd * t
        objs.append(box(name + "_dgE", (hw - 0.06, y, z), (0.16, 0.22, 0.10), mat))
        objs.append(box(name + "_dgW", (-hw + 0.06, y, z), (0.16, 0.22, 0.10), mat))
    for i in range(count):
        t = (i + 0.5) / count
        x = -hw + 2 * hw * t
        objs.append(box(name + "_dg2N", (x, hd - 0.02, z + 0.12), (0.14, 0.10, 0.08), "Lacquer"))
        objs.append(box(name + "_dg2S", (x, -hd + 0.02, z + 0.12), (0.14, 0.10, 0.08), "Lacquer"))
    return objs


def lattice_window(name, x, y, z, w, h, facing="front"):
    """CelosÃ­a: marco de madera + barrotes + panel claro."""
    objs = []
    depth = 0.06
    dx, dy = (0, 0) if facing == "front" else (depth, 0)
    objs.append(box(name + "_frame", (x, y, z), (w, depth, h), "DarkWood"))
    objs.append(box(name + "_paper", (x, y - 0.02 if facing == "front" else y + 0.02, z),
                    (w * 0.82, 0.02, h * 0.82), "Lime"))
    for i in range(1, 4):
        objs.append(box(name + "_barV", (x - w * 0.5 + w * i / 4.0, y - 0.04, z),
                        (0.03, 0.03, h * 0.9), "DarkWood"))
        objs.append(box(name + "_barH", (x, y - 0.04, z - h * 0.5 + h * i / 4.0),
                        (w * 0.9, 0.03, 0.03), "DarkWood"))
    return objs


def lantern(name, x, y, z, size=0.16, color="Lacquer"):
    objs = [ball(name + "_body", (x, y, z), size, color, subdiv=2, scale=(1, 1, 1.15)),
            cyl(name + "_capT", (x, y, z + size * 1.15), size * 0.45, size * 0.35, 0.05, "Gold", 8),
            cyl(name + "_capB", (x, y, z - size * 1.15), size * 0.35, size * 0.45, 0.05, "Gold", 8),
            cyl(name + "_tassel", (x, y, z - size * 1.35), 0.015, 0.015, 0.16, "Gold", 6)]
    return objs


def banner(name, x, y, z, w=0.28, h=0.9):
    objs = [box(name + "_cloth", (x, y, z), (w, 0.03, h), "Lacquer"),
            box(name + "_trim", (x, y - 0.02, z - h * 0.42), (w * 1.05, 0.02, 0.07), "Gold"),
            cyl(name + "_pole", (x, y + 0.04, z + h * 0.5), 0.02, 0.02, 0.3, "DarkWood", 6)]
    return objs


def lion(name, x, y, z=0.0):
    """LeÃ³n guardiÃ¡n estilizado sobre pedestal."""
    objs = [box(name + "_ped", (x, y, z + 0.12), (0.34, 0.34, 0.24), "Stone"),
            box(name + "_body", (x, y, z + 0.40), (0.22, 0.30, 0.26), "Stone"),
            ball(name + "_head", (x, y - 0.10, z + 0.60), 0.13, "Stone", subdiv=1),
            box(name + "_ear", (x - 0.07, y - 0.06, z + 0.70), (0.05, 0.05, 0.08), "Stone"),
            box(name + "_ear2", (x + 0.07, y - 0.06, z + 0.70), (0.05, 0.05, 0.08), "Stone")]
    return objs


def railing(name, w, d, z, mat="Lacquer"):
    objs = []
    n = max(3, int(w * 2))
    for i in range(n + 1):
        t = i / n
        x = -w * 0.5 + w * t
        for sy in (-1, 1):
            objs.append(box(name + "_post", (x, sy * d * 0.5, z), (0.04, 0.04, 0.22), mat))
    objs.append(box(name + "_top1", (0, d * 0.5, z + 0.11), (w, 0.05, 0.05), mat))
    objs.append(box(name + "_top2", (0, -d * 0.5, z + 0.11), (w, 0.05, 0.05), mat))
    for sx in (-1, 1):
        objs.append(box(name + "_top3", (sx * w * 0.5, 0, z + 0.11), (0.05, d, 0.05), mat))
    return objs



def uv_unwrap(obj, angle=66.0, margin=0.02):
    """UV para que las texturas importadas en Unity se vean bien."""
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")
def export_static(path, obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        mesh_smooth_type="FACE", use_mesh_modifiers=True, add_leaf_bones=False,
    )


def preview(name, size=3.0, z=0.9):
    bpy.ops.object.camera_add(location=(size * 0.85, -size * 1.05, z + size * 0.55))
    cam = bpy.context.active_object
    direction = Vector((0.0, 0.0, z * 0.5)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = 50
    bpy.context.scene.camera = cam
    bpy.ops.object.light_add(type="SUN", rotation=(math.radians(52), 0, math.radians(35)))
    bpy.context.active_object.data.energy = 4.0
    bpy.ops.object.light_add(type="SUN", rotation=(math.radians(70), 0, math.radians(-120)))
    bpy.context.active_object.data.energy = 1.2
    scene = bpy.context.scene
    scene.render.resolution_x = 560
    scene.render.resolution_y = 560
    scene.render.filepath = os.path.join(PREV, name + ".png")
    bpy.ops.render.render(write_still=True)


REGISTRY = []


def register(name, path, tris, bones=0, actions=None, loops=None, kind="model"):
    REGISTRY.append({
        "name": name, "file": os.path.basename(path), "tris": tris,
        "bones": bones, "animations": actions or [], "loops": loops or [],
        "kind": kind,
    })
    print("[CHINA2] %s tris=%d bones=%d actions=%s size=%d" % (
        name, tris, bones, actions or [], os.path.getsize(path)), flush=True)


def tris_of(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# ---------------------------------------------------------------------------
# EDIFICIOS
# ---------------------------------------------------------------------------

def build_casa():
    bc.reset_scene()
    o = []
    o.append(box("plinth", (0, 0, 0.10), (2.30, 0.20, 2.30), "Stone"))
    o.append(box("plinth2", (0, 0, 0.20), (2.10, 0.10, 2.10), "Stone"))
    # columnas
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(cyl("col", (0.92 * sx, 0.92 * sy, 1.05), 0.075, 0.07, 1.55, "Lacquer", 10))
            o.append(cyl("colBase", (0.92 * sx, 0.92 * sy, 0.30), 0.11, 0.10, 0.10, "Stone", 10))
    # paneles de pared entre columnas (blanco cal)
    o.append(box("wallBack", (0, 0.94, 0.95), (1.75, 0.10, 1.35), "Lime"))
    o.append(box("wallL", (-0.94, 0, 0.95), (0.10, 1.75, 1.35), "Lime"))
    o.append(box("wallR", (0.94, 0, 0.95), (0.10, 1.75, 1.35), "Lime"))
    o.append(box("wallFrontTop", (0, -0.94, 1.50), (1.75, 0.10, 0.26), "Lime"))
    # puerta central con studs dorados
    o.append(box("door", (0, -0.96, 0.80), (0.80, 0.08, 1.05), "DarkWood"))
    o.append(box("doorFrame", (0, -0.99, 0.80), (0.94, 0.05, 1.12), "Lacquer"))
    for i in range(3):
        for j in range(3):
            o.append(ball("stud", (-0.24 + i * 0.24, -1.01, 0.55 + j * 0.26), 0.022, "Gold", subdiv=1))
    # celosÃ­as laterales
    o += lattice_window("winL", -0.55, -0.96, 1.20, 0.42, 0.42)
    o += lattice_window("winR", 0.55, -0.96, 1.20, 0.42, 0.42)
    # vigas + dougong + techo
    o.append(box("beamFront", (0, -0.92, 1.72), (2.05, 0.14, 0.14), "DarkWood"))
    o.append(box("beamBack", (0, 0.92, 1.72), (2.05, 0.14, 0.14), "DarkWood"))
    o.append(box("beamL", (-0.92, 0, 1.72), (0.14, 2.05, 0.14), "DarkWood"))
    o.append(box("beamR", (0.92, 0, 1.72), (0.14, 2.05, 0.14), "DarkWood"))
    o += dougong("dg", 2.0, 2.0, 1.80, 3)
    r = roof("roof", 2.15, 2.15, 0.85, 0.42, 0.26)
    for ob in r:
        ob.location.z += 1.88
    o += r
    # farolillos y leones
    for sx in (-1, 1):
        o += lantern("lant", sx * 0.75, -1.05, 1.45, 0.12)
        o += lion("lion", sx * 0.85, -1.25, 0.0)
    body = join(o, "CasaHan"); uv_unwrap(body)
    preview("CasaHan", 4.4, 1.3)
    path = os.path.join(OUT, "SM_Building_CasaHan.fbx")
    export_static(path, body)
    register("SM_Building_CasaHan", path, tris_of(body), kind="building")


def build_pagoda():
    bc.reset_scene()
    o = []
    o.append(box("base", (0, 0, 0.18), (2.6, 0.36, 2.6), "Stone"))
    o.append(box("base2", (0, 0, 0.34), (2.2, 0.14, 2.2), "Stone"))
    tiers = [(1.70, 0.40, 1.10, "Lacquer"), (1.45, 1.72, 0.95, "Lime"),
             (1.20, 2.86, 0.85, "Lacquer"), (1.00, 3.88, 0.75, "Lime"),
             (0.80, 4.78, 0.65, "Lacquer")]
    for i, (w, z, h, mat) in enumerate(tiers):
        o.append(box("body%d" % i, (0, 0, z + h * 0.5), (w, w, h), mat))
        # columnas de esquina
        for sx in (-1, 1):
            for sy in (-1, 1):
                o.append(cyl("col%d" % i, (sx * w * 0.48, sy * w * 0.48, z + h * 0.5),
                             0.055, 0.05, h * 0.95, "DarkWood", 8))
        # puerta/ventanas
        o.append(box("door%d" % i, (0, -w * 0.5 - 0.02, z + h * 0.42), (w * 0.34, 0.06, h * 0.72), "DarkWood"))
        o.append(box("win%d" % i, (w * 0.5 + 0.02, 0, z + h * 0.55), (0.06, w * 0.34, h * 0.5), "DarkWood"))
        # balcÃ³n + baranda
        if i > 0:
            o.append(box("balc%d" % i, (0, 0, z + 0.02), (w + 0.35, w + 0.35, 0.08), "DarkWood"))
        o += dougong("dg%d" % i, w + 0.2, w + 0.2, z + h * 0.92, 3)
        r = roof("roof%d" % i, w + 0.24, w + 0.24, 0.55, 0.30, 0.22)
        for ob in r:
            ob.location.z += z + h
        o += r
        if i == 0:
            for sx in (-1, 1):
                o += lantern("lant%d" % i, sx * (w * 0.5 + 0.3), -w * 0.5 - 0.2, z + h * 0.4, 0.11)
    # pinÃ¡culo: aros dorados + esfera
    for k in range(5):
        o.append(cyl("ring%d" % k, (0, 0, 5.55 + k * 0.12), 0.13 - k * 0.015, 0.13 - k * 0.015, 0.03, "Gold", 12))
    o.append(cyl("spire", (0, 0, 5.95), 0.035, 0.02, 0.55, "Gold", 8))
    o.append(ball("orb", (0, 0, 6.28), 0.07, "Gold", subdiv=2))
    body = join(o, "Pagoda05"); uv_unwrap(body)
    preview("Pagoda05", 8.0, 3.2)
    path = os.path.join(OUT, "SM_Building_Pagoda_05.fbx")
    export_static(path, body)
    register("SM_Building_Pagoda_05", path, tris_of(body), kind="building")


def build_paifang():
    bc.reset_scene()
    o = []
    for sx in (-1, 1):
        o.append(box("base", (1.55 * sx, 0, 0.14), (0.55, 0.55, 0.28), "Stone"))
        o.append(cyl("col", (1.55 * sx, 0, 1.55), 0.13, 0.12, 2.7, "Lacquer", 10))
        o.append(box("brace", (1.25 * sx, 0, 2.55), (0.55, 0.16, 0.16), "DarkWood", rot_y=-sx * 0.5))
    o.append(box("beam1", (0, 0, 2.72), (3.9, 0.22, 0.20), "DarkWood"))
    o.append(box("beam2", (0, 0, 3.02), (3.6, 0.18, 0.16), "Lacquer"))
    o.append(box("plaque", (0, -0.15, 2.87), (1.05, 0.06, 0.26), "Gold"))
    o += dougong("dg", 3.6, 0.9, 3.12, 4)
    r = roof("roofMain", 4.1, 1.0, 0.55, 0.38, 0.24)
    for ob in r:
        ob.location.z += 3.2
    o += r
    # techos laterales
    for sx in (-1, 1):
        r2 = roof("roofSide", 1.0, 0.9, 0.3, 0.25, 0.14)
        for ob in r2:
            ob.location.x += sx * 1.55
            ob.location.z += 2.45
        o += r2
    for sx in (-1, 1):
        o += lantern("lant", sx * 1.05, 0, 2.30, 0.13)
        o += banner("band", sx * 1.55, -0.35, 1.65)
    body = join(o, "Paifang"); uv_unwrap(body)
    preview("Paifang", 6.2, 2.0)
    path = os.path.join(OUT, "SM_Building_Paifang.fbx")
    export_static(path, body)
    register("SM_Building_Paifang", path, tris_of(body), kind="building")


def build_ding():
    bc.reset_scene()
    o = []
    # cuerpo troncocÃ³nico
    o.append(cyl("bodyL", (0, 0, 0.62), 0.42, 0.50, 0.42, "Bronze", 16))
    o.append(cyl("bodyU", (0, 0, 0.92), 0.50, 0.46, 0.22, "Bronze", 16))
    o.append(cyl("rim", (0, 0, 1.06), 0.48, 0.48, 0.08, "Bronze", 16))
    o.append(cyl("rimLip", (0, 0, 1.11), 0.51, 0.51, 0.03, "Gold", 16))
    # banda taotie (relieve)
    for i in range(8):
        a = 2 * math.pi * i / 8
        x, y = math.cos(a) * 0.50, math.sin(a) * 0.50
        o.append(box("relief", (x, y, 0.86), (0.16, 0.06, 0.20), "Bronze",
                     rot_z=a + math.pi / 2))
    for sx in (-1, 1):
        o.append(ball("eye", (0.16 * sx, -0.50, 0.86), 0.045, "Gold", subdiv=1))
    # patas
    for i in range(3):
        a = math.radians(90 + i * 120)
        x, y = math.cos(a) * 0.30, math.sin(a) * 0.30
        o.append(cyl("leg", (x, y, 0.24), 0.055, 0.075, 0.48, "Bronze", 8))
        o.append(cyl("hoof", (x, y, 0.03), 0.085, 0.075, 0.06, "Bronze", 8))
    # asas
    for sx in (-1, 1):
        o.append(box("handle", (0.46 * sx, 0, 1.28), (0.10, 0.10, 0.30), "Bronze", rot_y=sx * 0.12))
        o.append(box("handleTop", (0.44 * sx, 0, 1.43), (0.22, 0.10, 0.08), "Bronze"))
    body = join(o, "Ding"); uv_unwrap(body)
    preview("Ding", 2.4, 0.75)
    path = os.path.join(OUT, "SM_Prop_Ding.fbx")
    export_static(path, body)
    register("SM_Prop_Ding", path, tris_of(body), kind="prop")


def build_lantern():
    bc.reset_scene()
    o = []
    o.append(cyl("base", (0, 0, 0.09), 0.17, 0.15, 0.18, "Stone", 10))
    o.append(cyl("pole", (0, 0, 1.0), 0.05, 0.04, 1.75, "DarkWood", 8))
    o.append(box("arm", (0.28, 0, 1.80), (0.65, 0.07, 0.07), "DarkWood"))
    o.append(box("brace", (0.15, 0, 1.62), (0.45, 0.05, 0.05), "DarkWood", rot_y=-0.6))
    o.append(cyl("hook", (0.56, 0, 1.72), 0.02, 0.02, 0.10, "Gold", 6))
    o += lantern("lantern", 0.56, 0, 1.52, 0.19)
    o += banner("flag", 0.56, -0.20, 1.45, 0.20, 0.42)
    body = join(o, "Lantern"); uv_unwrap(body)
    preview("Lantern", 3.0, 1.1)
    path = os.path.join(OUT, "SM_Prop_Lantern.fbx")
    export_static(path, body)
    register("SM_Prop_Lantern", path, tris_of(body), kind="prop")


# ---------------------------------------------------------------------------
# PERSONAJES (nombres de hueso estilo Unity Humanoid)
# ---------------------------------------------------------------------------

HUMANOID_BONES = [
    ("root",       (0.0, 0.0, 0.02), (0.0, 0.0, 0.16), None),
    ("Hips",       (0.0, 0.0, 0.92), (0.0, 0.0, 1.04), "root"),
    ("Spine",      (0.0, 0.0, 1.04), (0.0, 0.0, 1.26), "Hips"),
    ("Chest",      (0.0, 0.0, 1.26), (0.0, 0.0, 1.44), "Spine"),
    ("Neck",       (0.0, 0.0, 1.44), (0.0, 0.0, 1.53), "Chest"),
    ("Head",       (0.0, 0.0, 1.53), (0.0, 0.0, 1.78), "Neck"),
    ("LeftShoulder", (0.05, 0.0, 1.42), (0.24, 0.0, 1.42), "Chest"),
    ("LeftUpperArm", (0.24, 0.0, 1.42), (0.24, 0.0, 1.15), "LeftShoulder"),
    ("LeftLowerArm", (0.24, 0.0, 1.15), (0.24, 0.0, 0.90), "LeftUpperArm"),
    ("LeftHand",     (0.24, 0.0, 0.90), (0.24, 0.0, 0.80), "LeftLowerArm"),
    ("RightShoulder", (-0.05, 0.0, 1.42), (-0.24, 0.0, 1.42), "Chest"),
    ("RightUpperArm", (-0.24, 0.0, 1.42), (-0.24, 0.0, 1.15), "RightShoulder"),
    ("RightLowerArm", (-0.24, 0.0, 1.15), (-0.24, 0.0, 0.90), "RightUpperArm"),
    ("RightHand",     (-0.24, 0.0, 0.90), (-0.24, 0.0, 0.80), "RightLowerArm"),
    ("LeftUpperLeg",  (0.11, 0.0, 0.92), (0.11, 0.0, 0.50), "Hips"),
    ("LeftLowerLeg",  (0.11, 0.0, 0.50), (0.11, 0.0, 0.09), "LeftUpperLeg"),
    ("LeftFoot",      (0.11, 0.0, 0.09), (0.11, -0.18, 0.04), "LeftLowerLeg"),
    ("LeftToes",      (0.11, -0.18, 0.04), (0.11, -0.28, 0.03), "LeftFoot"),
    ("RightUpperLeg", (-0.11, 0.0, 0.92), (-0.11, 0.0, 0.50), "Hips"),
    ("RightLowerLeg", (-0.11, 0.0, 0.50), (-0.11, 0.0, 0.09), "RightUpperLeg"),
    ("RightFoot",     (-0.11, 0.0, 0.09), (-0.11, -0.18, 0.04), "RightLowerLeg"),
    ("RightToes",     (-0.11, -0.18, 0.04), (-0.11, -0.28, 0.03), "RightFoot"),
]


def build_armature(name):
    arm = bpy.data.armatures.new(name)
    rig = bpy.data.objects.new(name, arm)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for bname, head, tail, parent in HUMANOID_BONES:
        b = arm.edit_bones.new(bname)
        b.head = Vector(head)
        b.tail = Vector(tail)
        b.roll = 0.0
        made[bname] = b
    for bname, head, tail, parent in HUMANOID_BONES:
        if parent:
            made[bname].parent = made[parent]
            made[bname].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def key(rig, bone, frame, rot=None, loc=None):
    pb = rig.pose.bones[bone]
    pb.rotation_mode = "XYZ"
    if rot is not None:
        pb.rotation_euler = Euler(rot, "XYZ")
        pb.keyframe_insert("rotation_euler", frame=frame)
    if loc is not None:
        pb.location = Vector(loc)
        pb.keyframe_insert("location", frame=frame)


def new_action(rig, name):
    if rig.animation_data is None:
        rig.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data.action = act
    return act


def anim_idle(rig):
    a = new_action(rig, "Idle")
    for f, lean, bob in [(1, 0.0, 0.0), (15, -0.02, 0.012), (30, 0.0, 0.0), (45, 0.02, 0.012), (60, 0.0, 0.0)]:
        key(rig, "Spine", f, rot=(lean, 0, 0))
        key(rig, "Chest", f, rot=(lean * 0.6, 0, 0))
        key(rig, "Head", f, rot=(-lean * 0.5, 0, lean * 2))
        key(rig, "LeftUpperArm", f, rot=(0, 0, -0.10 - lean))
        key(rig, "RightUpperArm", f, rot=(0, 0, 0.10 + lean))
        key(rig, "LeftLowerArm", f, rot=(-0.15, 0, 0))
        key(rig, "RightLowerArm", f, rot=(-0.15, 0, 0))
        key(rig, "Hips", f, loc=(0, 0, bob))
    return a


def walk_cycle(rig, name, start, length, amp, arm_amp, bob, knee, lean):
    a = new_action(rig, name)
    q = length / 4.0
    for i in range(5):
        f = start + i * q
        t = i / 4.0
        swing = math.sin(2 * math.pi * t) * amp
        arm = math.sin(2 * math.pi * t) * arm_amp
        key(rig, "LeftUpperLeg", f, rot=(swing, 0, 0))
        key(rig, "RightUpperLeg", f, rot=(-swing, 0, 0))
        key(rig, "LeftLowerLeg", f, rot=(-max(0.0, swing) * knee - 0.05, 0, 0))
        key(rig, "RightLowerLeg", f, rot=(-max(0.0, -swing) * knee - 0.05, 0, 0))
        key(rig, "LeftFoot", f, rot=(-swing * 0.35, 0, 0))
        key(rig, "RightFoot", f, rot=(swing * 0.35, 0, 0))
        key(rig, "LeftUpperArm", f, rot=(-arm, 0, -0.08))
        key(rig, "RightUpperArm", f, rot=(arm, 0, 0.08))
        key(rig, "LeftLowerArm", f, rot=(-arm * 0.5 - 0.15, 0, 0))
        key(rig, "RightLowerArm", f, rot=(arm * 0.5 - 0.15, 0, 0))
        key(rig, "Hips", f, rot=(0, swing * 0.10, 0), loc=(0, 0, bob * math.sin(4 * math.pi * t)))
        key(rig, "Spine", f, rot=(lean, 0, -swing * 0.12))
        key(rig, "Head", f, rot=(-lean * 0.6, 0, swing * 0.06))
    return a


def anim_walk(rig):
    return walk_cycle(rig, "Walk", 1, 32, math.radians(26), math.radians(22), 0.025, 1.1, math.radians(3))


def anim_run(rig):
    return walk_cycle(rig, "Run", 1, 20, math.radians(42), math.radians(38), 0.05, 1.6, math.radians(12))


def anim_work(rig):
    a = new_action(rig, "Work")
    beats = [(1, 0.10, -1.90, -0.55, 0.0), (5, 0.20, -0.55, -0.15, -0.03),
             (8, 0.22, -0.30, -0.05, -0.04), (12, 0.12, -1.30, -0.40, 0.0),
             (18, 0.10, -1.90, -0.55, 0.0), (22, 0.20, -0.55, -0.15, -0.03),
             (25, 0.22, -0.30, -0.05, -0.04), (30, 0.12, -1.30, -0.40, 0.0),
             (36, 0.10, -1.90, -0.55, 0.0), (40, 0.20, -0.55, -0.15, -0.03),
             (44, 0.14, -1.20, -0.35, 0.0)]
    for f, lean, arm, elbow, bob in beats:
        key(rig, "Spine", f, rot=(lean, 0, 0))
        key(rig, "Chest", f, rot=(lean * 0.5, 0, -0.10))
        key(rig, "Head", f, rot=(lean * 0.8, 0, 0))
        key(rig, "RightUpperArm", f, rot=(arm, 0, 0.18))
        key(rig, "RightLowerArm", f, rot=(elbow, 0, 0))
        key(rig, "LeftUpperArm", f, rot=(-0.55, 0, -0.12))
        key(rig, "LeftLowerArm", f, rot=(-0.75, 0, 0))
        key(rig, "Hips", f, loc=(0, 0, bob))
    return a


def anim_wave(rig):
    a = new_action(rig, "Wave")
    for f, lift, wave in [(1, 0.0, 0.0), (8, 1.95, 0.0), (14, 2.15, 0.30), (20, 2.15, -0.30),
                          (26, 2.15, 0.30), (32, 1.35, 0.0), (40, 0.0, 0.0)]:
        key(rig, "RightUpperArm", f, rot=(-0.45, 0, lift))
        key(rig, "RightLowerArm", f, rot=(-0.35, 0, wave * 0.8))
        key(rig, "RightHand", f, rot=(0, 0, wave))
        key(rig, "LeftUpperArm", f, rot=(0, 0, -0.10))
        key(rig, "Head", f, rot=(0, wave * 0.2, 0))
        key(rig, "Chest", f, rot=(0, 0, wave * 0.10))
    return a


def anim_bow(rig):
    """Reverencia china (hands together, inclinaciÃ³n)."""
    a = new_action(rig, "Bow")
    poses = [(1, 0.0, 0.0, 0.0), (10, 0.30, 0.9, 0.35), (20, 0.55, 1.15, 0.55),
             (30, 0.55, 1.15, 0.55), (42, 0.10, 0.35, 0.15), (52, 0.0, 0.0, 0.0)]
    for f, lean, arm, elbow in poses:
        key(rig, "Spine", f, rot=(lean, 0, 0))
        key(rig, "Chest", f, rot=(lean * 0.6, 0, 0))
        key(rig, "Head", f, rot=(lean * 0.5, 0, 0))
        key(rig, "LeftUpperArm", f, rot=(-arm, 0, -0.55))
        key(rig, "RightUpperArm", f, rot=(-arm, 0, 0.55))
        key(rig, "LeftLowerArm", f, rot=(-elbow, 0, 0))
        key(rig, "RightLowerArm", f, rot=(-elbow, 0, 0))
    return a


def anim_cheer(rig):
    a = new_action(rig, "Cheer")
    for f, lift, jump in [(1, 0.0, 0.0), (8, 2.6, 0.0), (14, 2.9, 0.06), (20, 2.5, 0.0),
                          (26, 2.9, 0.06), (32, 2.5, 0.0), (40, 0.0, 0.0)]:
        key(rig, "LeftUpperArm", f, rot=(0, 0, -lift))
        key(rig, "RightUpperArm", f, rot=(0, 0, lift))
        key(rig, "LeftLowerArm", f, rot=(-0.3, 0, 0))
        key(rig, "RightLowerArm", f, rot=(-0.3, 0, 0))
        key(rig, "Spine", f, rot=(-0.10, 0, 0))
        key(rig, "Head", f, rot=(-0.15, 0, 0))
        key(rig, "Hips", f, loc=(0, 0, jump))
    return a


def anim_gather(rig):
    a = new_action(rig, "Gather")
    poses = [(1, 0.0, 0.0, 0.0), (12, 0.55, -1.2, -0.4), (20, 0.75, -1.5, -0.5),
             (28, 0.55, -1.2, -0.4), (36, 0.0, 0.0, 0.0)]
    for f, lean, arm, elbow in poses:
        key(rig, "Spine", f, rot=(lean, 0, 0))
        key(rig, "Head", f, rot=(lean * 0.8, 0, 0))
        key(rig, "RightUpperArm", f, rot=(arm, 0, 0.15))
        key(rig, "RightLowerArm", f, rot=(elbow, 0, 0))
        key(rig, "LeftUpperArm", f, rot=(arm * 0.8, 0, -0.15))
        key(rig, "LeftLowerArm", f, rot=(elbow * 0.8, 0, 0))
        key(rig, "LeftUpperLeg", f, rot=(-lean * 0.4, 0, 0))
        key(rig, "RightUpperLeg", f, rot=(-lean * 0.4, 0, 0))
    return a


def assemble(name, parts, rig_name):
    for obj, bone in parts:
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    body = join([p[0] for p in parts], name + "Body")
    rig = build_armature(rig_name)
    body.parent = rig
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    mod.use_vertex_groups = True
    return rig, body


def face_parts(parts, hm=1.0):
    for sx in (-1, 1):
        parts.append((box("eye", (0.055 * sx * hm, -0.128 * hm, 1.665), (0.03, 0.02, 0.04), "Ink"), "Head"))
        parts.append((box("brow", (0.058 * sx * hm, -0.132 * hm, 1.700), (0.05, 0.02, 0.014), "Hair"), "Head"))
    parts.append((box("nose", (0, -0.152 * hm, 1.632), (0.03, 0.03, 0.03), "Skin"), "Head"))
    parts.append((box("mouth", (0, -0.142 * hm, 1.578), (0.06, 0.02, 0.016), "Ink"), "Head"))


def build_sabio():
    bc.reset_scene()
    p = []
    # tÃºnica en capas
    p.append((cyl("skirtLow", (0, 0, 0.52), 0.30, 0.24, 0.55, "Robe", 14), "Hips"))
    p.append((cyl("skirtUp", (0, 0, 0.92), 0.24, 0.22, 0.26, "Robe", 14), "Spine"))
    p.append((box("chest", (0, 0, 1.30), (0.42, 0.26, 0.42), "Robe"), "Chest"))
    # cuello cruzado (solapas)
    p.append((box("lapelL", (0, -0.145, 1.42), (0.16, 0.02, 0.26), "Lime", rot_z=0.45), "Chest"))
    p.append((box("lapelR", (0, -0.145, 1.42), (0.16, 0.02, 0.26), "Lime", rot_z=-0.45), "Chest"))
    p.append((box("belt", (0, 0, 1.06), (0.46, 0.30, 0.10), "Gold"), "Spine"))
    p.append((cyl("jade", (0, -0.16, 1.00), 0.06, 0.06, 0.02, "Jade", 10), "Spine"))
    p.append((cyl("neck", (0, 0, 1.50), 0.05, 0.05, 0.09, "Skin", 8), "Neck"))
    p.append((ball("head", (0, 0, 1.645), 0.15, "Skin", subdiv=2, scale=(1, 0.96, 1.02)), "Head"))
    p.append((ball("hair", (0, 0.02, 1.70), 0.156, "Hair", subdiv=2, scale=(1.02, 1.0, 0.62)), "Head"))
    p.append((ball("bun", (0, 0.10, 1.79), 0.062, "Hair", subdiv=2), "Head"))
    p.append((cyl("pin", (0, 0.10, 1.80), 0.008, 0.008, 0.20, "Gold", 6, rot_x=math.pi / 2), "Head"))
    p.append((box("beard", (0, -0.13, 1.50), (0.09, 0.05, 0.26), "Hair"), "Head"))
    face_parts(p)
    for sx, side in ((1, "L"), (-1, "R")):
        p.append((ball("shoulder." + side, (0.215 * sx, 0, 1.40), 0.085, "Robe", subdiv=2),
                  "LeftShoulder" if sx > 0 else "RightShoulder"))
        p.append((box("sleeve." + side, (0.27 * sx, 0, 1.20), (0.17, 0.24, 0.42), "Robe"),
                  "LeftUpperArm" if sx > 0 else "RightUpperArm"))
        p.append((box("cuff." + side, (0.27 * sx, 0, 0.98), (0.20, 0.26, 0.10), "Lime"),
                  "LeftUpperArm" if sx > 0 else "RightUpperArm"))
        p.append((cyl("forearm." + side, (0.255 * sx, 0, 1.02), 0.042, 0.038, 0.24, "Skin", 8),
                  "LeftLowerArm" if sx > 0 else "RightLowerArm"))
        p.append((ball("hand." + side, (0.255 * sx, 0, 0.885), 0.055, "Skin", subdiv=2),
                  "LeftHand" if sx > 0 else "RightHand"))
        p.append((cyl("thigh." + side, (0.105 * sx, 0, 0.72), 0.075, 0.065, 0.40, "Trousers", 8),
                  "LeftUpperLeg" if sx > 0 else "RightUpperLeg"))
        p.append((cyl("shin." + side, (0.105 * sx, 0, 0.30), 0.062, 0.055, 0.40, "Trousers", 8),
                  "LeftLowerLeg" if sx > 0 else "RightLowerLeg"))
        p.append((box("boot." + side, (0.105 * sx, -0.05, 0.07), (0.13, 0.28, 0.14), "Boots"),
                  "LeftFoot" if sx > 0 else "RightFoot"))
    rig, body = assemble("Sabio", p, "SabioRig"); uv_unwrap(body)
    acts = [anim_idle(rig), anim_walk(rig), anim_run(rig), anim_work(rig), anim_wave(rig),
            anim_bow(rig), anim_cheer(rig), anim_gather(rig)]
    rig.animation_data.action = acts[0]
    preview_char("Sabio", rig, "Walk", 9)
    path = os.path.join(OUT, "SK_Sabio.fbx")
    bc.export_fbx(path, rig, body)
    register("SK_Sabio", path, tris_of(body), len(rig.data.bones),
             [a.name for a in acts], ["Idle", "Walk", "Run", "Work"], kind="character")


def build_guardia():
    bc.reset_scene()
    p = []
    # armadura lamelar: filas de placas
    p.append((box("chest", (0, 0, 1.34), (0.46, 0.28, 0.36), "Armor"), "Chest"))
    for row in range(3):
        for i in range(4):
            x = -0.17 + i * 0.115
            p.append((box("plate", (x, -0.155, 1.20 + row * 0.09), (0.10, 0.03, 0.075), "Gold"), "Chest"))
    p.append((box("abdomen", (0, 0, 1.10), (0.38, 0.24, 0.20), "Armor"), "Spine"))
    p.append((box("belt", (0, 0, 0.99), (0.42, 0.28, 0.08), "Lacquer"), "Hips"))
    # faldÃ³n de placas
    for i in range(5):
        x = -0.20 + i * 0.10
        p.append((box("skirt", (x, -0.13, 0.86), (0.09, 0.04, 0.22), "Armor", rot_x=0.12), "Hips"))
    p.append((ball("head", (0, 0, 1.62), 0.14, "Skin", subdiv=2), "Head"))
    p.append((ball("helmet", (0, 0.01, 1.71), 0.172, "Armor", subdiv=2, scale=(1.0, 1.0, 0.78)), "Head"))
    p.append((cyl("brim", (0, 0.01, 1.60), 0.23, 0.23, 0.05, "Gold", 12), "Head"))
    p.append((box("neckGuard", (0, 0.14, 1.55), (0.28, 0.10, 0.14), "Armor"), "Head"))
    p.append((box("plume", (0, 0.05, 1.90), (0.05, 0.05, 0.18), "Lacquer"), "Head"))
    for sx in (-1, 1):
        p.append((box("eye", (0.05 * sx, -0.12, 1.635), (0.03, 0.02, 0.03), "Ink"), "Head"))
    for sx, side in ((1, "L"), (-1, "R")):
        sh = "LeftShoulder" if sx > 0 else "RightShoulder"
        ua = "LeftUpperArm" if sx > 0 else "RightUpperArm"
        la = "LeftLowerArm" if sx > 0 else "RightLowerArm"
        ha = "LeftHand" if sx > 0 else "RightHand"
        ul = "LeftUpperLeg" if sx > 0 else "RightUpperLeg"
        ll = "LeftLowerLeg" if sx > 0 else "RightLowerLeg"
        ft = "LeftFoot" if sx > 0 else "RightFoot"
        p.append((box("pauldron." + side, (0.27 * sx, 0, 1.44), (0.22, 0.24, 0.10), "Armor"), sh))
        p.append((box("pauldron2." + side, (0.28 * sx, 0, 1.37), (0.18, 0.20, 0.08), "Gold"), sh))
        p.append((cyl("arm." + side, (0.235 * sx, 0, 1.27), 0.055, 0.05, 0.24, "Armor", 8), ua))
        p.append((cyl("forearm." + side, (0.235 * sx, 0, 1.03), 0.045, 0.042, 0.22, "Skin", 8), la))
        p.append((ball("hand." + side, (0.235 * sx, 0, 0.90), 0.055, "Skin", subdiv=2), ha))
        p.append((cyl("thigh." + side, (0.105 * sx, 0, 0.72), 0.072, 0.062, 0.40, "Trousers", 8), ul))
        p.append((cyl("shin." + side, (0.105 * sx, 0, 0.30), 0.062, 0.052, 0.40, "Trousers", 8), ll))
        p.append((box("shinGuard." + side, (0.105 * sx, -0.05, 0.26), (0.11, 0.06, 0.26), "Armor"), ll))
        p.append((box("boot." + side, (0.105 * sx, -0.05, 0.07), (0.13, 0.28, 0.14), "Boots"), ft))
    # lanza con moharra y borla
    p.append((cyl("spear", (-0.235, 0, 1.10), 0.022, 0.022, 2.05, "DarkWood", 6), "RightHand"))
    p.append((box("blade", (-0.235, 0, 2.17), (0.07, 0.02, 0.24), "Gold"), "RightHand"))
    p.append((ball("butt", (-0.235, 0, 0.07), 0.035, "Gold", subdiv=1), "RightHand"))
    p.append((box("tassel", (-0.235, 0, 2.02), (0.05, 0.05, 0.10), "Lacquer"), "RightHand"))
    rig, body = assemble("Guardia", p, "GuardiaRig"); uv_unwrap(body)
    acts = [anim_idle(rig), anim_walk(rig), anim_run(rig), anim_work(rig), anim_wave(rig),
            anim_bow(rig), anim_cheer(rig), anim_gather(rig)]
    rig.animation_data.action = acts[0]
    preview_char("Guardia", rig, "Walk", 9)
    path = os.path.join(OUT, "SK_Guardia.fbx")
    bc.export_fbx(path, rig, body)
    register("SK_Guardia", path, tris_of(body), len(rig.data.bones),
             [a.name for a in acts], ["Idle", "Walk", "Run", "Work"], kind="character")


def preview_char(name, rig, act_name, frame):
    act = next((a for a in bpy.data.actions if act_name.lower() in a.name.lower()), None)
    if act:
        rig.animation_data.action = act
        bpy.context.scene.frame_set(frame)
    preview(name, size=3.1, z=1.05)


# ---------------------------------------------------------------------------

def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    print("[CHINA2] inicio", flush=True)
    build_casa()
    build_pagoda()
    build_paifang()
    build_ding()
    build_lantern()
    build_market()
    build_temple()
    build_wall()
    build_sabio()
    build_guardia()
    index = {
        "asset_count": len(REGISTRY),
        "units": "metros (1 u = 1 m)",
        "axis": "Blender Z-up, frente -Y; FBX export Forward -Z / Up Y",
        "palette": {
            "rojo_laca": "#B33B2E", "dorado": "#D9A441", "verde_jade": "#3E7C59",
            "madera_oscura": "#4A3728", "techo_azul_gris": "#37474F", "blanco_cal": "#EDE6D6",
        },
        "assets": REGISTRY,
    }
    with open(os.path.join(STAGE, "index.json"), "w", encoding="utf-8") as fh:
        json.dump(index, fh, ensure_ascii=False, indent=2)
    print("[CHINA2] fin: %d assets" % len(REGISTRY), flush=True)


# ---------------------------------------------------------------------------
# tipologías nuevas: mercado, templo, muralla
# ---------------------------------------------------------------------------

def build_market():
    bc.reset_scene()
    o = []
    o.append(box("base", (0, 0, 0.10), (3.1, 0.20, 2.3), "Stone"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(cyl("post", (1.35 * sx, 0.95 * sy, 1.10), 0.06, 0.055, 2.0, "DarkWood", 8))
            o.append(box("postBase", (1.35 * sx, 0.95 * sy, 0.22), (0.22, 0.22, 0.12), "Stone"))
    o.append(box("beamN", (0, 0.95, 2.10), (2.9, 0.12, 0.12), "DarkWood"))
    o.append(box("beamS", (0, -0.95, 2.10), (2.9, 0.12, 0.12), "DarkWood"))
    o.append(box("beamE", (1.35, 0, 2.10), (0.12, 2.0, 0.12), "DarkWood"))
    o.append(box("beamW", (-1.35, 0, 2.10), (0.12, 2.0, 0.12), "DarkWood"))
    r = roof("awning", 3.0, 2.2, 0.5, 0.28, 0.14, mat="Canvas", ridge_mat="CanvasRed")
    for ob in r:
        ob.location.z += 2.16
    o += r
    for sx in (-0.75, 0.75):
        o.append(box("counter", (sx, -0.25, 0.85), (0.9, 0.5, 0.08), "DarkWood"))
        o.append(box("counterLegA", (sx - 0.35, -0.25, 0.60), (0.08, 0.45, 0.45), "DarkWood"))
        o.append(box("counterLegB", (sx + 0.35, -0.25, 0.60), (0.08, 0.45, 0.45), "DarkWood"))
        for k in range(3):
            o.append(ball("goods", (sx - 0.25 + k * 0.25, -0.25, 0.93), 0.06,
                          "Lacquer" if k % 2 else "Gold", subdiv=1))
    o.append(box("crate1", (1.05, 0.55, 0.30), (0.40, 0.40, 0.40), "DarkWood"))
    o.append(box("crate2", (0.62, 0.75, 0.26), (0.34, 0.34, 0.32), "DarkWood"))
    o.append(cyl("jar", (-1.05, 0.60, 0.35), 0.18, 0.14, 0.50, "Bronze", 10))
    for sx in (-1, 1):
        o += lantern("lant", 1.15 * sx, -1.05, 1.75, 0.11)
    o.append(box("sign", (0, -1.12, 2.00), (0.9, 0.05, 0.28), "Lacquer"))
    body = join(o, "Market")
    uv_unwrap(body)
    preview("Mercado", 4.2, 1.4)
    path = os.path.join(OUT, "SM_Building_Market.fbx")
    export_static(path, body)
    register("SM_Building_Market", path, tris_of(body), kind="building")


def build_temple():
    bc.reset_scene()
    o = []
    o.append(box("base", (0, 0, 0.14), (3.4, 0.28, 3.0), "Stone"))
    o.append(box("base2", (0, 0, 0.28), (3.1, 0.10, 2.7), "Stone"))
    for k in range(3):
        o.append(box("step", (0, -1.45, 0.10 + k * 0.10), (1.6 - k * 0.15, 0.30, 0.10), "Stone"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(cyl("col", (1.10 * sx, 1.00 * sy, 1.35), 0.10, 0.09, 2.10, "Lacquer", 10))
    o.append(box("wallBack", (0, 1.02, 1.30), (2.30, 0.10, 1.90), "Lime"))
    o.append(box("wallL", (-1.12, 0, 1.30), (0.10, 2.10, 1.90), "Lime"))
    o.append(box("wallR", (1.12, 0, 1.30), (0.10, 2.10, 1.90), "Lime"))
    o.append(box("doorFrame", (0, -1.05, 1.05), (1.00, 0.10, 1.50), "Lacquer"))
    o.append(box("door", (0, -1.10, 0.85), (0.80, 0.06, 1.10), "DarkWood"))
    o += lattice_window("winL", -0.70, -1.05, 1.45, 0.35, 0.40)
    o += lattice_window("winR", 0.70, -1.05, 1.45, 0.35, 0.40)
    o.append(box("altar", (0, 0.55, 0.85), (1.20, 0.50, 0.90), "DarkWood"))
    o.append(cyl("burner", (0, 0.55, 1.40), 0.18, 0.16, 0.22, "Bronze", 10))
    o.append(cyl("lid", (0, 0.55, 1.55), 0.16, 0.10, 0.10, "Gold", 10))
    o += dougong("dg", 2.5, 2.2, 2.35, 4)
    r1 = roof("roof1", 2.6, 2.3, 0.60, 0.40, 0.20)
    for ob in r1:
        ob.location.z += 2.40
    o += r1
    r2 = roof("roof2", 1.6, 1.4, 0.45, 0.30, 0.16)
    for ob in r2:
        ob.location.z += 3.15
    o += r2
    for sx in (-1, 1):
        o += banner("band", 1.10 * sx, -1.15, 1.60, 0.24, 0.80)
        o += lantern("lant", 0.85 * sx, -1.15, 2.10, 0.12)
    body = join(o, "Temple")
    uv_unwrap(body)
    preview("Templo", 4.8, 1.6)
    path = os.path.join(OUT, "SM_Building_Temple.fbx")
    export_static(path, body)
    register("SM_Building_Temple", path, tris_of(body), kind="building")


def build_wall():
    bc.reset_scene()
    o = []
    o.append(box("gateL", (-1.20, 0, 1.00), (1.20, 1.25, 2.00), "Stone"))
    o.append(box("gateR", (1.20, 0, 1.00), (1.20, 1.25, 2.00), "Stone"))
    o.append(box("lintel", (0, 0, 2.10), (2.40, 1.30, 0.35), "Stone"))
    o.append(box("door", (0, 0, 0.90), (1.60, 0.10, 1.70), "DarkWood"))
    o.append(box("walk", (0, 0, 2.06), (4.00, 1.20, 0.12), "Stone"))
    for i in range(6):
        x = -1.70 + i * 0.68
        o.append(box("merlonN", (x, 0.50, 2.35), (0.42, 0.50, 0.50), "Stone"))
        o.append(box("merlonS", (x, -0.50, 2.35), (0.42, 0.50, 0.50), "Stone"))
    for sx in (-1, 1):
        o.append(box("wing", (2.55 * sx, 0, 1.00), (1.10, 1.20, 2.00), "Stone"))
        o.append(box("wingWalk", (2.55 * sx, 0, 2.06), (1.10, 1.20, 0.12), "Stone"))
    body = join(o, "Wall")
    uv_unwrap(body)
    preview("Muralla", 5.6, 1.7)
    path = os.path.join(OUT, "SM_Building_Wall.fbx")
    export_static(path, body)
    register("SM_Building_Wall", path, tris_of(body), kind="building")

if __name__ == "__main__":
    main()
