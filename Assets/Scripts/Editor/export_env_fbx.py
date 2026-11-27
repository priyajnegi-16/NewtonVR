import bpy
import os

blend_path = "Assets/Environment/UnityNewtonFullEnv.blend"
fbx_path = "Assets/Environment/UnityNewtonFullEnv.fbx"
bpy.ops.wm.open_mainfile(filepath=blend_path)

# Convert all FONT objects to MESH for full FBX compatibility
bpy.ops.object.select_all(action='DESELECT')
font_count = 0
for obj in list(bpy.data.objects):
    if obj.type == 'FONT':
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target='MESH')
        obj.select_set(False)
        font_count += 1

print(f"Converted {font_count} FONT objects to MESH.")

# Select all EMPTY and MESH objects for FBX export
for obj in bpy.data.objects:
    if obj.type in {'EMPTY', 'MESH'}:
        obj.select_set(True)
    else:
        obj.select_set(False)

bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    object_types={'EMPTY', 'MESH'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    bake_space_transform=False
)

print(f"Exported FBX successfully to {fbx_path} (size: {os.path.getsize(fbx_path)} bytes)")

