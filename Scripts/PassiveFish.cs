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

	private Vector2 moveDir = Vector2.Right;
	private Vector2 currentVelocity = Vector2.Zero;
	private float directionTimer = 0f;
	private float swimTime = 0f;
	private float wanderPhase = 0f;

	private Sprite2D fishSprite;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		fishSprite = GetNode<Sprite2D>("Sprite2D");
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

	private void RandomizeDirection()
	{
		moveDir = Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));
		directionTimer = rng.RandfRange(DirectionChangeTimeMin, DirectionChangeTimeMax);
		wanderPhase = rng.RandfRange(0f, Mathf.Tau);
	}
}
