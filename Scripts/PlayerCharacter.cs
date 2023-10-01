using Godot;
using System;
using System.Collections.Generic;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public float baseMovementSpeed = 50f;
    private Bag Bag;
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
    public bool isRunning = false;
    private AnimatedSprite playerAnimatedSprite;
    private Vector2 currentDirection = Vector2.Zero;
    private int detectionIncrement = 0;
    private string PlayerState;
    public List<Artifact> collectedArtifacts = new List<Artifact>();
    private Node2D interactionBubbleSprite;
    private AnimationPlayer interactionBubbleAnimationPlayer;
    public bool nearAGuard, nearArtifact, nearExitZone;
    [Export]
    private float baseNoiseRadius = 128f; // Default noise radius when walking.
    [Export]
    private float noiseMultiplier = 1f;  // Default noise multiplier (e.g., different terrains or player states could adjust this).
    [Export]
    private float runningNoiseFactor = 2f; // Multiplier for noise when running.
    [Export]
    private float noiseReductionTime = 0.5f;
    private float idleTime = 0f;
    public bool isMakingNoise;

    public CircleShape2D noiseShape;

    public Artifact HeldArtifact;

    public Artifact nearestArtifact;


    // todo: array/list d'artifacts

    public override void _Ready()
    {
        playerAnimatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
        interactionBubbleAnimationPlayer = GetNode<AnimationPlayer>("AnimationBubble");
        interactionBubbleSprite = GetNode<Node2D>("InteractionBubble");
        interactionBubbleSprite.Visible = false;
        Bag = GetNode<Bag>("Bag");


        calculatedMovementSpeed = baseMovementSpeed;
        currentStamina = maxStamina;
        nearAGuard = false;
        nearArtifact = false;
        nearExitZone = false;

        Bag.OnPlacedArtifact += OnPlacedArtifact;
        Bag.OnDropArtifact += OnDropArtifact;
    }

    private void OnDropArtifact(object sender, EventArgs e)
    {
        if (HeldArtifact != null)
        {
            Bag.RemoveChild(HeldArtifact.ArtifactShape);
            HeldArtifact.AddChild(HeldArtifact.ArtifactShape);
            HeldArtifact.ArtifactShape.Visible = false;
            nearArtifact = true;
            nearestArtifact = HeldArtifact;

            HeldArtifact = null;
        }
    }

    private void OnPlacedArtifact(object sender, EventArgs e)
    {
        collectedArtifacts.Add(HeldArtifact);
        HeldArtifact.Collect();
        HeldArtifact = null;
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector2 inputDirection = GetInputDirection();
        HandleRunning(delta);
        MoveAndHandleAnimation(inputDirection, delta);
        CheckAndSetBubbleVisibility();
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

        if (direction != Vector2.Zero)
        {
            idleTime = 0;
        }

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
    }

    private void MoveAndHandleAnimation(Vector2 direction, float delta)
    {
        if (HeldArtifact != null) return;

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

    private void CheckAndSetBubbleVisibility()
    {
        if (nearArtifact || nearExitZone)
        {
            interactionBubbleSprite.Visible = true;
        }
        else
        {
            interactionBubbleSprite.Visible = false;
        }
    }

    private void HandleInteractions()
    {
        if (Input.IsActionJustPressed("ui_select")) // Touche 'E' est mapped a "ui_select" dans la map
        {
            GD.Print("Interaction Key Pressed!");

            if (nearestArtifact != null)
            {
                HeldArtifact = nearestArtifact;
                nearestArtifact = null;
                nearArtifact = false;

                HeldArtifact.RemoveChild(HeldArtifact.ArtifactShape);
                Bag.CurrentArtifactShape = HeldArtifact.ArtifactShape;
                Bag.Visible = true;
                HeldArtifact.ArtifactShape.Visible = true;

                Bag.AddChild(HeldArtifact.ArtifactShape);
            }

            if (nearExitZone)
            {
                World world = GetParent<World>();
                if (world != null)
                {
                    world.TriggerEndLevel();
                }
            }
        }
    }

    private void AddDetection(int detectionMeter)
    {
        detectionIncrement += detectionMeter;
        // @todo - Update le UI
        if (detectionIncrement >= 0)
        {
            // @todo - Gerer l'augmentation et eventuellement game over si = 100. Game over.
            GD.Print("BUSTED amigo!");
        }
    }

    public void PickUp(Artifact artifact)
    {
        HeldArtifact = artifact;
    }

    public void DropHeld()
    {
        HeldArtifact?.PanicDropThisArtifact(this.GlobalPosition);
        HeldArtifact = null;
    }

    // C'est cette méthode qu'on doit call à partir du script de UI du sac.
    public void DropArtifact(Artifact artifactToDrop)
    {
        if (collectedArtifacts.Count > 0)
        {
            artifactToDrop.PanicDropThisArtifact(this.GlobalPosition);
            collectedArtifacts.Remove(artifactToDrop); // On enleve de la liste 'logique'
        }
    }
}
