using Godot;

public partial class NPCFish : CharacterBody2D
{
	public enum MovementMode
	{
		Chase,
		Crossing
	}

	[Export] public float Speed = 120f;
	[Export] public float RotationSpeed = 3f;

	[Export] public float SwimAmplitude = 0.15f;
	[Export] public float SwimFrequency = 4f;

	// Boost
	[Export] public float BoostMultiplier = 2.5f;
	[Export] public float BoostDuration = 1.2f;
	[Export] public float BoostChance = 0.01f;

	// Separation
	[Export] public float SeparationRadius = 80f;
	[Export] public float SeparationStrength = 200f;

	// Offset movement
	[Export] public float ApproachOffsetStrength = 100f;
	[Export] public float CollisionRadius = 40f;

	public Node2D Player; // assigned from Main
	public MovementMode Mode = MovementMode.Chase;
	public Vector2 CrossingDirection = Vector2.Right;
	public float CrossingLifetime = 5.5f;

	private Sprite2D fishSprite;
	private float swimTime = 0f;

	private float boostTimer = 0f;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		fishSprite = GetNode<Sprite2D>("Sprite2D");
		rng.Randomize();

		AddToGroup("fish");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (Mode == MovementMode.Crossing)
		{
			UpdateCrossingMovement(dt);
			return;
		}

		if (Player == null) return;

		Main main = GetTree().GetFirstNodeInGroup("game_main") as Main;

		if (main != null && main.ShouldNpcsFlee)
		{
			UpdateFleeMovement(dt, main);
			return;
		}

		Vector2 toPlayer = (Player.Position - Position).Normalized();

		// Offset movement
		Vector2 perpendicular = new Vector2(-toPlayer.Y, toPlayer.X);
		float offset = Mathf.Sin(Time.GetTicksMsec() * 0.001f + GetInstanceId()) * ApproachOffsetStrength;
		Vector2 offsetDir = toPlayer + perpendicular * offset * 0.01f;

		// Separation
		Vector2 separation = Vector2.Zero;

		foreach (Node node in GetTree().GetNodesInGroup("fish"))
		{
			if (node == this) continue;

			if (node is NPCFish other)
			{
				float dist = Position.DistanceTo(other.Position);

				if (dist < SeparationRadius && dist > 0)
				{
					float safeDist = Mathf.Max(dist, 10f);
					separation += (Position - other.Position).Normalized() / safeDist;
				}
			}
		}

		separation *= SeparationStrength;

		// Random boost
		if (rng.Randf() < BoostChance && boostTimer <= 0)
		{
			boostTimer = BoostDuration;
		}

		float currentSpeed = Speed;

		if (boostTimer > 0)
		{
			currentSpeed *= BoostMultiplier;
			boostTimer -= dt;
		}

		Vector2 finalDir = (offsetDir + separation).Normalized();

		Velocity = finalDir * currentSpeed;
		MoveAndSlide();

		UpdateRotation(dt);
	}

	private void UpdateCrossingMovement(float dt)
	{
		CrossingLifetime -= dt;

		if (CrossingLifetime <= 0f)
		{
			QueueFree();
			return;
		}

		Velocity = CrossingDirection.Normalized() * Speed;
		MoveAndSlide();
		UpdateRotation(dt);
	}

	private void UpdateFleeMovement(float dt, Main main)
	{
		Vector2 awayFromPlayer = (Position - Player.Position);

		if (awayFromPlayer.LengthSquared() < 1f)
			awayFromPlayer = Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));

		awayFromPlayer = awayFromPlayer.Normalized();

		Vector2 separation = Vector2.Zero;

		foreach (Node node in GetTree().GetNodesInGroup("fish"))
		{
			if (node == this || node is not NPCFish other)
				continue;

			float dist = Position.DistanceTo(other.Position);

			if (dist < SeparationRadius && dist > 0f)
				separation += (Position - other.Position).Normalized() / Mathf.Max(dist, 10f);
		}

		separation *= SeparationStrength * 0.35f;

		Vector2 fleeDir = (awayFromPlayer + separation).Normalized();
		float fleeSpeed = Speed * main.NpcFleeSpeedMultiplier;

		Velocity = fleeDir * fleeSpeed;
		MoveAndSlide();
		UpdateRotation(dt);
	}

	private void UpdateRotation(float dt)
	{
		if (Velocity.Length() > 0.1f)
		{
			swimTime += dt;

			float targetRotation = Velocity.Angle() + Mathf.Pi;
			float wiggle = Mathf.Sin(swimTime * SwimFrequency) * SwimAmplitude;

			fishSprite.Rotation = Mathf.LerpAngle(
				fishSprite.Rotation,
				targetRotation + wiggle,
				dt * RotationSpeed
			);
		}
	}
}
