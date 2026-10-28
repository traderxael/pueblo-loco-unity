import os
from pathlib import Path
"""
PUEBLO LOCO - Generador de personajes animados (Blender headless)

Crea personajes low-poly CON ESQUELETO (armature) y animaciones, listos para
exportar a Unity como FBX.

Uso:
    blender.exe --background --factory-startup --offline-mode --python build_characters.py

Salida:
    Assets/Art/Characters/Villager.fbx   (adulto)
    Assets/Art/Characters/Kid.fbx        (nene/a)
    Assets/Art/Characters/Elder.fbx      (abuelo/a)
    Tools/Blender/previews/*.png         (control visual)

Cada FBX trae el mesh skinned + armature + 5 acciones:
    Idle, Walk, Run, Work, Wave
Unity las importa como clips separados (take por accion).
"""

import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector, Euler

# ---------------------------------------------------------------------------
# rutas
# ---------------------------------------------------------------------------
# Blender corre con permisos restringidos: escribe en staging dentro del
# workspace y luego sync_characters.ps1 copia los resultados al proyecto.
PROJECT = str(Path(__file__).resolve().parents[2])
STAGE = os.environ.get("PUEBLO_STAGE", str(Path(PROJECT) / "_stage"))
OUT_DIR = os.path.join(STAGE, "characters")
PREV_DIR = os.path.join(STAGE, "previews")

SKIN = "Skin"
SHIRT = "Shirt"
PANTS = "Pants"
HAIR = "Hair"
SHOES = "Shoes"

MAT_COLORS = {
    SKIN:  (0.93, 0.72, 0.58, 1.0),
    SHIRT: (0.20, 0.55, 0.85, 1.0),
    PANTS: (0.18, 0.22, 0.38, 1.0),
    HAIR:  (0.22, 0.14, 0.10, 1.0),
    SHOES: (0.09, 0.09, 0.11, 1.0),
}

# ---------------------------------------------------------------------------
# utilidades de escena
# ---------------------------------------------------------------------------

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True


def get_material(name):
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = MAT_COLORS[name]
        bsdf.inputs["Roughness"].default_value = 0.75
    mat.diffuse_color = MAT_COLORS[name]
    return mat


def new_mesh_object(name, verts, faces, mat_name):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.validate()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(get_material(mat_name))
    return obj


def box(name, center, size, mat_name, rot_x=0.0):
    cx, cy, cz = center
    sx, sy, sz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
    verts = [(-sx, -sy, -sz), (sx, -sy, -sz), (sx, sy, -sz), (-sx, sy, -sz),
             (-sx, -sy, sz), (sx, -sy, sz), (sx, sy, sz), (-sx, sy, sz)]
    if rot_x:
        c, s = math.cos(rot_x), math.sin(rot_x)
        verts = [(x, y * c - z * s, y * s + z * c) for (x, y, z) in verts]
    verts = [(x + cx, y + cy, z + cz) for (x, y, z) in verts]
    faces = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    return new_mesh_object(name, verts, faces, mat_name)


def cylinder(name, center, radius, height, mat_name, segments=10, scale_z=1.0, rot_x=0.0):
    verts = []
    faces = []
    cx, cy, cz = center
    hz = height * 0.5 * scale_z
    for i in range(segments):
        a = 2.0 * math.pi * i / segments
        x = math.cos(a) * radius
        y = math.sin(a) * radius
        verts.append((x, y, -hz))
        verts.append((x, y, hz))
    for i in range(segments):
        a0 = i * 2
        a1 = ((i + 1) % segments) * 2
        faces.append((a0, a1, a1 + 1, a0 + 1))
    faces.append(tuple(range(0, segments * 2, 2)))
    faces.append(tuple(range(segments * 2 - 1, 0, -2)))
    if rot_x:
        c, s = math.cos(rot_x), math.sin(rot_x)
        verts = [(x, y * c - z * s, y * s + z * c) for (x, y, z) in verts]
    verts = [(x + cx, y + cy, z + cz) for (x, y, z) in verts]
    return new_mesh_object(name, verts, faces, mat_name)


def sphere(name, center, radius, mat_name, subdiv=1, scale=(1, 1, 1)):
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
    obj.data.materials.append(get_material(mat_name))
    return obj


def join_objects(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    joined.data.name = name + "Mesh"
    return joined


# ---------------------------------------------------------------------------
# esqueleto
# ---------------------------------------------------------------------------

BONES = [
    # nombre, head, tail, padre
    ("root",      (0.0, 0.0, 0.02), (0.0, 0.0, 0.16), None),
    ("hips",      (0.0, 0.0, 0.92), (0.0, 0.0, 1.04), "root"),
    ("spine",     (0.0, 0.0, 1.04), (0.0, 0.0, 1.26), "hips"),
    ("chest",     (0.0, 0.0, 1.26), (0.0, 0.0, 1.44), "spine"),
    ("neck",      (0.0, 0.0, 1.44), (0.0, 0.0, 1.53), "chest"),
    ("head",      (0.0, 0.0, 1.53), (0.0, 0.0, 1.78), "neck"),
    ("shoulder.L", (0.05, 0.0, 1.42), (0.24, 0.0, 1.42), "chest"),
    ("upperarm.L", (0.24, 0.0, 1.42), (0.24, 0.0, 1.15), "shoulder.L"),
    ("forearm.L",  (0.24, 0.0, 1.15), (0.24, 0.0, 0.90), "upperarm.L"),
    ("hand.L",     (0.24, 0.0, 0.90), (0.24, 0.0, 0.80), "forearm.L"),
    ("shoulder.R", (-0.05, 0.0, 1.42), (-0.24, 0.0, 1.42), "chest"),
    ("upperarm.R", (-0.24, 0.0, 1.42), (-0.24, 0.0, 1.15), "shoulder.R"),
    ("forearm.R",  (-0.24, 0.0, 1.15), (-0.24, 0.0, 0.90), "upperarm.R"),
    ("hand.R",     (-0.24, 0.0, 0.90), (-0.24, 0.0, 0.80), "forearm.R"),
    ("thigh.L",    (0.11, 0.0, 0.92), (0.11, 0.0, 0.50), "hips"),
    ("shin.L",     (0.11, 0.0, 0.50), (0.11, 0.0, 0.09), "thigh.L"),
    ("foot.L",     (0.11, 0.0, 0.09), (0.11, -0.18, 0.04), "shin.L"),
    ("thigh.R",    (-0.11, 0.0, 0.92), (-0.11, 0.0, 0.50), "hips"),
    ("shin.R",     (-0.11, 0.0, 0.50), (-0.11, 0.0, 0.09), "thigh.R"),
    ("foot.R",     (-0.11, 0.0, 0.09), (-0.11, -0.18, 0.04), "shin.R"),
]


def build_armature(name="Rig"):
    arm_data = bpy.data.armatures.new(name)
    rig = bpy.data.objects.new(name, arm_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    created = {}
    for bname, head, tail, parent in BONES:
        bone = arm_data.edit_bones.new(bname)
        bone.head = Vector(head)
        bone.tail = Vector(tail)
        bone.roll = 0.0
        created[bname] = bone
    for bname, head, tail, parent in BONES:
        if parent:
            created[bname].parent = created[parent]
            created[bname].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


# ---------------------------------------------------------------------------
# cuerpo (mesh por partes, cada parte rigida a un hueso)
# ---------------------------------------------------------------------------

def build_body(head_mul=1.0):
    """Devuelve lista de (objeto, nombre_de_hueso). Personaje cartoon low-poly."""
    parts = []
    hm = head_mul

    # torso con forma (pecho ancho, cintura angosta)
    parts.append((box("pecho", (0, 0, 1.36), (0.44, 0.26, 0.20), SHIRT), "chest"))
    parts.append((box("cintura", (0, 0, 1.16), (0.36, 0.23, 0.24), SHIRT), "spine"))
    parts.append((box("cadera", (0, 0, 0.97), (0.34, 0.23, 0.16), PANTS), "hips"))

    # cuello + cabeza grande de dibujito
    parts.append((cylinder("cuello", (0, 0, 1.50), 0.055, 0.09, SKIN, segments=8), "neck"))
    parts.append((sphere("cabeza", (0, 0, 1.645), 0.155 * hm, SKIN, subdiv=2, scale=(1.0, 0.96, 1.02)), "head"))
    # pelo: casquete arriba y atras (no tapa la cara)
    parts.append((sphere("pelo", (0, 0.018, 1.70), 0.158 * hm, HAIR, subdiv=2, scale=(1.03, 1.0, 0.62)), "head"))
    # ojos
    for side, sx in (("L", 1), ("R", -1)):
        parts.append((box("ojo." + side, (0.055 * sx * hm, -0.128 * hm, 1.665), (0.030, 0.02, 0.042), SHOES), "head"))
    # nariz
    parts.append((box("nariz", (0, -0.152 * hm, 1.632), (0.030, 0.030, 0.030), SKIN), "head"))
    # boca
    parts.append((box("boca", (0, -0.142 * hm, 1.578), (0.062, 0.02, 0.016), SHOES), "head"))

    # brazos
    for side, sx in (("L", 1), ("R", -1)):
        parts.append((sphere("hombro." + side, (0.215 * sx, 0, 1.40), 0.075, SHIRT, subdiv=1), "shoulder." + side))
        parts.append((cylinder("brazo." + side, (0.225 * sx, 0, 1.285), 0.056, 0.24, SHIRT, segments=8), "upperarm." + side))
        parts.append((cylinder("antebrazo." + side, (0.235 * sx, 0, 1.045), 0.048, 0.24, SKIN, segments=8), "forearm." + side))
        parts.append((sphere("mano." + side, (0.235 * sx, 0, 0.905), 0.058, SKIN, subdiv=1), "hand." + side))

    # piernas
    for side, sx in (("L", 1), ("R", -1)):
        parts.append((cylinder("muslo." + side, (0.105 * sx, 0, 0.735), 0.072, 0.38, PANTS, segments=8), "thigh." + side))
        parts.append((cylinder("pantorrilla." + side, (0.105 * sx, 0, 0.315), 0.060, 0.40, PANTS, segments=8), "shin." + side))
        parts.append((box("pie." + side, (0.105 * sx, -0.055, 0.055), (0.13, 0.27, 0.11), SHOES), "foot." + side))

    return parts


def assemble_character(name, head_mul=1.0, overall=1.0):
    parts = build_body(head_mul)
    mesh_objs = [p[0] for p in parts]

    # grupo de vertices rigido: cada parte 100% a su hueso
    for obj, bone in parts:
        vg = obj.vertex_groups.new(name=bone)
        vg.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")

    body = join_objects(mesh_objs, name + "Body")

    rig = build_armature(name + "Rig")
    body.parent = rig
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    mod.use_vertex_groups = True

    # El escalado final por personaje se aplica en Unity (prefab scale),
    # aca solo cambian las proporciones relativas (head_mul).
    return rig, body


# ---------------------------------------------------------------------------
# animaciones
# ---------------------------------------------------------------------------

def key_bone(rig, bone, frame, rot=None, loc=None):
    pb = rig.pose.bones[bone]
    pb.rotation_mode = "XYZ"
    if rot is not None:
        pb.rotation_euler = Euler(rot, "XYZ")
        pb.keyframe_insert("rotation_euler", frame=frame)
    if loc is not None:
        pb.location = Vector(loc)
        pb.keyframe_insert("location", frame=frame)


def start_action(rig, name):
    if rig.animation_data is None:
        rig.animation_data_create()
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data.action = action
    return action


def finish_action(action, start, end):
    # Blender 5.x usa acciones con slots/layers: no hace falta tocar las
    # curvas, el interpolado por defecto (Bezier) ya es el correcto.
    try:
        action.use_cyclic = False
    except Exception:
        pass


def anim_idle(rig):
    action = start_action(rig, "Idle")
    keys = [
        (1,  0.00, 0.00),
        (15, -0.02, 0.012),
        (30, 0.00, 0.00),
        (45, 0.02, 0.012),
        (60, 0.00, 0.00),
    ]
    for f, lean, bob in keys:
        key_bone(rig, "spine", f, rot=(lean, 0, 0))
        key_bone(rig, "chest", f, rot=(lean * 0.6, 0, 0))
        key_bone(rig, "head", f, rot=(-lean * 0.5, 0, lean * 2.0))
        key_bone(rig, "upperarm.L", f, rot=(0, 0, -0.10 - lean))
        key_bone(rig, "upperarm.R", f, rot=(0, 0, 0.10 + lean))
        key_bone(rig, "forearm.L", f, rot=(-0.15, 0, 0))
        key_bone(rig, "forearm.R", f, rot=(-0.15, 0, 0))
        key_bone(rig, "hips", f, loc=(0, 0, bob))
    finish_action(action, 1, 60)
    return action


def _walk_cycle(rig, name, start, length, amp, arm_amp, bob, knee, lean):
    action = start_action(rig, name)
    quarter = length / 4.0
    # 5 claves: 0, 1/4, 1/2, 3/4, 1 para cerrar el ciclo
    frames = [start + i * quarter for i in range(5)]
    for i, f in enumerate(frames):
        t = i / 4.0
        swing = math.sin(2 * math.pi * t) * amp
        arm_swing = math.sin(2 * math.pi * t) * arm_amp

        key_bone(rig, "thigh.L", f, rot=(swing, 0, 0))
        key_bone(rig, "thigh.R", f, rot=(-swing, 0, 0))
        key_bone(rig, "shin.L", f, rot=(-max(0.0, swing) * knee - 0.05, 0, 0))
        key_bone(rig, "shin.R", f, rot=(-max(0.0, -swing) * knee - 0.05, 0, 0))
        key_bone(rig, "foot.L", f, rot=(-swing * 0.35, 0, 0))
        key_bone(rig, "foot.R", f, rot=(swing * 0.35, 0, 0))

        key_bone(rig, "upperarm.L", f, rot=(-arm_swing, 0, -0.08))
        key_bone(rig, "upperarm.R", f, rot=(arm_swing, 0, 0.08))
        key_bone(rig, "forearm.L", f, rot=(-arm_swing * 0.5 - 0.15, 0, 0))
        key_bone(rig, "forearm.R", f, rot=(arm_swing * 0.5 - 0.15, 0, 0))

        key_bone(rig, "hips", f, rot=(0, swing * 0.10, 0), loc=(0, 0, bob * math.sin(4 * math.pi * t)))
        key_bone(rig, "spine", f, rot=(lean, 0, -swing * 0.12))
        key_bone(rig, "head", f, rot=(-lean * 0.6, 0, swing * 0.06))
    finish_action(action, start, start + length)
    return action


def anim_walk(rig):
    return _walk_cycle(rig, "Walk", 1, 32, amp=math.radians(26), arm_amp=math.radians(22),
                       bob=0.025, knee=1.1, lean=math.radians(3))


def anim_run(rig):
    return _walk_cycle(rig, "Run", 1, 20, amp=math.radians(42), arm_amp=math.radians(38),
                       bob=0.05, knee=1.6, lean=math.radians(12))


def anim_work(rig):
    action = start_action(rig, "Work")
    # martillear: el brazo derecho sube y baja golpeando, cuerpo levemente inclinado
    beats = [
        (1,  0.10, -1.90, -0.55, 0.00),
        (5,  0.20, -0.55, -0.15, -0.03),
        (8,  0.22, -0.30, -0.05, -0.04),
        (12, 0.12, -1.30, -0.40, 0.00),
        (18, 0.10, -1.90, -0.55, 0.00),
        (22, 0.20, -0.55, -0.15, -0.03),
        (25, 0.22, -0.30, -0.05, -0.04),
        (30, 0.12, -1.30, -0.40, 0.00),
        (36, 0.10, -1.90, -0.55, 0.00),
        (40, 0.20, -0.55, -0.15, -0.03),
        (44, 0.14, -1.20, -0.35, 0.00),
    ]
    for f, lean, arm, elbow, bob in beats:
        key_bone(rig, "spine", f, rot=(lean, 0, 0))
        key_bone(rig, "chest", f, rot=(lean * 0.5, 0, -0.10))
        key_bone(rig, "head", f, rot=(lean * 0.8, 0, 0))
        key_bone(rig, "upperarm.R", f, rot=(arm, 0, 0.18))
        key_bone(rig, "forearm.R", f, rot=(elbow, 0, 0))
        key_bone(rig, "upperarm.L", f, rot=(-0.55, 0, -0.12))
        key_bone(rig, "forearm.L", f, rot=(-0.75, 0, 0))
        key_bone(rig, "hips", f, loc=(0, 0, bob))
    finish_action(action, 1, 44)
    return action


def anim_wave(rig):
    action = start_action(rig, "Wave")
    # saluda levantando el brazo derecho al costado y moviendo la mano
    poses = [
        (1,  0.00, 0.00),
        (8,  1.95, 0.00),
        (14, 2.15, 0.30),
        (20, 2.15, -0.30),
        (26, 2.15, 0.30),
        (32, 1.35, 0.00),
        (40, 0.00, 0.00),
    ]
    for f, lift, wave in poses:
        key_bone(rig, "upperarm.R", f, rot=(-0.45, 0, lift))
        key_bone(rig, "forearm.R", f, rot=(-0.35, 0, wave * 0.8))
        key_bone(rig, "hand.R", f, rot=(0, 0, wave))
        key_bone(rig, "upperarm.L", f, rot=(0, 0, -0.10))
        key_bone(rig, "head", f, rot=(0, wave * 0.2, 0))
        key_bone(rig, "chest", f, rot=(0, 0, wave * 0.10))
    finish_action(action, 1, 40)
    return action


# ---------------------------------------------------------------------------
# render de control
# ---------------------------------------------------------------------------

def setup_preview_camera():
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = Vector((1.55, -2.25, 1.35))
    target = Vector((0.0, 0.0, 0.98))
    direction = target - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 55
    bpy.context.scene.camera = cam

    light_data = bpy.data.lights.new("Key", "SUN")
    light_data.energy = 3.5
    light = bpy.data.objects.new("Key", light_data)
    bpy.context.collection.objects.link(light)
    light.rotation_euler = Euler((math.radians(55), 0, math.radians(35)), "XYZ")

    scene = bpy.context.scene
    scene.render.resolution_x = 560
    scene.render.resolution_y = 560
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("W")
    scene.world.color = (0.08, 0.09, 0.12)


def render_preview(path, frame):
    bpy.context.scene.frame_set(frame)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------------------
# export
# ---------------------------------------------------------------------------

def export_fbx(path, rig, body):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_bones=True,
        bake_anim_force_startend_keying=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_mesh_modifiers=True,
    )


def build_character(file_name, overall=1.0, head_mul=1.0, preview_frames=None):
    reset_scene()
    rig, body = assemble_character(file_name, head_mul=head_mul, overall=overall)

    actions = [anim_idle(rig), anim_walk(rig), anim_run(rig), anim_work(rig), anim_wave(rig)]
    for a in actions:
        a.use_fake_user = True

    rig.animation_data.action = actions[0]

    setup_preview_camera()
    os.makedirs(PREV_DIR, exist_ok=True)
    previews = [("Idle", 1), ("Walk", 9), ("Work", 8), ("Wave", 14)]
    for i, (act_name, frame) in enumerate(previews):
        rig.animation_data.action = bpy.data.actions[act_name]
        render_preview(os.path.join(PREV_DIR, f"{file_name}_{i}_{act_name}.png"), frame)

    rig.animation_data.action = actions[0]
    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, file_name + ".fbx")
    export_fbx(out, rig, body)

    tris = sum(len(p.vertices) - 2 for p in body.data.polygons)
    acts = [a.name for a in bpy.data.actions]
    print(f"[CHAR] {file_name}: verts={len(body.data.vertices)} tris={tris} "
          f"bones={len(rig.data.bones)} actions={acts} "
          f"size={os.path.getsize(out) if os.path.exists(out) else -1} bytes")
    return out


def main():
    print("[CHAR] inicio")
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(PREV_DIR, exist_ok=True)
    build_character("Villager", overall=1.0, head_mul=1.0)
    build_character("Kid", overall=0.72, head_mul=1.28)
    build_character("Elder", overall=0.95, head_mul=1.05)
    print("[CHAR] fin")


if __name__ == "__main__":
    main()
