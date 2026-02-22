namespace Game;

using Godot;

public static class GameActions
{
    public static readonly StringName PauseGame = new("pause_game");

    public static readonly StringName PlayerForward = "player_forward";
    public static readonly StringName PlayerBackward = "player_backward";
    public static readonly StringName PlayerStrafeLeft = "player_left";
    public static readonly StringName PlayerStrafeRight = "player_right";

    public static readonly StringName PlayerRun = "player_run";

    public static readonly StringName PlayerJump = "player_jump";

    public static readonly StringName PlayerFire = "fire";
}
