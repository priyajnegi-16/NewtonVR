import bpy
import math

print("=== Starting Newton VR Environment Reorganization ===")

# Open UnityNewtonFullEnv.blend
bpy.ops.wm.open_mainfile(filepath="Assets/Environment/UnityNewtonFullEnv.blend")

# -------------------------------------------------------------
# 1. Open North doorway in MainLobby_Wall
# -------------------------------------------------------------
wall = bpy.data.objects.get("MainLobby_Wall")
if wall:
    modified_verts = 0
    for v in wall.data.vertices:
        # North wall region: Y > 15.0, abs(X) <= 2.1, Z < 0.1
        if v.co.y > 15.0 and abs(v.co.x) <= 2.1 and v.co.z < 0.1:
            v.co.z = 3.6
            modified_verts += 1
    wall.data.update()
    print(f"[MainLobby_Wall] Raised {modified_verts} bottom vertices to Z=3.6 to create North opening.")

# Remove WallScreen_5 and WallScreen_6 that were in North doorway
for ws_name in ["WallScreen_5", "WallScreen_6"]:
    ws = bpy.data.objects.get(ws_name)
    if ws:
        # Delete ws and all children
        objs_to_del = [ws] + list(ws.children_recursive)
        for o in objs_to_del:
            bpy.data.objects.remove(o, do_unlink=True)
        print(f"Removed {ws_name} from North doorway.")

# -------------------------------------------------------------
# 2. Remove Station 1 from MainLobby (will be placed inside Lab 1)
# -------------------------------------------------------------
station1 = bpy.data.objects.get("Station1_FirstLaw")
if station1:
    objs_to_del = [station1] + list(station1.children_recursive)
    for o in objs_to_del:
        bpy.data.objects.remove(o, do_unlink=True)
    print("Removed Station1_FirstLaw from MainLobby.")

station_light = bpy.data.objects.get("StationLight")
if station_light:
    bpy.data.objects.remove(station_light, do_unlink=True)
    print("Removed StationLight from MainLobby.")

# -------------------------------------------------------------
# 3. Create North Entrance Portal & EXPERIMENT 1 Sign facing MainLobby
# -------------------------------------------------------------
main_lobby = bpy.data.objects.get("MainLobby")
portal_south = bpy.data.objects.get("W.002")

def duplicate_hierarchy(source_obj, parent_obj=None, name_suffix="_N"):
    new_obj = source_obj.copy()
    if source_obj.data:
        new_obj.data = source_obj.data.copy()
    new_obj.name = source_obj.name + name_suffix
    bpy.context.scene.collection.objects.link(new_obj)
    if parent_obj:
        new_obj.parent = parent_obj
    new_obj.matrix_local = source_obj.matrix_local.copy()
    
    for child in source_obj.children:
        duplicate_hierarchy(child, new_obj, name_suffix)
    return new_obj

if portal_south:
    portal_north = duplicate_hierarchy(portal_south, main_lobby, "_North")
    portal_north.name = "Portal_Lab1"
    portal_north.location = (0.0, 15.95, 0.0)
    portal_north.rotation_euler = (0.0, 0.0, math.pi) # Face South into MainLobby
    print("Created Portal_Lab1 at (0, 15.95, 0) facing MainLobby.")

    # Find sign text and background in portal_north
    for child in portal_north.children:
        if "Sign" in child.name and child.type == "FONT":
            child.data.body = "EXPERIMENT 1\nNEWTON'S FIRST LAW"
            child.data.size = 0.38
            child.data.align_x = 'CENTER'
            child.data.align_y = 'CENTER'
            child.location = (0.0, -0.15, 4.38)
            print(f"Updated sign text to: '{child.data.body}'")
        elif "SignBG" in child.name:
            child.scale = (4.8, 0.12, 1.1)
            child.location = (0.0, -0.06, 4.38)

# -------------------------------------------------------------
# 4. Update Portal Signs for Lab 2 and Lab 3
# -------------------------------------------------------------
# East Portal W -> EXPERIMENT 2: NEWTON'S SECOND LAW
sign_east = bpy.data.objects.get("Sign")
if sign_east and sign_east.type == "FONT":
    sign_east.data.body = "EXPERIMENT 2\nNEWTON'S SECOND LAW"
    sign_east.data.size = 0.38
    sign_east.data.align_x = 'CENTER'
    sign_east.data.align_y = 'CENTER'
    print("Updated East Portal sign to EXPERIMENT 2: NEWTON'S SECOND LAW")
sign_bg_east = bpy.data.objects.get("SignBG")
if sign_bg_east:
    sign_bg_east.scale = (4.8, 0.12, 1.1)

# West Portal W.001 -> EXPERIMENT 3: NEWTON'S THIRD LAW
sign_west = bpy.data.objects.get("Sign.001")
if sign_west and sign_west.type == "FONT":
    sign_west.data.body = "EXPERIMENT 3\nNEWTON'S THIRD LAW"
    sign_west.data.size = 0.38
    sign_west.data.align_x = 'CENTER'
    sign_west.data.align_y = 'CENTER'
    print("Updated West Portal sign to EXPERIMENT 3: NEWTON'S THIRD LAW")
sign_bg_west = bpy.data.objects.get("SignBG.001")
if sign_bg_west:
    sign_bg_west.scale = (4.8, 0.12, 1.1)

# -------------------------------------------------------------
# 5. Clean up Lab 2 (BriefingRoom) interior
# -------------------------------------------------------------
briefing = bpy.data.objects.get("BriefingRoom")
if briefing:
    to_delete = []
    for c in briefing.children:
        c_name = c.name
        # Keep structural walls, floor, ceiling, lights, and entrance portal
        if any(keep in c_name for keep in ["BriefingRoom_Wall", "Floor.001", "Ceiling.001", "CeilRing.001", "NeonRing", "RoomFill", "W.003"]):
            continue
        to_delete.append(c)
        for sc in c.children_recursive:
            to_delete.append(sc)
    
    # Delete collected objects
    for o in set(to_delete):
        bpy.data.objects.remove(o, do_unlink=True)
    print("Cleaned up Lab 2 (BriefingRoom) interior while preserving shell and architecture.")

# -------------------------------------------------------------
# 6. Clean up Lab 3 (ResultsRoom) interior
# -------------------------------------------------------------
results = bpy.data.objects.get("ResultsRoom")
if results:
    to_delete = []
    for c in results.children:
        c_name = c.name
        # Keep structural walls, floor, ceiling, lights, and entrance portal
        if any(keep in c_name for keep in ["ResultsRoom_Wall", "Floor.002", "FloorRing", "Ceiling.002", "CeilRing.002", "RoomFill", "W.024"]):
            continue
        to_delete.append(c)
        for sc in c.children_recursive:
            to_delete.append(sc)
    
    for o in set(to_delete):
        bpy.data.objects.remove(o, do_unlink=True)
    print("Cleaned up Lab 3 (ResultsRoom) interior while preserving shell and architecture.")

# -------------------------------------------------------------
# 7. Create Corridor_North connecting MainLobby to Lab 1
# -------------------------------------------------------------
corr_south = bpy.data.objects.get("Corridor_South")
if corr_south:
    corr_north = duplicate_hierarchy(corr_south, None, "_North")
    corr_north.name = "Corridor_North"
    corr_north.location = (0.0, 25.0, 0.0)
    corr_north.rotation_euler = (0.0, 0.0, math.pi / 2.0) # Points North
    print("Created Corridor_North at (0, 25, 0).")

# -------------------------------------------------------------
# 8. Create Lab 1 Room at (0, 48, 0)
# -------------------------------------------------------------
tut_room = bpy.data.objects.get("TutorialRoom")
if tut_room:
    lab1_room = duplicate_hierarchy(tut_room, None, "_Lab1")
    lab1_room.name = "Lab1_FirstLaw"
    lab1_room.location = (0.0, 48.0, 0.0)
    lab1_room.rotation_euler = (0.0, 0.0, math.pi) # Portal aligns to Corridor_North at Y=34
    
    # Clean up any tutorial kiosk duplicates from Lab 1
    to_delete = []
    for c in lab1_room.children:
        c_name = c.name
        if any(k in c_name for k in ["W.028", "W.029", "W.030", "W.031", "W.032", "TeleMarker", "TeleBeam", "TeleDisc", "TeleLight"]):
            to_delete.append(c)
            for sc in c.children_recursive:
                to_delete.append(sc)
    for o in set(to_delete):
        bpy.data.objects.remove(o, do_unlink=True)
    
    # Add station anchor empty inside Lab 1
    station1_new = bpy.data.objects.new("Station1_FirstLaw", None)
    station1_new.empty_display_type = 'PLAIN_AXES'
    station1_new.location = (0.0, 0.0, 0.0)
    bpy.context.scene.collection.objects.link(station1_new)
    station1_new.parent = lab1_room
    print("Created Lab1_FirstLaw room at (0, 48, 0) with internal Station1_FirstLaw anchor.")

# -------------------------------------------------------------
# 9. Save blend file and export to FBX
# -------------------------------------------------------------
blend_path = "Assets/Environment/UnityNewtonFullEnv.blend"
fbx_path = "Assets/Environment/UnityNewtonFullEnv.fbx"

bpy.ops.wm.save_mainfile(filepath=blend_path)
print(f"Saved modified blend to {blend_path}")

# FBX Export settings matching original export
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
    use_subsurf=False,
    use_armature_deform_only=False,
    bake_anim=False
)
print(f"Exported reorganized environment FBX to {fbx_path}")
print("=== Reorganization script completed successfully! ===")

