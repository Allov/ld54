using Godot;
using System;

public class Guard : KinematicBody2D
{
    [Export]
    private float moveSpeed = 10f;
    [Export]
    public Vector2[] patrolPoints; // Array de points de patrol pour creer la "route"

    private AnimatedSprite animatedSprite;
    private int currentPatrolPoint = 0;
    private bool isRedirected = false;  // rediriger par un mur
    private Vector2 currentDirection = Vector2.Zero;
    
    [Export]
    public float idleTimePerPatrolPoint = 1f;
    public float currentIdleTimer = 0f;

    public override void _Ready()
    {
        //events connectes dans Godot editor
        animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");        
        // InitializePatrolPoints();
    }

    public override void _PhysicsProcess(float delta)
    {
        Patrol(delta);
        // @todo - ajouter logique detection ici (poursuivre?)
    }

    private void InitializePatrolPoints()
    {
    }

    private void Patrol(float delta)
    {

        Vector2 target = patrolPoints[currentPatrolPoint];
        Vector2 moveDirection = target - Position;

        if (currentIdleTimer > 0f)
        {
            currentIdleTimer -= delta;
            UpdateIdleAnimation();
        }
        else if (moveDirection.Length() < 1f) // Arrive pres du point, on passe au suivant
        {
            currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
            currentIdleTimer = idleTimePerPatrolPoint;
            return;
        }
        else
        {
            currentDirection = moveDirection.Normalized();
            MoveAndSlide(currentDirection * moveSpeed);
            UpdateWalkingAnimation();
        }
    }

    private void UpdateIdleAnimation()
    {
        var y = Mathf.RoundToInt(currentDirection.y);
        var x = Mathf.RoundToInt(currentDirection.x);
        if (y < 0f)
        {
            animatedSprite.Animation = "idle_up";
        }
        else if (y > 0f)
        {
            animatedSprite.Animation = "idle_down";
        }
        else if (x < 0f)
        {
            animatedSprite.Animation = "idle_left";
            animatedSprite.FlipH = true;
        }   
        else if (x > 0f)
        {
            animatedSprite.Animation = "idle_right";
            animatedSprite.FlipH = false;
        }

        if (!animatedSprite.Playing)
            animatedSprite.Play();
    }

    private void UpdateWalkingAnimation()
    {
        var y = Mathf.RoundToInt(currentDirection.y);
        var x = Mathf.RoundToInt(currentDirection.x);
        if (y < 0f)
        {
            animatedSprite.Animation = "walk_up";
        }
        else if (y > 0f)
        {
            animatedSprite.Animation = "walk_down";
        }
        else if (x < 0f)
        {
            animatedSprite.Animation = "walk_left";
            animatedSprite.FlipH = true;
        }   
        else if (x > 0f)
        {
            animatedSprite.Animation = "walk_right";
            animatedSprite.FlipH = false;
        }

        if (!animatedSprite.Playing)
            animatedSprite.Play();
    }

    // Placeholder pour la detection du garde avec le joueur
    private void OnPlayerDetected(Area2D player)
    {
        GD.Print("Player detected!");
    }

    private void OnDetectionAreaEntered(Node2D area)
    {

        if (area.IsInGroup("player"))
        {
            GD.Print("player detected");
            // logique ici pour le '?', alerter autres gardes,
        }

        if (area.IsInGroup("walls"))
        {
            isRedirected = true;
            currentDirection = -currentDirection; // tourne 180 degres
        }
        // ... autre logique de détection
    }

    private void OnDetectionAreaExited(Node2D area)
    {
        if (area.IsInGroup("walls") && isRedirected)
        {
            isRedirected = false;
        }
    }
}