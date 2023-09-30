using Godot;
using System;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    private float moveSpeed = 200f;
    private AnimatedSprite animatedSprite;
    private Vector2 currentDirection = Vector2.Zero;
    private int detectionIncrement = 0;
    private string PlayerState;

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector2 inputDirection = GetInputDirection();
        MoveAndHandleAnimation(inputDirection, delta);
        HandleInteractions();
    }

    private Vector2 GetInputDirection()
    {
        Vector2 direction = new Vector2();

        if (Input.IsActionPressed("ui_up"))
            direction.y -= 1;
        if (Input.IsActionPressed("ui_down"))
            direction.y += 1;
        if (Input.IsActionPressed("ui_left"))
            direction.x -= 1;
        if (Input.IsActionPressed("ui_right"))
            direction.x += 1;

        return direction.Normalized();
    }

    private void MoveAndHandleAnimation(Vector2 direction, float delta)
    {
        if (direction != Vector2.Zero) // Si ca bouge
        {
            currentDirection = direction; // Update de la directiton
            MoveAndSlide(direction * moveSpeed);
            PlayerState = "Walking";
            UpdateWalkingAnimation();
        }
        else
        {
            // Pas d'input I guess
            PlayerState = "Idle";
            UpdateIdlingAnimation();
        }
    }

    private void UpdateWalkingAnimation()
    {
        if (currentDirection == Vector2.Up)
        {
            animatedSprite.Animation = "walk_up";
        }
        else if (currentDirection == Vector2.Down)
        {
            animatedSprite.Animation = "walk_down";
        }
        else if (currentDirection == Vector2.Left)
        {
            animatedSprite.Animation = "walk_left";
            animatedSprite.FlipH = true;
        }   
        else if (currentDirection == Vector2.Right)
        {
            animatedSprite.Animation = "walk_right";
            animatedSprite.FlipH = false;
        }

        if (!animatedSprite.Playing)
            animatedSprite.Play();
    }

    private void UpdateIdlingAnimation()
    {
        if (currentDirection == Vector2.Up)
        {
            animatedSprite.Animation = "idle_up";
        }
        else if (currentDirection == Vector2.Down)
        {
            animatedSprite.Animation = "idle_down";
        }
        else if (currentDirection == Vector2.Left)
        {
            animatedSprite.Animation = "idle_left";
            animatedSprite.FlipH = true;
        }   
        else if (currentDirection == Vector2.Right)
        {
            animatedSprite.Animation = "idle_right";
            animatedSprite.FlipH = false;
        }

        if (!animatedSprite.Playing)
            animatedSprite.Play();
    }    

    private void HandleInteractions()
    {
        if (Input.IsActionJustPressed("ui_select")) // Touche 'E' est mapped a "ui_select" dans la map
        {
            // @todo - gerer interactions avec artifacts et interactable objects
            GD.Print("Interaction Key Pressed!");
        }
    }

    // Placeholder lorsque joueur collide
    private void OnAreaEntered(Area2D area)
    {
        // Identifier type de zone
        if (area.IsInGroup("guards"))
        {
            GD.Print("Collided with guard!");
            AddDetection(10);
        }
        else if (area.IsInGroup("cameras"))
        {
            GD.Print("Detected by camera!");
            AddDetection(5);
        }
        else if (area.IsInGroup("artifacts"))
        {
            GD.Print("Near artifact!");
        }
    }

    private void AddDetection(int nbDetection)
    {
        detectionIncrement += nbDetection;
        // @todo - Update le UI
        if (detectionIncrement >= 0)
        {
            // @todo - Gerer l'augmentation et eventuellement game over si = 100
            GD.Print("BUSTED amigo!");
        }
    }
}
