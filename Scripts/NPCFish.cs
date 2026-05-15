using Godot;

public partial class NPCFish : CharacterBody2D
{
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

	public Node2D Player; // ✅ assigned from Main

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
		if (Player == null) return;

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
					float safeDist = Mathf.Max(dist, 10f); // ✅ FIX
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
			boostTimer -= (float)delta;
		}

		Vector2 finalDir = (offsetDir + separation).Normalized();

		Velocity = finalDir * currentSpeed;
		MoveAndSlide();

		// Rotation + wiggle
		if (Velocity.Length() > 0.1f)
		{
			swimTime += (float)delta;

			float targetRotation = Velocity.Angle() + Mathf.Pi;
			float wiggle = Mathf.Sin(swimTime * SwimFrequency) * SwimAmplitude;

			fishSprite.Rotation = Mathf.LerpAngle(
				fishSprite.Rotation,
				targetRotation + wiggle,
				(float)delta * RotationSpeed
			);
		}
	}
}
