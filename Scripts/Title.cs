using Godot;
using System;

public class Title : Node2D
{
    [Export]
    public PackedScene WorldScene;
    public override void _Ready()
    {

    }


    public override void _Process(float delta)
    {
        if (Input.IsActionJustPressed("skip_menu"))
        {
            GetTree().ChangeSceneTo(WorldScene);
        }
    }
}
