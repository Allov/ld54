using Godot;
using System.Collections.Generic;
using System;
public class Guard : KinematicBody2D
{
    [Export]
    private float moveSpeed = 10f;
    [Export]
    private float idleTimePerPatrolPoint = 3f;
    [Export]
    private float detectionTimerMax = 1.0f;

    private AnimatedSprite animatedSprite;
    private string idleAnimation = "idle_up";
    private Path2D path2D;
    private PathFollow2D pathFollow2D;
    private Area2D detectionArea;
    private List<Vector2> patrolPoints = new List<Vector2>();
    private int currentPatrolPoint = 1;
    private Vector2 lastPosition;
    private bool isPlayerDetected = false;
    private float detectionTimer;
    public float currentIdleTimer = 0f;

    Area2D noiseDetectionArea;
    private bool isDetectingNoise = false;
    private PlayerCharacter playerInNoiseArea;
    private bool investigationNoise = false;

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
        pathFollow2D = GetParent() as PathFollow2D;
        path2D = pathFollow2D.GetParent() as Path2D;
        detectionArea = GetNode<Area2D>("DetectionArea");
        detectionTimer = detectionTimerMax;
        lastPosition = GlobalPosition;
        PreparePatrolPoint();

        noiseDetectionArea = GetNode<Area2D>("NoiseDetection");
    }

    public override void _Process(float delta)
    {

        CheckingNoise(delta);

    if (!isPlayerDetected)
    {
        GD.Print("Guard is patrolling."); 
        Patrol(delta);
    }
    else
    {
        GD.Print("Guard has detected the player.");
        PlayerDetected(delta);
    }

    }

    private void CheckingNoise(float delta)
    {
        if (playerInNoiseArea != null)
        {
            if (playerInNoiseArea.isRunning)
            {                           
                if (!isPlayerDetected)
                {
                    isPlayerDetected=true;
                }
            }
            else
            {
                isPlayerDetected=false;
            }
        }
    }

    private void OnNoiseDetection(KinematicBody2D body)
    {
        if (body is PlayerCharacter player)
        {
            playerInNoiseArea = player;
            GD.Print("Guard detected noise!");

            if (player.isRunning)
            {
                isDetectingNoise = true;
                if (!investigationNoise)
                {
                    isPlayerDetected = true;
                }
            }
        }
    }

    private void OnNoiseDetectionAreaExited(KinematicBody2D body)
    {
        if (body is PlayerCharacter player)
        {
            GD.Print("No more noise detected.");
            isPlayerDetected = false; // Est-ce sortir du noise area met automatiquement la vision detection to false? Disons que oui...
            playerInNoiseArea = null;
            isDetectingNoise = false;
            investigationNoise = false;
        }
    }

    private void PreparePatrolPoint()
    {
        for (int i = 0; i < path2D.Curve.GetPointCount(); i++)
        {
            patrolPoints.Add(path2D.Curve.GetPointPosition(i));
        }

        patrolPoints.RemoveAt(patrolPoints.Count - 1);
    }

    private void Patrol(float delta)
    {
        Vector2 difference = pathFollow2D.Position - patrolPoints[currentPatrolPoint];

        if (detectionTimer < detectionTimerMax)
        {
            UpdateIdleAnimation();
            detectionTimer += delta;

            if (detectionTimer > detectionTimerMax)
                detectionTimer = detectionTimerMax;
        }
        else
        {
            if (currentIdleTimer > 0f)
            {
                currentIdleTimer -= delta;
                UpdateIdleAnimation();
            }
            else if (difference.Length() < 5f)
            {
                currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Count;
                currentIdleTimer = idleTimePerPatrolPoint;
            }
            else
            {
                pathFollow2D.Offset = pathFollow2D.Offset + moveSpeed * delta;
                UpdateWalkingAnimation(lastPosition - GlobalPosition);
            }
        }

        lastPosition = GlobalPosition;
    }

    private void PlayerDetected(float delta)
    {
        UpdateIdleAnimation();
        detectionTimer -= delta;

        if (detectionTimer <= 0)
        {
            detectionTimer = 0.0f;
        }
    }

    private void UpdateIdleAnimation()
    {
        animatedSprite.Animation = idleAnimation;

        if (!animatedSprite.Playing)
            animatedSprite.Play();
    }

    private void UpdateWalkingAnimation(Vector2 direction)
    {
        if (direction.x < 0 && Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            animatedSprite.Animation = "walk_left";
            detectionArea.RotationDegrees = 270.0f;
            GetNode<Light2D>("Light2D").RotationDegrees = 270.0f;
            idleAnimation = "idle_left";
            animatedSprite.FlipH = false;
        }
        if (direction.x > 0 && Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            animatedSprite.Animation = "walk_right";
            detectionArea.RotationDegrees = 90.0f;
            GetNode<Light2D>("Light2D").RotationDegrees = 90.0f;
            idleAnimation = "idle_right";
            animatedSprite.FlipH = true;
        }
        if (direction.y > 0 && Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            animatedSprite.Animation = "walk_up";
            detectionArea.RotationDegrees = 180.0f;
            GetNode<Light2D>("Light2D").RotationDegrees = 180.0f;
            idleAnimation = "idle_up";
        }
        if (direction.y < 0 && Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            animatedSprite.Animation = "walk_down";
            detectionArea.RotationDegrees = 0.0f;
            GetNode<Light2D>("Light2D").RotationDegrees = 0.0f;
            idleAnimation = "idle_down";
        }

        if (!animatedSprite.Playing)
            animatedSprite.Play();
    }

    private void OnDetectionAreaEntered(Node2D area)
    {
        if (area.IsInGroup("player"))
        {
            isPlayerDetected = true;
        }
    }

    private void OnDetectionAreaExited(Area2D player)
    {
        if (player.IsInGroup("player"))
        {
            isPlayerDetected = false;
        }
    }
}
