"""Colores base de los materiales del dragon (para verificacion por pixeles)."""
import bpy

bpy.ops.wm.open_mainfile(filepath=r"C:\Users\USUARIO\Documents\deepseek-harness\default-workspace\pueblo_stage_china_v2\downloads\FantasyDragon_Animation_Free.blend")
for m in bpy.data.materials:
    col = None
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type == "BSDF_PRINCIPLED":
                col = n.inputs["Base Color"].default_value[:]
    print("[DMAT] %s = %s" % (m.name, tuple(round(c, 2) for c in col) if col else "?"))
