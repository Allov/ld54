using Godot;
using System;

public class Guard : KinematicBody2D
{
    [Export]
    public float moveSpeed = 100f;
    [Export]
    public Vector2[] patrolPoints; // Array de points de patrol pour creer la "route"

    private AnimatedSprite animatedSprite;
    private int currentPatrolPoint = 0;
    private bool isRedirected = false;  // rediriger par un mur
    private Vector2 currentDirection = Vector2.Zero;

    public override void _Ready()
    {
        //events connectes dans Godot editor
        animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");        
        InitializePatrolPoints();
    }

    public override void _PhysicsProcess(float delta)
    {
        Patrol();
        // @todo - ajouter logique detection ici (poursuivre?)
    }

    private void InitializePatrolPoints()
    {
            patrolPoints = new Vector2[] { new Vector2(100, 100), new Vector2(200, 200) }; // valeurs pour test          
    }

    private void Patrol()
    {

        Vector2 target = patrolPoints[currentPatrolPoint];
        Vector2 moveDirection = target - Position;

        if (moveDirection.Length() < 5f) // Arrive pres du point, on passe au suivant
        {
            currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
            return;
        }

        currentDirection = moveDirection.Normalized();
        MoveAndSlide(currentDirection * moveSpeed);
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        // @todo
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