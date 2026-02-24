extends CharacterBody3D


@export var speed := 4.0
@export var gravity := 9.8
@export var target_path : NodePath
var target: Node3D
@onready var nav := $NavigationAgent3D

func _ready():
	if target_path:
		target = get_node(target_path)

func _physics_process(delta: float):
	if not is_on_floor():
		velocity.y -= gravity * delta

	if target:
		nav.target_position = target.global_position
		
		var next_pos = nav.get_next_path_position()
		var dir = next_pos - global_position
		#dir.y = 0
		
		
		
		#var dir = (target.global_position - global_position)
		#dir.y = 0
		
		if dir.length() > 0.1:
			dir = dir.normalized()
			velocity.x = dir.x * speed
			velocity.z = dir.z * speed
			
			var look_target = target.global_position
			look_target.y = global_position.y
			
			look_at(look_target)

	move_and_slide()
