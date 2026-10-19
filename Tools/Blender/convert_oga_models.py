"""Convierte los .blend descargados de OpenGameArt a FBX para Unity."""
import os
import shutil
import sys

import bpy
import mathutils

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import build_china_assets_v2 as v2  # noqa: E402

DL = r"C:\Users\USUARIO\Documents\deepseek-harness\default-workspace\pueblo_stage_china_v2\downloads"
OUT = v2.OUT

FILES = [
    ("gong.blend", "SM_Prop_Gong", False),
    ("chinese_cart.blend", "SM_Prop_Cart", False),
    ("FantasyDragon_Animation_Free.blend", "SM_Prop_Dragon", True),
]


def stats(tag):
    meshes = [ob for ob in bpy.data.objects if ob.type == "MESH"]
    tris = 0
    lo = mathutils.Vector((1e9, 1e9, 1e9))
    hi = mathutils.Vector((-1e9, -1e9, -1e9))
    for ob in meshes:
        for p in ob.data.polygons:
            tris += len(p.vertices) - 2
        for c in ob.bound_box:
            w = ob.matrix_world @ mathutils.Vector(c)
            for i in range(3):
                lo[i] = min(lo[i], w[i])
                hi[i] = max(hi[i], w[i])
    arms = [ob for ob in bpy.data.objects if ob.type == "ARMATURE"]
    print("[OGA] %s meshes=%d tris=%d armatures=%d" % (tag, len(meshes), tris, len(arms)), flush=True)
    if meshes:
        print("[OGA] %s bounds=(%.2f..%.2f, %.2f..%.2f, %.2f..%.2f) tamanoY=%.2f" % (
            tag, lo.x, hi.x, lo.y, hi.y, lo.z, hi.z, hi.z - lo.z), flush=True)
    print("[OGA] %s acciones=%d materiales=%d" % (
        tag, len(bpy.data.actions), len(bpy.data.materials)), flush=True)


def show_all():
    """Habilita todas las colecciones y objetos en el View Layer."""
    vl = bpy.context.view_layer

    def rec(lc):
        try:
            lc.exclude = False
            lc.hide_viewport = False
        except Exception:
            pass
        for c in lc.children:
            rec(c)

    rec(vl.layer_collection)
    for ob in bpy.data.objects:
        try:
            ob.hide_viewport = False
            ob.hide_render = False
            ob.hide_select = False
        except Exception:
            pass


def main():
    os.makedirs(OUT, exist_ok=True)
    for src, out_name, has_anim in FILES:
        path_in = os.path.join(DL, src)
        if not os.path.exists(path_in):
            print("[OGA] FALTA %s" % src, flush=True)
            continue
        print("[OGA] === %s ===" % src, flush=True)
        bpy.ops.wm.open_mainfile(filepath=path_in)
        stats(src)
        show_all()
        # desempaquetar texturas embebidas si las hay
        try:
            bpy.ops.file.unpack_all(method="WRITE_ORIGINAL")
            print("[OGA] texturas desempaquetadas", flush=True)
        except Exception as exc:
            print("[OGA] unpack: %s" % exc, flush=True)
        # exportar: mallas (+ armatures si tiene animaciones)
        bpy.ops.object.select_all(action="DESELECT")
        types = {"MESH"} | ({"ARMATURE"} if has_anim else set())
        active = None
        for ob in bpy.data.objects:
            if ob.type in types:
                ob.select_set(True)
                if active is None and ob.type == "MESH":
                    active = ob
        if active is not None:
            bpy.context.view_layer.objects.active = active
        fbx = os.path.join(OUT, out_name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=fbx, use_selection=True,
            object_types={"ARMATURE", "MESH"},
            apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
            mesh_smooth_type="FACE", use_mesh_modifiers=True, add_leaf_bones=False,
            bake_anim=has_anim, bake_anim_use_all_actions=has_anim,
            bake_anim_use_nla_strips=False,
        )
        print("[OGA] export %s -> %s (%d KB)" % (src, out_name, os.path.getsize(fbx) // 1024), flush=True)
    # copiar texturas del carrito junto al FBX (Unity las enlaza por nombre)
    for png in ("chinese_cart-01.png", "chinese_cart_roof-01.png", "chinese_cart_wheel-01.png"):
        src = os.path.join(DL, png)
        if os.path.exists(src):
            shutil.copy(src, os.path.join(OUT, png))
            print("[OGA] textura copiada: %s" % png, flush=True)
    print("[OGA] fin", flush=True)


if __name__ == "__main__":
    main()
