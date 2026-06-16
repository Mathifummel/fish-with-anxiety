using Godot;

public partial class Jellyfish : CharacterBody2D
{
	[Export] public float Speed = 74f;
	[Export] public float RotationSpeed = 2.1f;
	[Export] public float DetectionRadius = 650f;
	[Export] public float ComfortRadius = 230f;
	[Export] public float OrbitStrength = 1.05f;
	[Export] public float WanderInfluence = 0.42f;
	[Export] public float PulseFrameRate = 4.8f;
	[Export] public float PulseAmplitude = 0.09f;
	[Export] public float CollisionRadius = 32f;
	[Export] public float SandBoundaryExtraPadding = 6f;
	[Export] public float SandBoundaryPushSpeed = 120f;

	public Node2D Player;

	private Sprite2D sprite;
	private Texture2D[] pulseFrames;
	private Vector2 wanderDir = Vector2.Right;
	private Vector2 currentVelocity = Vector2.Zero;
	private float directionTimer = 0f;
	private float pulseTime = 0f;
	private float orbitSide = 1f;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		pulseFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Qualle.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Qualle2.png")
		};

		rng.Randomize();
		orbitSide = rng.Randf() < 0.5f ? -1f : 1f;
		RandomizeWander();
		UpdatePulseFrame();
		AddToGroup("jellyfish");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		pulseTime += dt;
		directionTimer -= dt;

		if (directionTimer <= 0f)
			RandomizeWander();

		Vector2 desiredDir = wanderDir;
		float speedMultiplier = 1f;

		if (Player != null)
		{
			Main main = GetTree().GetFirstNodeInGroup("game_main") as Main;
			Vector2 toPlayer = Player.GlobalPosition - GlobalPosition;
			float distance = toPlayer.Length();

			if (main != null && main.ShouldNpcsFlee)
			{
				desiredDir = GetAwayDirection(toPlayer);
				speedMultiplier = 1.18f;
			}
			else if (distance < DetectionRadius && distance > 1f)
			{
				float alert = 1f - Mathf.Clamp(distance / DetectionRadius, 0f, 1f);
				Vector2 toward = toPlayer / distance;
				Vector2 tangent = new Vector2(-toward.Y, toward.X) * orbitSide;
				Vector2 radial = distance > ComfortRadius
					? toward * Mathf.Lerp(0.25f, 0.78f, alert)
					: -toward * 0.58f;

				desiredDir =
					(radial + tangent * OrbitStrength + wanderDir * WanderInfluence)
					.Normalized();
				speedMultiplier = Mathf.Lerp(1f, 1.34f, alert);
			}
		}

		Vector2 separation = GetJellyfishSeparation();
		if (separation.LengthSquared() > 0.01f)
			desiredDir = (desiredDir + separation).Normalized();

		Vector2 targetVelocity = desiredDir * Speed * speedMultiplier;
		currentVelocity = currentVelocity.Lerp(targetVelocity, Mathf.Clamp(4.2f * dt, 0f, 1f));
		Velocity = currentVelocity;

		MoveAndSlide();
		currentVelocity = SandBoundary.ClampCharacterAboveSand(
			this,
			currentVelocity,
			CollisionRadius + SandBoundaryExtraPadding,
			SandBoundaryPushSpeed
		);
		UpdateVisual(dt);
	}

	private Vector2 GetAwayDirection(Vector2 toPlayer)
	{
		if (toPlayer.LengthSquared() < 1f)
			return Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));

		return (-toPlayer).Normalized();
	}

	private Vector2 GetJellyfishSeparation()
	{
		Vector2 separation = Vector2.Zero;

		foreach (Node node in GetTree().GetNodesInGroup("jellyfish"))
		{
			if (node == this || node is not Jellyfish other)
				continue;

			float distance = GlobalPosition.DistanceTo(other.GlobalPosition);

			if (distance > 0f && distance < 96f)
				separation += (GlobalPosition - other.GlobalPosition).Normalized() / Mathf.Max(distance, 8f);
		}

		return separation * 18f;
	}

	private void UpdateVisual(float dt)
	{
		UpdatePulseFrame();

		float pulse = Mathf.Sin(pulseTime * Mathf.Tau * 0.85f) * PulseAmplitude;
		if (sprite != null)
			sprite.Scale = new Vector2(0.72f + pulse * 0.4f, 0.72f - pulse);

		if (Velocity.Length() <= 0.1f || sprite == null)
			return;

		float targetRotation = Velocity.Angle() + Mathf.Pi * 0.5f;
		float wobble = Mathf.Sin(pulseTime * 3.6f + GetInstanceId()) * 0.07f;
		sprite.Rotation = Mathf.LerpAngle(
			sprite.Rotation,
			targetRotation + wobble,
			Mathf.Clamp(dt * RotationSpeed, 0f, 1f)
		);
	}

	private void UpdatePulseFrame()
	{
		if (sprite == null || pulseFrames == null || pulseFrames.Length == 0)
			return;

		int frameIndex = Mathf.PosMod((int)(pulseTime * PulseFrameRate), pulseFrames.Length);
		Texture2D texture = pulseFrames[frameIndex];

		if (texture != null && sprite.Texture != texture)
			sprite.Texture = texture;
	}

	private void RandomizeWander()
	{
		wanderDir = Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));
		directionTimer = rng.RandfRange(1.4f, 3.3f);

		if (rng.Randf() < 0.22f)
			orbitSide *= -1f;
	}
}
