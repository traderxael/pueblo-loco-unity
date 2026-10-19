"""Inspecciona el FBX del samurai: huesos, acciones, bounds y materiales."""
import bpy
import os

SRC = r"C:\Users\USUARIO\Documents\deepseek-harness\default-workspace\pueblo_stage_china_v2\downloads\samurai.fbx"

bpy.ops.wm.read_homefile(use_empty=True)
# activar un objeto para que el importador no falle por contexto
bpy.ops.mesh.primitive_plane_add(size=0.01)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.import_scene.fbx(filepath=SRC)

print("[SAMY] objetos importados:", flush=True)
for ob in bpy.data.objects:
    kind = ob.type
    extra = ""
    if kind == "ARMATURE":
        extra = " huesos=%d" % len(ob.data.bones)
    if kind == "MESH":
        extra = " verts=%d" % len(ob.data.vertices)
    print("  %s [%s]%s" % (ob.name, kind, extra), flush=True)

print("[SAMY] acciones:", flush=True)
for a in bpy.data.actions:
    frames = [k.co[0] for fc in a.fcurves for k in fc.keyframe_points] if hasattr(a, "fcurves") else []
    rango = " %.0f-%0.f" % (min(frames), max(frames)) if frames else ""
    print("  accion=%s fcurves=%d%s" % (a.name, len(a.fcurves) if hasattr(a, "fcurves") else -1, rango), flush=True)

print("[SAMY] materiales:", flush=True)
for m in bpy.data.materials:
    print("  mat=%s" % m.name, flush=True)

print("[SAMY] texturas:", flush=True)
for t in bpy.data.textures:
    print("  tex=%s" % t.name, flush=True)

print("[SAMY] fin", flush=True)
