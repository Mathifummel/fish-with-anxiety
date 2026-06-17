using Godot;

public partial class PassiveFish : CharacterBody2D
{
	[Export] public float Speed = 80f;
	[Export] public float RotationSpeed = 3f;
	[Export] public float SwimAmplitude = 0.12f;
	[Export] public float SwimFrequency = 3.5f;
	[Export] public float DirectionChangeTimeMin = 1.8f;
	[Export] public float DirectionChangeTimeMax = 4.2f;
	[Export] public float WanderTurnStrength = 0.45f;
	[Export] public float CollisionRadius = 22f;
	[Export] public float SandBoundaryExtraPadding = 5f;
	[Export] public float WaterSurfaceExtraPadding = 12f;
	[Export] public float SandBoundaryPushSpeed = 110f;

	private Vector2 moveDir = Vector2.Right;
	private Vector2 currentVelocity = Vector2.Zero;
	private float directionTimer = 0f;
	private float swimTime = 0f;
	private float wanderPhase = 0f;
	private int facingDirection = -1;
	private Texture2D[] leftSwimFrames;
	private Texture2D[] rightSwimFrames;

	private Sprite2D fishSprite;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		fishSprite = GetNode<Sprite2D>("Sprite2D");
		leftSwimFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2.png")
		};
		rightSwimFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1 1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2 1.png")
		};
		rng.Randomize();
		RandomizeDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		directionTimer -= dt;

		if (directionTimer <= 0f)
			RandomizeDirection();

		moveDir = moveDir.Rotated(
			Mathf.Sin(wanderPhase + Time.GetTicksMsec() * 0.001f) *
			WanderTurnStrength *
			dt
		).Normalized();

		Vector2 targetVelocity = moveDir * Speed;
		currentVelocity = currentVelocity.Lerp(targetVelocity, 4.5f * dt);
		Velocity = currentVelocity;

		MoveAndSlide();
		currentVelocity = SandBoundary.ClampCharacterInsideWater(
			this,
			currentVelocity,
			CollisionRadius + SandBoundaryExtraPadding,
			WaterSurfaceExtraPadding,
			SandBoundaryPushSpeed
		);

		if (Velocity.Length() > 0.1f)
		{
			swimTime += dt;

			UpdateFacingDirection();
			UpdateSwimFrame();

			float moveAngle = Velocity.Angle();
			float targetRotation =
				facingDirection > 0
					? moveAngle
					: Mathf.Wrap(moveAngle - Mathf.Pi, -Mathf.Pi, Mathf.Pi);
			float wiggle = Mathf.Sin(swimTime * SwimFrequency) * SwimAmplitude;

			fishSprite.Rotation = Mathf.LerpAngle(
				fishSprite.Rotation,
				targetRotation + wiggle,
				dt * RotationSpeed
			);
		}
	}

	private void UpdateSwimFrame()
	{
		Texture2D[] frames = facingDirection > 0 ? rightSwimFrames : leftSwimFrames;

		if (frames == null || frames.Length == 0)
			return;

		int frameIndex = Mathf.PosMod((int)(swimTime * 6.5f), frames.Length);
		Texture2D texture = frames[frameIndex];

		if (texture != null && fishSprite.Texture != texture)
			fishSprite.Texture = texture;

		fishSprite.FlipH = false;
	}

	private void UpdateFacingDirection()
	{
		if (Velocity.X > 8f)
			facingDirection = 1;
		else if (Velocity.X < -8f)
			facingDirection = -1;
	}

	private void RandomizeDirection()
	{
		moveDir = new Vector2(
			rng.RandfRange(-1f, 1f),
			rng.RandfRange(-0.34f, 0.34f)
		).Normalized();
		directionTimer = rng.RandfRange(DirectionChangeTimeMin, DirectionChangeTimeMax);
		wanderPhase = rng.RandfRange(0f, Mathf.Tau);
	}
}
