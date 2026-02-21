extends CharacterBody3D


const SPEED = 5.0
const JUMP_VELOCITY = 4.5


# Camera (first-person) controls -- minimal add
# Generate by GitHub Copilot
@export var mouse_sensitivity: Vector2 = Vector2(0.15, 0.15)
var yaw: float = 0.0
var pitch: float = 0.0
@onready var camera: Camera3D = $Camera3D

func _ready() -> void:
	# Generate by GitHub Copilot: capture/hide the mouse for first-person control
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

	# initialize rotations from current transforms
	yaw = rotation.y
	pitch = camera.rotation.x

func _input(event: InputEvent) -> void:
	# Generate by GitHub Copilot: allow toggling mouse capture with Esc (`ui_cancel`).
	# Toggle mouse capture so pressing Esc makes the cursor visible/movable.
	if Input.is_action_just_pressed("ui_cancel"):
		if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
			Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
		else:
			Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

	# Only handle mouse motion to rotate camera (no other changes made).
	if event is InputEventMouseMotion:
		yaw -= event.relative.x * mouse_sensitivity.x * 0.01
		pitch -= event.relative.y * mouse_sensitivity.y * 0.01
		var max_pitch := deg_to_rad(89.0)
		pitch = clamp(pitch, -max_pitch, max_pitch)
		rotation.y = yaw
		camera.rotation.x = pitch


func _physics_process(delta: float) -> void:
	# Add the gravity.
	if not is_on_floor():
		velocity += get_gravity() * delta

	# Handle jump.
	if Input.is_action_just_pressed("ui_accept") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir := Input.get_vector("left", "right", "up", "down")
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	move_and_slide()
