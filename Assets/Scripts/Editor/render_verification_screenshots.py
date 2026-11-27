#!/usr/bin/env python3
"""
Render 6 high-fidelity verification screenshots of the Newton VR environment using Blender Workbench.
"""

import bpy
import math
import mathutils
import os

OUTPUT_DIR = "Assets/Screenshots"
os.makedirs(OUTPUT_DIR, exist_ok=True)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 1920
scene.render.resolution_y = 1080
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGB'

scene.display.shading.type = 'SOLID'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_shadows = True
scene.display.shading.shadow_intensity = 0.35
scene.display.shading.studio_light = 'studio.sl'
scene.display.shading.show_specular_highlight = True

# Check if Newton's First Law apparatus model is already under Station1_FirstLaw
s1 = bpy.data.objects.get("Station1_FirstLaw")
apparatus_objects = []
if s1 and len(s1.children) > 0:
    print(f"Station1_FirstLaw already has {len(s1.children)} apparatus objects.")
else:
    apparatus_path = "Assets/Environment/Newtonsfirstlawexpmodel.blend"
    if os.path.exists(apparatus_path):
        with bpy.data.libraries.load(apparatus_path, link=False) as (data_from, data_to):
            data_to.objects = data_from.objects
        for obj in data_to.objects:
            if obj is not None:
                bpy.context.collection.objects.link(obj)
                apparatus_objects.append(obj)
                obj.location.y += 48.0
        print(f"Loaded {len(apparatus_objects)} temporary apparatus objects into Lab 1 at Y=48")

def render_shot(name, location, target, lens_mm=26):
    for o in [o for o in bpy.data.objects if o.type == 'CAMERA' and o.name.startswith('VerifCam')]:
        bpy.data.objects.remove(o, do_unlink=True)

    cam_data = bpy.data.cameras.new('VerifCam_Data')
    cam_data.lens = lens_mm
    cam_data.clip_start = 0.1
    cam_data.clip_end = 600.0

    cam_obj = bpy.data.objects.new('VerifCam', cam_data)
    bpy.context.collection.objects.link(cam_obj)
    cam_obj.location = location

    direction = (mathutils.Vector(target) - mathutils.Vector(location)).normalized()
    cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

    scene.camera = cam_obj
    scene.render.filepath = f"{OUTPUT_DIR}/{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"Rendered: {name}.png")

# Shot 1: Standing in central MainLobby facing North directly at Lab 1 entrance and prominent sign
render_shot(
    "CentralHall_Facing_Lab1",
    location=(0.0, 5.0, 1.7),
    target=(0.0, 15.9, 3.8),
    lens_mm=26
)

# Shot 2: Standing in central MainLobby looking East at Lab 2 entrance and EXPERIMENT 2 sign
render_shot(
    "CentralHall_Facing_Lab2",
    location=(3.0, 0.0, 1.7),
    target=(15.9, 0.0, 4.3),
    lens_mm=24
)

# Shot 3: Standing in central MainLobby looking West at Lab 3 entrance and EXPERIMENT 3 sign
render_shot(
    "CentralHall_Facing_Lab3",
    location=(-3.0, 0.0, 1.7),
    target=(-15.9, 0.0, 4.3),
    lens_mm=24
)

# Shot 4: Inside Lab 1 facing the complete Newton's First Law apparatus from the 'T' Teleport spawn point
render_shot(
    "Inside_Lab1_FirstLaw",
    location=(0.0, 42.5, 1.8),
    target=(0.0, 48.0, 1.7),
    lens_mm=26
)

# Shot 5: Interactive perspective inside Lab 1 showing cart, track, ball, and control console
render_shot(
    "Inside_Lab1_Apparatus_Detail",
    location=(-2.8, 44.5, 1.7),
    target=(0.2, 48.0, 1.5),
    lens_mm=28
)

# Shot 6: Overview of the Central Exhibition Hall from South entrance (clean, no duplicate apparatus)
render_shot(
    "CentralHall_Overview_From_Entrance",
    location=(0.0, -9.0, 1.8),
    target=(0.0, 10.0, 2.5),
    lens_mm=20
)

# Shot 7: Inside Lab 2 facing the complete Newton's Second Law apparatus (Cart A 1kg, Cart B 5kg, track, telemetry)
render_shot(
    "Inside_Lab2_SecondLaw",
    location=(39.0, 0.0, 1.9),
    target=(48.0, 0.0, 1.5),
    lens_mm=28
)

# Shot 8: Interactive perspective inside Lab 2 showing dual carts, mass blocks, and control console
render_shot(
    "Inside_Lab2_Apparatus_Detail",
    location=(43.5, 3.2, 1.8),
    target=(48.0, -0.4, 1.4),
    lens_mm=26
)

# Clean up temporary imported objects
for obj in apparatus_objects:
    try:
        bpy.data.objects.remove(obj, do_unlink=True)
    except:
        pass

print("All 8 verification screenshots rendered successfully!")
