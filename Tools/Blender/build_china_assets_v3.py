"""
PUEBLO LOCO — Ciudad China v3 (contenido adicional)

Reutiliza los helpers de build_china_assets_v2 e importa:
  - SM_Building_Blacksmith (herrería con fragua)
  - SM_Building_Farm       (granja de arroz en terrazas)
  - SM_Building_Inn         (posada de dos plantas)
  - SM_Building_BellTower   (campanario)
  - SM_Prop_Well            (pozo)
  - SK_Merchant             (comerciante, 22 huesos, 8 animaciones)
  - SK_Farmer               (campesino, 22 huesos, 8 animaciones)

Uso:
  blender.exe --background --factory-startup --offline-mode --python build_china_assets_v3.py
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
    "Ember": (1.0, 0.42, 0.10, 1.0),
    "Water": (0.12, 0.30, 0.34, 1.0),
    "Rice": (0.42, 0.62, 0.20, 1.0),
    "Thatch": (0.72, 0.62, 0.36, 1.0),
    "Straw": (0.86, 0.76, 0.48, 1.0),
    "Terracotta": (0.66, 0.33, 0.20, 1.0),
})


def box(name, center, size, mat, rot_x=0.0, rot_y=0.0, rot_z=0.0):
    return v2.box(name, center, size, mat, rot_x=rot_x, rot_y=rot_y, rot_z=rot_z)


def cyl(name, center, rb, rt, h, mat, seg=12, rot_x=0.0):
    return v2.cyl(name, center, rb, rt, h, mat, segments=seg, rot_x=rot_x)


def ball(name, center, r, mat, subdiv=2, scale=(1, 1, 1)):
    return v2.ball(name, center, r, mat, subdiv=subdiv, scale=scale)


# ---------------------------------------------------------------------------
# edificios
# ---------------------------------------------------------------------------

def build_blacksmith():
    bc.reset_scene()
    o = []
    o.append(box("base", (0, 0, 0.10), (2.7, 0.20, 2.3), "Stone"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(cyl("post", (1.10 * sx, 0.90 * sy, 1.05), 0.07, 0.06, 1.95, "DarkWood", 8))
    o.append(box("beamN", (0, 0.90, 2.05), (2.30, 0.12, 0.12), "DarkWood"))
    o.append(box("beamS", (0, -0.90, 2.05), (2.30, 0.12, 0.12), "DarkWood"))
    o.append(box("beamE", (1.10, 0, 2.05), (0.12, 1.90, 0.12), "DarkWood"))
    o.append(box("beamW", (-1.10, 0, 2.05), (0.12, 1.90, 0.12), "DarkWood"))
    o.append(box("wallBack", (0, 0.93, 0.95), (2.10, 0.10, 1.65), "Lime"))
    o.append(box("wallL", (-1.11, 0, 0.95), (0.10, 1.70, 1.65), "Lime"))
    o += v2.dougong("dg", 2.1, 1.9, 2.12, 3)
    r = v2.roof("roof", 2.4, 2.1, 0.55, 0.32, 0.18)
    for ob in r:
        ob.location.z += 2.16
    o += r
    # fragua + brasas + chimenea
    o.append(box("forge", (0.20, 0.35, 0.55), (1.05, 0.75, 0.95), "Stone"))
    o.append(box("embers", (0.20, 0.35, 1.04), (0.75, 0.48, 0.10), "Ember"))
    o.append(cyl("chimney", (0.20, 0.35, 2.05), 0.20, 0.24, 1.55, "Stone", 10))
    o.append(cyl("chimneyCap", (0.20, 0.35, 2.86), 0.30, 0.24, 0.12, "Terracotta", 10))
    # yunque sobre tronco
    o.append(cyl("stump", (-0.62, -0.30, 0.28), 0.20, 0.18, 0.56, "DarkWood", 10))
    o.append(box("anvil", (-0.62, -0.30, 0.66), (0.52, 0.20, 0.20), "Ink"))
    o.append(box("anvilHorn", (-0.92, -0.30, 0.66), (0.18, 0.12, 0.12), "Ink"))
    # herramientas colgadas y balde
    for k in range(3):
        o.append(box("tool", (0.50 + k * 0.22, 0.35, 1.55), (0.05, 0.05, 0.55), "DarkWood", rot_y=0.25))
    o.append(cyl("bucket", (-0.85, 0.55, 0.20), 0.17, 0.15, 0.32, "DarkWood", 8))
    o.append(box("sign", (0, -1.02, 1.95), (0.8, 0.05, 0.26), "Lacquer"))
    o += v2.lantern("lant", 0.95, -1.00, 1.70, 0.11)
    body = v2.join(o, "Blacksmith")
    v2.uv_unwrap(body)
    v2.preview("Herreria", 3.8, 1.4)
    path = os.path.join(OUT, "SM_Building_Blacksmith.fbx")
    v2.export_static(path, body)
    v2.register("SM_Building_Blacksmith", path, v2.tris_of(body), kind="building")


def build_farm():
    bc.reset_scene()
    o = []
    # terraza alta
    o.append(box("terraceA", (0, 0.75, 0.16), (2.8, 1.30, 0.32), "Lime"))
    o.append(box("waterA", (0, 0.75, 0.33), (2.45, 1.05, 0.02), "Water"))
    for i in range(5):
        o.append(box("riceA", (-0.90 + i * 0.45, 0.75, 0.40), (0.12, 0.95, 0.12), "Rice"))
    # terraza baja
    o.append(box("terraceB", (0, -0.55, 0.12), (2.8, 1.10, 0.24), "Lime"))
    o.append(box("waterB", (0, -0.55, 0.25), (2.45, 0.85, 0.02), "Water"))
    for i in range(5):
        o.append(box("riceB", (-0.90 + i * 0.45, -0.55, 0.32), (0.12, 0.75, 0.12), "Rice"))
    # cabaña con techo de paja
    o.append(box("hut", (0.95, 0.90, 0.55), (0.85, 0.85, 0.90), "Lime"))
    o.append(box("hutDoor", (0.95, 0.46, 0.40), (0.32, 0.06, 0.60), "DarkWood"))
    hutRoof = v2.roof("hutRoof", 1.05, 1.05, 0.35, 0.22, 0.10, mat="Thatch", ridge_mat="Straw")
    for ob in hutRoof:
        ob.location.x += 0.95
        ob.location.y += 0.90
        ob.location.z += 1.00
    o += hutRoof
    # cerco
    for i in range(6):
        o.append(cyl("fence", (-1.35 + i * 0.54, -1.20, 0.28), 0.04, 0.04, 0.56, "DarkWood", 6))
    o.append(box("fenceRail", (0, -1.20, 0.48), (2.8, 0.05, 0.05), "DarkWood"))
    body = v2.join(o, "Farm")
    v2.uv_unwrap(body)
    v2.preview("Granja", 4.2, 1.2)
    path = os.path.join(OUT, "SM_Building_Farm.fbx")
    v2.export_static(path, body)
    v2.register("SM_Building_Farm", path, v2.tris_of(body), kind="building")


def build_inn():
    bc.reset_scene()
    o = []
    o.append(box("base", (0, 0, 0.12), (2.8, 0.22, 2.4), "Stone"))
    # planta baja
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(cyl("col", (1.10 * sx, 0.95 * sy, 1.10), 0.08, 0.07, 2.0, "Lacquer", 10))
    o.append(box("wallBack", (0, 0.98, 1.05), (2.3, 0.10, 1.85), "Lime"))
    o.append(box("wallL", (-1.15, 0, 1.05), (0.10, 1.95, 1.85), "Lime"))
    o.append(box("wallR", (1.15, 0, 1.05), (0.10, 1.95, 1.85), "Lime"))
    o.append(box("door", (0, -1.02, 0.95), (0.9, 0.10, 1.6), "DarkWood"))
    o += v2.lattice_window("win1", -0.72, -1.02, 1.30, 0.34, 0.40)
    o += v2.lattice_window("win2", 0.72, -1.02, 1.30, 0.34, 0.40)
    # planta alta con balcón
    o.append(box("floor2", (0, 0, 2.05), (2.6, 2.1, 0.14), "DarkWood"))
    o.append(box("upWall", (0, 0.90, 2.70), (2.2, 0.10, 1.10), "Lime"))
    o.append(box("upWallL", (-1.05, 0, 2.70), (0.10, 1.8, 1.10), "Lime"))
    o.append(box("upWallR", (1.05, 0, 2.70), (0.10, 1.8, 1.10), "Lime"))
    o += v2.lattice_window("winUp1", -0.45, -0.95, 2.75, 0.30, 0.36)
    o += v2.lattice_window("winUp2", 0.45, -0.95, 2.75, 0.30, 0.36)
    o += v2.railing("rail", 2.5, 2.0, 2.20)
    o += v2.dougong("dg2", 2.3, 2.0, 3.20, 3)
    r = v2.roof("roof", 2.6, 2.3, 0.60, 0.34, 0.20)
    for ob in r:
        ob.location.z += 3.24
    o += r
    o.append(box("sign", (0.95, -1.05, 2.55), (0.5, 0.05, 0.7), "Lacquer"))
    for sx in (-1, 1):
        o += v2.lantern("lant", 1.10 * sx, -1.06, 2.25, 0.12)
    o += v2.banner("band", 0, -1.06, 1.90, 0.22, 0.55)
    body = v2.join(o, "Inn")
    v2.uv_unwrap(body)
    v2.preview("Posada", 4.6, 1.7)
    path = os.path.join(OUT, "SM_Building_Inn.fbx")
    v2.export_static(path, body)
    v2.register("SM_Building_Inn", path, v2.tris_of(body), kind="building")


def build_belltower():
    bc.reset_scene()
    o = []
    o.append(box("base", (0, 0, 0.16), (1.9, 0.32, 1.9), "Stone"))
    o.append(box("base2", (0, 0, 0.32), (1.7, 0.12, 1.7), "Stone"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            o.append(cyl("col", (0.65 * sx, 0.65 * sy, 1.65), 0.09, 0.08, 2.65, "Lacquer", 10))
    # vigas
    for sx in (-1, 1):
        o.append(box("beamX", (0, 0.65 * sx, 2.75), (1.5, 0.12, 0.12), "DarkWood"))
        o.append(box("beamY", (0.65 * sx, 0, 2.75), (0.12, 1.5, 0.12), "DarkWood"))
    o.append(box("beamTop", (0, 0, 3.05), (1.5, 1.5, 0.12), "DarkWood"))
    # campana
    o.append(cyl("bell", (0, 0, 2.30), 0.30, 0.42, 0.60, "Bronze", 14))
    o.append(ball("bellTop", (0, 0, 2.66), 0.14, "Bronze", subdiv=1))
    o.append(cyl("clapper", (0, 0, 1.95), 0.05, 0.04, 0.30, "Bronze", 8))
    # techo y baranda
    o += v2.dougong("dg", 1.5, 1.5, 3.15, 3)
    r = v2.roof("roof", 1.8, 1.8, 0.50, 0.32, 0.18)
    for ob in r:
        ob.location.z += 3.20
    o += r
    o += v2.lantern("lant", 0.6, -0.9, 2.9, 0.11)
    body = v2.join(o, "BellTower")
    v2.uv_unwrap(body)
    v2.preview("Campanario", 4.2, 1.9)
    path = os.path.join(OUT, "SM_Building_BellTower.fbx")
    v2.export_static(path, body)
    v2.register("SM_Building_BellTower", path, v2.tris_of(body), kind="building")


def build_well():
    bc.reset_scene()
    o = []
    o.append(cyl("ring", (0, 0, 0.35), 0.55, 0.50, 0.70, "Stone", 14))
    o.append(cyl("water", (0, 0, 0.66), 0.42, 0.42, 0.04, "Water", 14))
    for sx in (-1, 1):
        o.append(cyl("post", (0.60 * sx, 0, 0.95), 0.06, 0.05, 1.30, "DarkWood", 8))
    o.append(box("roofWell", (0, 0, 1.62), (1.5, 0.55, 0.10), "Thatch"))
    o.append(box("roofWell2", (0, 0, 1.72), (1.2, 0.40, 0.08), "Thatch"))
    o.append(cyl("bucket", (0, 0, 1.10), 0.13, 0.11, 0.22, "DarkWood", 8))
    o.append(cyl("rope", (0, 0, 1.45), 0.012, 0.012, 0.45, "Straw", 6))
    body = v2.join(o, "Well")
    v2.uv_unwrap(body)
    v2.preview("Pozo", 2.4, 1.0)
    path = os.path.join(OUT, "SM_Prop_Well.fbx")
    v2.export_static(path, body)
    v2.register("SM_Prop_Well", path, v2.tris_of(body), kind="prop")


# ---------------------------------------------------------------------------
# personajes
# ---------------------------------------------------------------------------

def assemble_character(name, parts):
    rig, body = v2.assemble(name, parts, name + "Rig")
    acts = [v2.anim_idle(rig), v2.anim_walk(rig), v2.anim_run(rig), v2.anim_work(rig),
            v2.anim_wave(rig), v2.anim_bow(rig), v2.anim_cheer(rig), v2.anim_gather(rig)]
    rig.animation_data.action = acts[0]
    return rig, body, acts


def build_merchant():
    bc.reset_scene()
    p = []
    p.append((cyl("skirt", (0, 0, 0.60), 0.26, 0.22, 0.72, "Robe", 14), "Hips"))
    p.append((box("chest", (0, 0, 1.28), (0.42, 0.26, 0.40), "Robe"), "Chest"))
    p.append((box("apron", (0, -0.15, 1.00), (0.34, 0.03, 0.72), "Lime"), "Spine"))
    p.append((box("belt", (0, 0, 1.08), (0.44, 0.28, 0.09), "Trim"), "Spine"))
    p.append((box("pouch", (0.22, -0.10, 1.02), (0.12, 0.10, 0.14), "DarkWood"), "Spine"))
    p.append((cyl("neck", (0, 0, 1.49), 0.05, 0.05, 0.09, "Skin", 8), "Neck"))
    p.append((ball("head", (0, 0, 1.64), 0.148, "Skin", subdiv=2), "Head"))
    p.append((ball("hair", (0, 0.02, 1.69), 0.152, "Hair", subdiv=2, scale=(1.02, 1, 0.6)), "Head"))
    p.append((cyl("hat", (0, 0.02, 1.78), 0.30, 0.16, 0.10, "Straw", 14), "Head"))
    p.append((cyl("hatTop", (0, 0.02, 1.84), 0.16, 0.06, 0.10, "Straw", 14), "Head"))
    v2.face_parts(p)
    for sx, side in ((1, "L"), (-1, "R")):
        p.append((ball("shoulder." + side, (0.21 * sx, 0, 1.39), 0.08, "Robe", subdiv=2),
                  "LeftShoulder" if sx > 0 else "RightShoulder"))
        p.append((box("sleeve." + side, (0.26 * sx, 0, 1.20), (0.16, 0.20, 0.38), "Robe"),
                  "LeftUpperArm" if sx > 0 else "RightUpperArm"))
        p.append((cyl("forearm." + side, (0.25 * sx, 0, 1.01), 0.042, 0.038, 0.24, "Skin", 8),
                  "LeftLowerArm" if sx > 0 else "RightLowerArm"))
        p.append((ball("hand." + side, (0.25 * sx, 0, 0.88), 0.055, "Skin", subdiv=2),
                  "LeftHand" if sx > 0 else "RightHand"))
        p.append((cyl("thigh." + side, (0.105 * sx, 0, 0.72), 0.075, 0.065, 0.40, "Trousers", 8),
                  "LeftUpperLeg" if sx > 0 else "RightUpperLeg"))
        p.append((cyl("shin." + side, (0.105 * sx, 0, 0.30), 0.062, 0.055, 0.40, "Trousers", 8),
                  "LeftLowerLeg" if sx > 0 else "RightLowerLeg"))
        p.append((box("boot." + side, (0.105 * sx, -0.05, 0.07), (0.13, 0.28, 0.14), "Boots"),
                  "LeftFoot" if sx > 0 else "RightFoot"))
    rig, body, acts = assemble_character("Merchant", p)
    v2.uv_unwrap(body)
    v2.preview_char("Comerciante", rig, "Walk", 9)
    path = os.path.join(OUT, "SK_Merchant.fbx")
    bc.export_fbx(path, rig, body)
    v2.register("SK_Merchant", path, v2.tris_of(body), len(rig.data.bones),
                [a.name for a in acts], ["Idle", "Walk", "Run", "Work"], kind="character")


def build_farmer():
    bc.reset_scene()
    p = []
    p.append((box("chest", (0, 0, 1.27), (0.44, 0.26, 0.42), "Robe"), "Chest"))
    p.append((box("tunicSkirt", (0, 0, 1.02), (0.42, 0.26, 0.16), "Robe"), "Spine"))
    p.append((cyl("neck", (0, 0, 1.48), 0.05, 0.05, 0.09, "Skin", 8), "Neck"))
    p.append((ball("head", (0, 0, 1.63), 0.147, "Skin", subdiv=2), "Head"))
    p.append((ball("hair", (0, 0.02, 1.68), 0.15, "Hair", subdiv=2, scale=(1.02, 1, 0.6)), "Head"))
    p.append((cyl("strawHat", (0, 0.01, 1.76), 0.34, 0.05, 0.16, "Straw", 14), "Head"))
    v2.face_parts(p)
    # caña al hombro con dos cestas
    p.append((box("pole", (0, 0, 1.55), (1.9, 0.05, 0.05), "DarkWood"), "Chest"))
    for sx in (-1, 1):
        p.append((cyl("basket." + ("L" if sx > 0 else "R"), (0.85 * sx, 0, 1.10), 0.16, 0.13, 0.22,
                      "Thatch", 10), "Chest"))
    for sx, side in ((1, "L"), (-1, "R")):
        p.append((ball("shoulder." + side, (0.21 * sx, 0, 1.38), 0.08, "Robe", subdiv=2),
                  "LeftShoulder" if sx > 0 else "RightShoulder"))
        p.append((box("sleeve." + side, (0.26 * sx, 0, 1.20), (0.15, 0.18, 0.34), "Robe"),
                  "LeftUpperArm" if sx > 0 else "RightUpperArm"))
        p.append((cyl("forearm." + side, (0.25 * sx, 0, 1.02), 0.042, 0.038, 0.22, "Skin", 8),
                  "LeftLowerArm" if sx > 0 else "RightLowerArm"))
        p.append((ball("hand." + side, (0.25 * sx, 0, 0.90), 0.055, "Skin", subdiv=2),
                  "LeftHand" if sx > 0 else "RightHand"))
        p.append((cyl("thigh." + side, (0.105 * sx, 0, 0.72), 0.075, 0.065, 0.40, "Trousers", 8),
                  "LeftUpperLeg" if sx > 0 else "RightUpperLeg"))
        p.append((cyl("shin." + side, (0.105 * sx, 0, 0.30), 0.062, 0.055, 0.40, "Trousers", 8),
                  "LeftLowerLeg" if sx > 0 else "RightLowerLeg"))
        p.append((box("boot." + side, (0.105 * sx, -0.05, 0.07), (0.13, 0.28, 0.14), "Boots"),
                  "LeftFoot" if sx > 0 else "RightFoot"))
    rig, body, acts = assemble_character("Farmer", p)
    v2.uv_unwrap(body)
    v2.preview_char("Campesino", rig, "Walk", 9)
    path = os.path.join(OUT, "SK_Farmer.fbx")
    bc.export_fbx(path, rig, body)
    v2.register("SK_Farmer", path, v2.tris_of(body), len(rig.data.bones),
                [a.name for a in acts], ["Idle", "Walk", "Run", "Work"], kind="character")


# ---------------------------------------------------------------------------

def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    print("[CHINA3] inicio", flush=True)
    build_blacksmith()
    build_farm()
    build_inn()
    build_belltower()
    build_well()
    build_merchant()
    build_farmer()

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
    print("[CHINA3] fin: %d assets (total %d)" % (len(v2.REGISTRY), merged["asset_count"]), flush=True)


if __name__ == "__main__":
    main()
