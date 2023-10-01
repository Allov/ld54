using Godot;
using System.Collections.Generic;
using System.Linq;
public class Guard : KinematicBody2D
{
    [Export]
    private float moveSpeed = 10f;
    [Export]
    private float idleTimePerPatrolPoint = 3f;
    [Export]
    private float detectionTimerMax = 2.0f;

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
    private Node2D questionBubbleSprite;
    private Node2D exclamationBubbleSprite;
    private ProgressBar detectionBar;
    public float currentIdleTimer = 0f;

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
        pathFollow2D = GetParent() as PathFollow2D;
        path2D = pathFollow2D.GetParent() as Path2D;
        detectionArea = GetNode<Area2D>("DetectionArea");
        detectionTimer = detectionTimerMax;
        lastPosition = GlobalPosition;
        questionBubbleSprite = GetNode<Node2D>("QuestionBubble");
        questionBubbleSprite.Visible = false;
        exclamationBubbleSprite = GetNode<Node2D>("ExclamationBubble");
        exclamationBubbleSprite.Visible = false;
        detectionBar = GetNode<ProgressBar>("DetectionBar");
        detectionBar.MaxValue = detectionTimerMax;
        PreparePatrolPoint();
    }

    public override void _Process(float delta)
    {
        if (!isPlayerDetected)
            Patrol(delta);
        else
            PlayerDetected(delta);

        Node2D[] detectedBodies = detectionArea.GetOverlappingBodies().Cast<Node2D>().ToArray();
        foreach(Node2D body in detectedBodies)
        {
            if(body is PlayerCharacter player)
            {
                var spaceState = GetWorld2d().DirectSpaceState;
                Godot.Collections.Dictionary result = spaceState.IntersectRay(GlobalPosition, player.GlobalPosition, new Godot.Collections.Array{this}, CollisionMask);

                if (result.Contains("collider") && result["collider"] is KinematicBody2D)
                    isPlayerDetected = true;
            }
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

        if(detectionTimer < detectionTimerMax)
        {
            UpdateIdleAnimation();
            detectionTimer += delta;
            
            if (detectionTimer >= detectionTimerMax)
            {
                detectionTimer = detectionTimerMax;
                HideDetectionFeedback();
            }
            else
                UpdateDetectionFeedback(-delta);
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

        if (detectionTimer <= 0.0f)
        {
            detectionTimer = 0.0f;
        }
        else
            UpdateDetectionFeedback(delta);
    }

    private void UpdateDetectionFeedback(float delta)
    {
        detectionBar.Value += delta;
        detectionBar.Visible = true;

        if (detectionTimer <= detectionTimerMax / 2)
        {
            questionBubbleSprite.Visible = false;
            exclamationBubbleSprite.Visible = true;
        }
        else
        {
            questionBubbleSprite.Visible = true;
            exclamationBubbleSprite.Visible = false;
        }
    }

    private void HideDetectionFeedback()
    {
        questionBubbleSprite.Visible = false;
        exclamationBubbleSprite.Visible = false;
        detectionBar.Value = 0.0f;
        detectionBar.Visible = false;
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
            var spaceState = GetWorld2d().DirectSpaceState;
            Godot.Collections.Dictionary result = spaceState.IntersectRay(GlobalPosition, area.GlobalPosition, new Godot.Collections.Array{this}, CollisionMask);

            if (result.Contains("collider") && result["collider"] is KinematicBody2D)
                isPlayerDetected = true;
        }
    }

    private void OnDetectionAreaExited(Area2D player)
    {
        isPlayerDetected = false;
    }
}
