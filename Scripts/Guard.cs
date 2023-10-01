using Godot;
using System.Collections.Generic;

public class Guard : KinematicBody2D
{
	[Export]
	private float moveSpeed = 10f;
	[Export]
	private float idleTimePerPatrolPoint = 3f;

	private AnimatedSprite animatedSprite;
	private string idleAnimation = "idle_up";
	private Path2D path2D;
	private PathFollow2D pathFollow2D;
	private List<Vector2> patrolPoints = new List<Vector2>();
	private int currentPatrolPoint = 1;
	private Vector2 lastPosition;
	private Vector2 currentDirection = Vector2.Zero;
	public float currentIdleTimer = 0f;

	public override void _Ready()
	{
		animatedSprite = GetNode<AnimatedSprite>("AnimatedSprite");
		pathFollow2D = GetParent() as PathFollow2D;
		path2D = pathFollow2D.GetParent() as Path2D;
		lastPosition = GlobalPosition;
		PreparePatrolPoint();
	}

	public override void _Process(float delta)
	{
		Patrol(delta);
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

		if (currentIdleTimer > 0f)
		{
			currentIdleTimer -= delta;
			UpdateIdleAnimation(lastPosition - GlobalPosition);
		}
		else if (difference.Length() < 2f)
		{
			currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Count;
			currentIdleTimer = idleTimePerPatrolPoint;
		}
		else
		{
			pathFollow2D.Offset = pathFollow2D.Offset + moveSpeed * delta;
			UpdateWalkingAnimation(lastPosition - GlobalPosition);
		}

		lastPosition = GlobalPosition;
	}

	private void UpdateIdleAnimation(Vector2 direction)
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
			idleAnimation = "idle_left";
			animatedSprite.FlipH = false;
		}
		if (direction.x > 0 && Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
		{
			animatedSprite.Animation = "walk_right";
			idleAnimation = "idle_right";
			animatedSprite.FlipH = true;
		}
		if (direction.y > 0 && Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
		{
			animatedSprite.Animation = "walk_up";
			idleAnimation = "idle_up";
		}
		if (direction.y < 0 && Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
		{
			animatedSprite.Animation = "walk_down";
			idleAnimation = "idle_down";
		}

		if (!animatedSprite.Playing)
			animatedSprite.Play();
	}

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
	}

	private void OnDetectionAreaExited(Area2D player)
	{

	}
}
