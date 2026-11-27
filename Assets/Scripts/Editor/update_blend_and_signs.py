import bpy
import math
import os

blend_path = "Assets/Environment/UnityNewtonFullEnv.blend"
fbx_path = "Assets/Environment/UnityNewtonFullEnv.fbx"

# 1. Update Sign.002_North position, rotation, and text
s_north = bpy.data.objects.get("Sign.002_North")
if s_north:
    s_north.rotation_euler = (math.pi / 2.0, 0.0, math.pi)
    s_north.location = (0.0, 0.14, 4.38) # In front of SignBG.002_North
    s_north.data.body = "EXPERIMENT 1\nNEWTON'S FIRST LAW"
    s_north.data.size = 0.52
    s_north.data.space_line = 1.15
    s_north.data.align_x = 'CENTER'
    s_north.data.align_y = 'CENTER'
    print("Updated Sign.002_North transform & text.")

bg_north = bpy.data.objects.get("SignBG.002_North")
if bg_north:
    bg_north.location = (0.0, 0.05, 4.38)
    bg_north.scale = (4.8, 0.12, 1.1)

# Ensure sign text objects for Lab 2 and Lab 3 have correct text and alignment
s_east = bpy.data.objects.get("Sign")
if s_east and s_east.type == 'FONT':
    s_east.data.body = "EXPERIMENT 2\nNEWTON'S SECOND LAW"
    s_east.data.size = 0.52
    s_east.data.space_line = 1.15
    s_east.data.align_x = 'CENTER'
    s_east.data.align_y = 'CENTER'

s_west = bpy.data.objects.get("Sign.001")
if s_west and s_west.type == 'FONT':
    s_west.data.body = "EXPERIMENT 3\nNEWTON'S THIRD LAW"
    s_west.data.size = 0.52
    s_west.data.space_line = 1.15
    s_west.data.align_x = 'CENTER'
    s_west.data.align_y = 'CENTER'

# 2. Update material viewport/diffuse colors for vibrant futuristic look
color_map = {
    'TextCyan.001': (0.05, 0.88, 1.0, 1.0),
    'TextCyan': (0.05, 0.88, 1.0, 1.0),
    'NeonCyan': (0.05, 0.88, 1.0, 1.0),
    'NeonRing': (0.05, 0.88, 1.0, 1.0),
    'PanelBG': (0.015, 0.035, 0.08, 1.0),
    'SignBG': (0.015, 0.035, 0.08, 1.0),
    'Wall': (0.84, 0.87, 0.92, 1.0),
    'Floor': (0.14, 0.17, 0.23, 1.0),
    'Screen': (0.04, 0.10, 0.22, 1.0),
}

for mat in bpy.data.materials:
    for prefix, col in color_map.items():
        if prefix.lower() in mat.name.lower():
            mat.diffuse_color = col
            # If node tree exists, also update Principled BSDF base color
            if mat.use_nodes and mat.node_tree:
                for n in mat.node_tree.nodes:
                    if n.type == 'BSDF_PRINCIPLED':
                        n.inputs['Base Color'].default_value = col
                        if 'Cyan' in mat.name or 'Neon' in mat.name:
                            n.inputs['Emission Color'].default_value = col
                            n.inputs['Emission Strength'].default_value = 2.0
                        elif 'PanelBG' in mat.name or 'SignBG' in mat.name:
                            n.inputs['Roughness'].default_value = 0.15

# Assign materials to signs
mat_cyan = bpy.data.materials.get('TextCyan.001')
mat_bg = bpy.data.materials.get('PanelBG')

for s in [s_north, s_east, s_west]:
    if s and mat_cyan:
        s.data.materials.clear()
        s.data.materials.append(mat_cyan)

for bg in [bg_north, bpy.data.objects.get("SignBG"), bpy.data.objects.get("SignBG.001")]:
    if bg and mat_bg:
        bg.data.materials.clear()
        bg.data.materials.append(mat_bg)

# Save blend
bpy.ops.wm.save_mainfile(filepath=blend_path)
print(f"Saved updated {blend_path}")

# Re-export FBX
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=False,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_NONE',
    axis_forward='-Z',
    axis_up='Y',
    bake_space_transform=False,
    object_types={'EMPTY', 'CAMERA', 'LIGHT', 'ARMATURE', 'MESH', 'OTHER'},
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
)
print(f"Re-exported updated {fbx_path}")

