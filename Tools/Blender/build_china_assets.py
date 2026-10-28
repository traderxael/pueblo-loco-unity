import os
from pathlib import Path
"""
PUEBLO LOCO â€” Assets temÃ¡ticos de la China antigua (Blender headless).

Genera, a partir de las referencias de la presentaciÃ³n de Canva:
  - CasaHan.fbx   casa del valle del rÃ­o Amarillo (techo curvo)
  - Pagoda.fbx    pagoda de tres cuerpos
  - Ding.fbx      vasija ritual de bronce
  - FarolHan.fbx  farol colgante
  - Puerta.fbx    puerta ceremonial (paifang)
  - Sabio.fbx     personaje riggeado (20 huesos) + Idle/Walk/Run/Work/Wave
  - Guardia.fbx   personaje riggeado con armadura y lanza + las 5 animaciones

Reutiliza helpers y el esqueleto de build_characters.py.

Uso:
  blender.exe --background --factory-startup --offline-mode --python build_china_assets.py
Salida: staging del workspace (Blender no puede escribir en el proyecto).
"""

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
STAGE = os.environ.get("PUEBLO_STAGE_CHINA", str(Path(PROJECT) / "_stage_china"))
OUT = os.path.join(STAGE, "assets")
PREV = os.path.join(STAGE, "previews")

bc.MAT_COLORS.update({
    "Wall": (0.82, 0.62, 0.38, 1.0),
    "RoofTiles": (0.16, 0.28, 0.22, 1.0),
    "Wood": (0.34, 0.19, 0.10, 1.0),
    "Red": (0.62, 0.12, 0.10, 1.0),
    "Gold": (0.85, 0.65, 0.18, 1.0),
    "Stone": (0.46, 0.45, 0.42, 1.0),
    "Bronze": (0.33, 0.47, 0.38, 1.0),
    "Paper": (0.92, 0.87, 0.72, 1.0),
    "Skin": (0.93, 0.72, 0.58, 1.0),
    "Robe": (0.45, 0.20, 0.45, 1.0),
    "Trim": (0.85, 0.65, 0.18, 1.0),
    "Hair": (0.12, 0.10, 0.10, 1.0),
    "Boots": (0.10, 0.09, 0.10, 1.0),
    "Armor": (0.55, 0.40, 0.16, 1.0),
    "Trousers": (0.22, 0.20, 0.30, 1.0),
})


# ---------------------------------------------------------------------------
# helpers de malla
# ---------------------------------------------------------------------------

def _finalize(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def mesh_obj(name, verts, faces, mat_name):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.validate()
    _finalize(mesh)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(bc.get_material(mat_name))
    return obj


def box(name, center, size, mat_name, rot_x=0.0):
    return bc.box(name, center, size, mat_name, rot_x)


def cylinder(name, center, radius, height, mat_name, segments=12, rot_x=0.0):
    return bc.cylinder(name, center, radius, height, mat_name, segments=segments, rot_x=rot_x)


def sphere(name, center, radius, mat_name, subdiv=1, scale=(1, 1, 1)):
    return bc.sphere(name, center, radius, mat_name, subdiv=subdiv, scale=scale)


def hip_roof(name, w, d, h, overhang, tip_lift, mat_name):
    """Techo a cuatro aguas con aleros levantados (estilo chino)."""
    c = w * 0.5 + overhang
    e = d * 0.5 + overhang
    # anillo del alero: esquinas levantadas
    ring0 = [(c, e, tip_lift), (-c, e, tip_lift), (-c, -e, tip_lift), (c, -e, tip_lift)]
    ring1 = [(w * 0.46, d * 0.46, h * 0.42), (-w * 0.46, d * 0.46, h * 0.42),
             (-w * 0.46, -d * 0.46, h * 0.42), (w * 0.46, -d * 0.46, h * 0.42)]
    ring2 = [(w * 0.24, d * 0.24, h * 0.72), (-w * 0.24, d * 0.24, h * 0.72),
             (-w * 0.24, -d * 0.24, h * 0.72), (w * 0.24, -d * 0.24, h * 0.72)]
    ridge = [(w * 0.10, 0.0, h), (-w * 0.10, 0.0, h)]

    verts = []
    verts.extend(ring0)
    verts.extend(ring1)
    verts.extend(ring2)
    verts.extend(ridge)
    verts.append((0.0, 0.0, 0.0))  # centro de la base (tapa inferior)
    center_idx = len(verts) - 1

    faces = []
    for i in range(4):
        j = (i + 1) % 4
        faces.append((i, j, 4 + j, 4 + i))
        faces.append((4 + i, 4 + j, 8 + j, 8 + i))
    # de ring2 al caballete (12 = +x, 13 = -x)
    faces.append((8, 9, 13, 12))
    faces.append((10, 11, 12, 13))
    faces.append((9, 10, 13))
    faces.append((11, 8, 12))
    # tapa inferior
    for i in range(4):
        j = (i + 1) % 4
        faces.append((center_idx, j, i))
    return mesh_obj(name, verts, faces, mat_name)


def join(objs, name):
    return bc.join_objects(objs, name)


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
    bpy.ops.object.camera_add(location=(size * 0.9, -size * 1.1, z + size * 0.55))
    cam = bpy.context.active_object
    direction = Vector((0.0, 0.0, z * 0.55)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = 50
    bpy.context.scene.camera = cam
    bpy.ops.object.light_add(type="SUN", rotation=(math.radians(55), 0, math.radians(35)))
    bpy.context.active_object.data.energy = 3.5
    scene = bpy.context.scene
    scene.render.resolution_x = 480
    scene.render.resolution_y = 480
    scene.render.filepath = os.path.join(PREV, name + ".png")
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------------------
# edificios y props
# ---------------------------------------------------------------------------

def build_casa_han():
    bc.reset_scene()
    objs = []
    objs.append(box("base", (0, 0, 0.08), (2.0, 0.14, 2.0), "Stone"))
    objs.append(box("muros", (0, 0, 0.78), (1.55, 1.32, 1.55), "Wall"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            objs.append(cylinder("poste", (0.72 * sx, 0.72 * sy, 0.78), 0.06, 1.4, "Wood", segments=8))
    objs.append(box("puerta", (0, -0.72, 0.55), (0.42, 0.08, 0.9), "Wood"))
    for sx in (-1, 1):
        objs.append(box("ventana_marco", (0.42 * sx, -0.72, 0.95), (0.34, 0.06, 0.34), "Wood"))
        objs.append(box("ventana_papel", (0.42 * sx, -0.735, 0.95), (0.26, 0.02, 0.26), "Paper"))
    roof = hip_roof("techo", 1.9, 1.9, 0.82, 0.36, 0.2, "RoofTiles")
    roof.location = (0, 0, 1.46)
    objs.append(roof)
    ridge = box("caballete", (0, 0, 2.32), (0.95, 0.1, 0.12), "Red")
    objs.append(ridge)
    body = join(objs, "CasaHan")
    preview("CasaHan", size=3.2, z=1.2)
    path = os.path.join(OUT, "CasaHan.fbx")
    export_static(path, body)
    print("[CHINA] CasaHan verts=%d size=%d" % (len(body.data.vertices), os.path.getsize(path)), flush=True)


def build_pagoda():
    bc.reset_scene()
    objs = []
    objs.append(box("base", (0, 0, 0.16), (2.3, 0.32, 2.3), "Stone"))
    tiers = [(1.55, 0.32, 1.15, "Red"), (1.25, 1.52, 0.95, "Wall"), (0.95, 2.52, 0.8, "Red")]
    for i, (w, z, h, mat) in enumerate(tiers):
        objs.append(box("cuerpo%d" % i, (0, 0, z + h * 0.5), (w, w, h), mat))
        objs.append(box("ventana%d" % i, (0, -w * 0.5 - 0.01, z + h * 0.45), (0.26, 0.06, 0.34), "Wood"))
        r = hip_roof("techo%d" % i, w + 0.25, w + 0.25, 0.4, 0.34, 0.16, "RoofTiles")
        r.location = (0, 0, z + h)
        objs.append(r)
    finial = cylinder("pinaculo", (0, 0, 4.05), 0.06, 0.55, "Gold", segments=8)
    objs.append(finial)
    objs.append(sphere("esfera", (0, 0, 4.36), 0.1, "Gold", subdiv=1))
    body = join(objs, "Pagoda")
    preview("Pagoda", size=6.2, z=2.4)
    path = os.path.join(OUT, "Pagoda.fbx")
    export_static(path, body)
    print("[CHINA] Pagoda verts=%d size=%d" % (len(body.data.vertices), os.path.getsize(path)), flush=True)


def build_ding():
    bc.reset_scene()
    objs = []
    for i in range(3):
        a = math.radians(90 + i * 120)
        x, y = math.cos(a) * 0.28, math.sin(a) * 0.28
        objs.append(cylinder("pata%d" % i, (x, y, 0.25), 0.07, 0.5, "Bronze", segments=8))
    objs.append(cylinder("cuerpo", (0, 0, 0.72), 0.44, 0.5, "Bronze", segments=16))
    objs.append(cylinder("borde", (0, 0, 0.98), 0.47, 0.09, "Bronze", segments=16))
    for sx in (-1, 1):
        objs.append(box("asa", (0.42 * sx, 0, 1.08), (0.09, 0.09, 0.24), "Bronze"))
    for sx in (-1, 1):
        objs.append(sphere("ojo", (0.16 * sx, -0.43, 0.76), 0.045, "Gold", subdiv=1))
    body = join(objs, "Ding")
    preview("Ding", size=1.9, z=0.7)
    path = os.path.join(OUT, "Ding.fbx")
    export_static(path, body)
    print("[CHINA] Ding verts=%d size=%d" % (len(body.data.vertices), os.path.getsize(path)), flush=True)


def build_farol():
    bc.reset_scene()
    objs = []
    objs.append(cylinder("base", (0, 0, 0.08), 0.16, 0.16, "Stone", segments=10))
    objs.append(cylinder("poste", (0, 0, 0.95), 0.045, 1.7, "Wood", segments=8))
    objs.append(box("brazo", (0.28, 0, 1.72), (0.62, 0.06, 0.06), "Wood"))
    objs.append(cylinder("aro", (0.55, 0, 1.62), 0.10, 0.04, "Gold", segments=8))
    objs.append(sphere("farol", (0.55, 0, 1.45), 0.16, "Red", subdiv=2, scale=(1, 1, 1.25)))
    objs.append(cylinder("tapon", (0.55, 0, 1.63), 0.06, 0.05, "Gold", segments=8))
    objs.append(cylinder("borla", (0.55, 0, 1.22), 0.02, 0.18, "Gold", segments=6))
    body = join(objs, "FarolHan")
    preview("FarolHan", size=2.8, z=1.1)
    path = os.path.join(OUT, "FarolHan.fbx")
    export_static(path, body)
    print("[CHINA] FarolHan verts=%d size=%d" % (len(body.data.vertices), os.path.getsize(path)), flush=True)


def build_puerta():
    bc.reset_scene()
    objs = []
    for sx in (-1, 1):
        objs.append(box("basamento", (1.35 * sx, 0, 0.12), (0.5, 0.5, 0.24), "Stone"))
        objs.append(cylinder("columna", (1.35 * sx, 0, 1.45), 0.13, 2.6, "Red", segments=10))
    objs.append(box("viga1", (0, 0, 2.62), (3.3, 0.2, 0.18), "Wood"))
    objs.append(box("viga2", (0, 0, 2.92), (3.1, 0.18, 0.16), "Red"))
    objs.append(box("placa", (0, -0.14, 2.77), (0.9, 0.06, 0.24), "Gold"))
    roof = hip_roof("techo", 3.5, 0.85, 0.5, 0.32, 0.2, "RoofTiles")
    roof.location = (0, 0, 2.98)
    objs.append(roof)
    for sx in (-1, 1):
        objs.append(sphere("farolillo", (0.9 * sx, 0, 2.2), 0.12, "Red", subdiv=2, scale=(1, 1, 1.2)))
        objs.append(cylinder("cuerda", (0.9 * sx, 0, 2.38), 0.012, 0.2, "Gold", segments=6))
    body = join(objs, "Puerta")
    preview("Puerta", size=4.8, z=1.9)
    path = os.path.join(OUT, "Puerta.fbx")
    export_static(path, body)
    print("[CHINA] Puerta verts=%d size=%d" % (len(body.data.vertices), os.path.getsize(path)), flush=True)


# ---------------------------------------------------------------------------
# personajes riggeados
# ---------------------------------------------------------------------------

def assemble(name, parts):
    for obj, bone in parts:
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    body = join([p[0] for p in parts], name + "Body")
    rig = bc.build_armature(name + "Rig")
    body.parent = rig
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    mod.use_vertex_groups = True
    return rig, body


def face_parts(parts):
    for sx in (-1, 1):
        parts.append((box("ojo", (0.055 * sx, -0.128, 1.665), (0.03, 0.02, 0.04), "Boots"), "head"))
    parts.append((box("nariz", (0, -0.152, 1.632), (0.03, 0.03, 0.03), "Skin"), "head"))
    parts.append((box("boca", (0, -0.142, 1.578), (0.06, 0.02, 0.016), "Boots"), "head"))


def build_sabio():
    bc.reset_scene()
    parts = []
    parts.append((box("torso", (0, 0, 1.30), (0.42, 0.26, 0.44), "Robe"), "spine"))
    # tunica hasta la rodilla para que se vean las piernas al caminar
    parts.append((cylinder("tunica", (0, 0, 0.84), 0.235, 0.50, "Robe", segments=12), "hips"))
    parts.append((box("faja", (0, 0, 1.07), (0.44, 0.28, 0.09), "Trim"), "spine"))
    parts.append((cylinder("cuello", (0, 0, 1.50), 0.05, 0.09, "Skin", segments=8), "neck"))
    parts.append((sphere("cabeza", (0, 0, 1.645), 0.15, "Skin", subdiv=2, scale=(1, 0.96, 1.02)), "head"))
    parts.append((sphere("pelo", (0, 0.02, 1.70), 0.155, "Hair", subdiv=2, scale=(1.02, 1.0, 0.6)), "head"))
    parts.append((sphere("rodete", (0, 0.10, 1.79), 0.06, "Hair", subdiv=1), "head"))
    parts.append((box("barba", (0, -0.13, 1.50), (0.09, 0.05, 0.26), "Hair"), "head"))
    face_parts(parts)
    for sx, side in ((1, "L"), (-1, "R")):
        parts.append((sphere("hombro." + side, (0.215 * sx, 0, 1.40), 0.08, "Robe", subdiv=1), "shoulder." + side))
        parts.append((box("manga." + side, (0.27 * sx, 0, 1.20), (0.17, 0.22, 0.42), "Robe"), "upperarm." + side))
        parts.append((cylinder("antebrazo." + side, (0.255 * sx, 0, 1.03), 0.042, 0.22, "Skin", segments=8), "forearm." + side))
        parts.append((sphere("mano." + side, (0.255 * sx, 0, 0.90), 0.055, "Skin", subdiv=1), "hand." + side))
        parts.append((cylinder("muslo." + side, (0.105 * sx, 0, 0.72), 0.072, 0.40, "Trousers", segments=8), "thigh." + side))
        parts.append((cylinder("pierna." + side, (0.105 * sx, 0, 0.30), 0.062, 0.40, "Trousers", segments=8), "shin." + side))
        parts.append((box("pie." + side, (0.105 * sx, -0.05, 0.06), (0.13, 0.26, 0.12), "Boots"), "foot." + side))
    rig, body = assemble("Sabio", parts)
    rig.animation_data_create()
    acts = [bc.anim_idle(rig), bc.anim_walk(rig), bc.anim_run(rig), bc.anim_work(rig), bc.anim_wave(rig)]
    for a in acts:
        a.use_fake_user = True
    rig.animation_data.action = acts[0]
    export_character("Sabio", rig, body, preview_action=("Walk", 9))


def build_guardia():
    bc.reset_scene()
    parts = []
    parts.append((box("pecho", (0, 0, 1.34), (0.46, 0.28, 0.40), "Armor"), "chest"))
    parts.append((box("abdomen", (0, 0, 1.10), (0.38, 0.24, 0.22), "Armor"), "spine"))
    parts.append((box("faldon", (0, 0, 0.94), (0.42, 0.30, 0.22), "Armor"), "hips"))
    parts.append((cylinder("cuello", (0, 0, 1.52), 0.05, 0.08, "Skin", segments=8), "neck"))
    parts.append((sphere("cabeza", (0, 0, 1.64), 0.14, "Skin", subdiv=2), "head"))
    parts.append((sphere("casco", (0, 0.01, 1.72), 0.17, "Armor", subdiv=2, scale=(1.0, 1.0, 0.72)), "head"))
    parts.append((cylinder("ala", (0, 0.01, 1.60), 0.22, 0.045, "Trim", segments=12), "head"))
    parts.append((box("penacho", (0, 0.06, 1.88), (0.05, 0.05, 0.14), "Red"), "head"))
    for sx in (-1, 1):
        parts.append((box("ojo", (0.05 * sx, -0.12, 1.645), (0.03, 0.02, 0.035), "Boots"), "head"))
    for sx, side in ((1, "L"), (-1, "R")):
        parts.append((box("hombrera." + side, (0.26 * sx, 0, 1.42), (0.20, 0.22, 0.12), "Armor"), "shoulder." + side))
        parts.append((cylinder("brazo." + side, (0.235 * sx, 0, 1.27), 0.055, 0.24, "Armor", segments=8), "upperarm." + side))
        parts.append((cylinder("antebrazo." + side, (0.235 * sx, 0, 1.03), 0.045, 0.22, "Skin", segments=8), "forearm." + side))
        parts.append((sphere("mano." + side, (0.235 * sx, 0, 0.90), 0.055, "Skin", subdiv=1), "hand." + side))
    for sx, side in ((1, "L"), (-1, "R")):
        parts.append((cylinder("muslo." + side, (0.105 * sx, 0, 0.72), 0.07, 0.40, "Boots", segments=8), "thigh." + side))
        parts.append((cylinder("pierna." + side, (0.105 * sx, 0, 0.30), 0.06, 0.40, "Boots", segments=8), "shin." + side))
        parts.append((box("bota." + side, (0.105 * sx, -0.05, 0.06), (0.13, 0.26, 0.12), "Boots"), "foot." + side))
    # lanza en la mano derecha
    parts.append((cylinder("asta", (-0.235, 0, 1.10), 0.022, 2.0, "Wood", segments=6), "hand.R"))
    parts.append((box("hoja", (-0.235, 0, 2.14), (0.06, 0.02, 0.2), "Trim"), "hand.R"))
    parts.append((sphere("pomo", (-0.235, 0, 0.08), 0.035, "Gold", subdiv=1), "hand.R"))
    rig, body = assemble("Guardia", parts)
    rig.animation_data_create()
    acts = [bc.anim_idle(rig), bc.anim_walk(rig), bc.anim_run(rig), bc.anim_work(rig), bc.anim_wave(rig)]
    for a in acts:
        a.use_fake_user = True
    rig.animation_data.action = acts[0]
    export_character("Guardia", rig, body, preview_action=("Walk", 9))


def export_character(name, rig, body, preview_action=None):
    # preview con la accion indicada
    if preview_action:
        act_name, frame = preview_action
        act = next((a for a in bpy.data.actions if act_name.lower() in a.name.lower()), None)
        if act:
            rig.animation_data.action = act
            bpy.context.scene.frame_set(frame)
    preview(name, size=2.4, z=1.0)
    path = os.path.join(OUT, name + ".fbx")
    bc.export_fbx(path, rig, body)
    print("[CHINA] %s verts=%d bones=%d actions=%d size=%d" % (
        name, len(body.data.vertices), len(rig.data.bones), len(bpy.data.actions), os.path.getsize(path)), flush=True)


# ---------------------------------------------------------------------------

def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    print("[CHINA] inicio", flush=True)
    build_casa_han()
    build_pagoda()
    build_ding()
    build_farol()
    build_puerta()
    build_sabio()
    build_guardia()
    print("[CHINA] fin", flush=True)


if __name__ == "__main__":
    main()

