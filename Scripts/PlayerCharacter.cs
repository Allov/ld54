using Godot;
using System;
using System.Collections.Generic;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public float baseMovementSpeed = 50f;
    private float calculatedMovementSpeed;
    [Export]
    private float runSpeedMultiplier = 1.25f; // Un multiplicateur à la vistesse de base au cas où des items biendrait jouer sur le base factor.
    [Export]
    private float maxStamina = 100f; // Maximum de stamina.
    [Export]
    private float staminaDrainRate = 10f; // Combien vite la stamina s'épuise en courrant
    [Export]
    private float staminaRecoveryRate = 5f; // Combien vite la stamina revient quand joueur ne court pas.
    private float currentStamina;
    private bool isRunning = false;
    private AnimatedSprite playerAnimatedSprite;
    private Vector2 currentDirection = Vector2.Zero;
    private int detectionIncrement = 0;
    private string PlayerState;
    public List<Artifact> collectedArtifacts = new List<Artifact>();
    private Node2D interactionBubbleSprite;
    private AnimationPlayer interactionBubbleAnimationPlayer;
    public bool nearAGuard, nearArtifact, nearExitZone;


    // todo: array/list d'artifacts

    public override void _Ready()
    {
        playerAnimatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
        interactionBubbleAnimationPlayer = GetNode<AnimationPlayer>("AnimationBubble");
        interactionBubbleSprite = GetNode<Node2D>("InteractionBubble");
        interactionBubbleSprite.Visible = false;

        calculatedMovementSpeed = baseMovementSpeed;
        currentStamina = maxStamina;
        nearAGuard = false;
        nearArtifact = false;
        nearExitZone = false;
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector2 inputDirection = GetInputDirection();
        HandleRunning(delta);
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

    private void HandleRunning(float delta)
    {
        isRunning = Input.IsActionPressed("run") && currentStamina > 0; // Check si le perso court et a de la stamina.

        if (isRunning)
        {
            calculatedMovementSpeed = baseMovementSpeed * runSpeedMultiplier; // Augmente la vitesse
            currentStamina -= staminaDrainRate * delta; // Drainer la stamina.
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina); // Check pour que la stamina va pas en bas de 0
        }
        else
        {
            calculatedMovementSpeed = baseMovementSpeed; // On restore vitesse .
            currentStamina += staminaRecoveryRate * delta; // Restaure la stamina.
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina); // Check de stamina pour les valeurs min max.
        }
        GD.Print("Stamina value: " + currentStamina);
    }

    private void MoveAndHandleAnimation(Vector2 direction, float delta)
    {
        if (direction != Vector2.Zero) // Si ca bouge
        {
            currentDirection = direction; // Update de la directiton
            MoveAndSlide(direction * calculatedMovementSpeed);
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
            playerAnimatedSprite.Animation = "walk_up";
        }
        else if (currentDirection == Vector2.Down)
        {
            playerAnimatedSprite.Animation = "walk_down";
        }
        else if (currentDirection == Vector2.Left)
        {
            playerAnimatedSprite.Animation = "walk_left";
            playerAnimatedSprite.FlipH = true;
        }
        else if (currentDirection == Vector2.Right)
        {
            playerAnimatedSprite.Animation = "walk_right";
            playerAnimatedSprite.FlipH = false;
        }

        if (!playerAnimatedSprite.Playing)
            playerAnimatedSprite.Play();
    }

    private void UpdateIdlingAnimation()
    {
        if (currentDirection == Vector2.Up)
        {
            playerAnimatedSprite.Animation = "idle_up";
        }
        else if (currentDirection == Vector2.Down)
        {
            playerAnimatedSprite.Animation = "idle_down";
        }
        else if (currentDirection == Vector2.Left)
        {
            playerAnimatedSprite.Animation = "idle_left";
            playerAnimatedSprite.FlipH = true;
        }
        else if (currentDirection == Vector2.Right)
        {
            playerAnimatedSprite.Animation = "idle_right";
            playerAnimatedSprite.FlipH = false;
        }

        if (!playerAnimatedSprite.Playing)
            playerAnimatedSprite.Play();
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
        if (area.CollisionLayer == 1)
        {
            GD.Print("Near a guard!");
            nearAGuard = true;
            AddDetection(5);
        }
        if (area.CollisionLayer == 5) //artifacts
        {
            GD.Print("Near artifact!");
            nearArtifact = true;
            interactionBubbleSprite.Visible = true;
            Artifact artifact = area as Artifact;
            if (artifact != null)
            {
                artifact.Collect();
                collectedArtifacts.Add(artifact);
            }
        }
        if (area.CollisionLayer == 10)
        {
            GD.Print("Near exit zone!");
            nearExitZone = true;
        }
    }

    private void OnAreaExit(Area2D area)
    {
        if (area.CollisionLayer == 1)
        {
            nearAGuard = false;
        }
        if (area.CollisionLayer == 5)
        {
            nearArtifact = false;
        }
        if (area.CollisionLayer == 10)
        {
            nearExitZone = false;
        }

    }

    private void AddDetection(int detectionMeter)
    {
        detectionIncrement += detectionMeter;
        // @todo - Update le UI
        if (detectionIncrement >= 0)
        {
            // @todo - Gerer l'augmentation et eventuellement game over si = 100
            GD.Print("BUSTED amigo!");
        }
    }

    // C'est cette méthode qu'on doit call à partir du script de UI du sac.
    public void DropArtifact(Artifact artifactToDrop)
    {
        if (collectedArtifacts.Count > 0)
        {
            artifactToDrop.PanicDropThisArtifact(this.Position);
            collectedArtifacts.Remove(artifactToDrop); // On enleve de la liste 'logique'
        }
    }
}
