"""
A script to automatically export blender objects into .glb format for use in Godot.
- Separates out top-level objects (objects with no parent) into their own files
- Only exports objects that are selected AND a top-level parent. Will ignore child objects.
- Names files based on the base name (e.g. for an object named car-col, it will export as car.glb)

Note: View the blender console for debugging if you run into any errors. This script is intended
to run inside Blender's scripting window.
"""

__author__ = "Aman Anas"

### Constants to change import settings

RECENTER = False
""" 
Whether or not we should recenter each object on the origin before exporting.
Set this to False for when you want the Godot object to have a non-zero default origin position.
Useful in certain situations.
"""

import bpy
import os

SAVE_LOCATION = "/batch_exports/"
"""
The top-level save location of the models. 
e.g. if set to '/batch_exports/', 
- File is in /assets/blend/coolcar.blend
- Objects will be placed in /assets/batch_exports/coolcar/<object_name_here>.glb

"""


file_dir = os.path.dirname(bpy.data.filepath)
file_name = os.path.splitext(os.path.basename(bpy.data.filepath))[0]

save_path = os.path.dirname(file_dir) + SAVE_LOCATION
print("Save path:", save_path)

if not file_dir:
    raise Exception("Blend file is not saved")

view_layer = bpy.context.view_layer

obj_active = view_layer.objects.active

initial_selection = bpy.context.selected_objects
selection = initial_selection  # bpy.context.scene.objects

bpy.ops.object.select_all(action="DESELECT")

for obj in selection:
    print("Exporting", obj, obj.name)

    if obj.parent is not None:
        # Skip non-top-level objects
        print(obj.parent)
        continue

    obj.select_set(True)
    for child in obj.children_recursive:
        child.select_set(True)

    # Some exporters only use the active object
    view_layer.objects.active = obj

    name: str = bpy.path.clean_name(obj.name)
    name = name.split("-")[0]

    fn = os.path.join(save_path + f"/{file_name}/", name)

    print(fn)

    saved_loc = tuple(obj.location)

    if RECENTER:
        obj.location = (0, 0, 0)

    bpy.ops.export_scene.gltf(
        filepath=fn + ".gltf",
        export_format="GLTF_SEPARATE",
        export_apply=True,
        use_selection=True,
        export_animation_mode="NLA_TRACKS",
    )

    obj.location = saved_loc

    obj.select_set(False)
    for child in obj.children_recursive:
        child.select_set(False)

    print("written:", fn)


view_layer.objects.active = obj_active

for obj in initial_selection:
    obj.select_set(True)
