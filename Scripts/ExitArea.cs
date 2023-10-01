using Godot;
using System;

public class ExitArea : Area2D
{
    private void OnExitAreaEntered(KinematicBody2D body)
    {
        if (body is PlayerCharacter player)
        {
            player.nearExitZone = true;
        }
    }

    private void OnExitAreaExited(KinematicBody2D body)
    {
        if (body is PlayerCharacter player)
        {
            player.nearExitZone = false;
        }
    }
}
