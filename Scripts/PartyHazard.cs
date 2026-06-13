using Godot;

public partial class PartyHazard : CharacterBody2D
{
	[Export] public float Speed = 86f;
	[Export] public float CollisionRadius = 32f;

	public Rect2 Bounds = new Rect2(new Vector2(-1600f, -1000f), new Vector2(3200f, 2000f));

	private Sprite2D sprite;
	private Texture2D[] frames;
	private Vector2 wanderDirection = Vector2.Right;
	private Vector2 currentVelocity = Vector2.Zero;
	private float pulseTime = 0f;
	private float directionTimer = 0f;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		rng.Randomize();
		EnsureNodes();
		frames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Qualle.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Qualle2.png")
		};
		RandomizeDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		pulseTime += dt;
		directionTimer -= dt;

		if (directionTimer <= 0f)
			RandomizeDirection();

		Vector2 targetVelocity = wanderDirection * Speed;
		currentVelocity = currentVelocity.Lerp(targetVelocity, Mathf.Clamp(dt * 4.2f, 0f, 1f));
		Velocity = currentVelocity;
		MoveAndSlide();
		ClampToBounds();
		UpdateVisual(dt);
	}

	private void EnsureNodes()
	{
		CollisionLayer = 0;
		CollisionMask = 0;

		sprite = new Sprite2D();
		sprite.Scale = new Vector2(0.72f, 0.72f);
		AddChild(sprite);
	}

	private void ClampToBounds()
	{
		Vector2 position = GlobalPosition;
		position.X = Mathf.Clamp(position.X, Bounds.Position.X, Bounds.End.X);
		position.Y = Mathf.Clamp(position.Y, Bounds.Position.Y, Bounds.End.Y);
		GlobalPosition = position;

		if (Mathf.IsEqualApprox(position.X, Bounds.Position.X) || Mathf.IsEqualApprox(position.X, Bounds.End.X))
			wanderDirection.X *= -1f;

		if (Mathf.IsEqualApprox(position.Y, Bounds.Position.Y) || Mathf.IsEqualApprox(position.Y, Bounds.End.Y))
			wanderDirection.Y *= -1f;
	}

	private void UpdateVisual(float dt)
	{
		if (sprite == null)
			return;

		if (frames != null && frames.Length > 0)
		{
			int frameIndex = Mathf.PosMod((int)(pulseTime * 5f), frames.Length);
			Texture2D texture = frames[frameIndex];
			if (texture != null)
				sprite.Texture = texture;
		}

		float pulse = Mathf.Sin(pulseTime * Mathf.Tau * 0.82f) * 0.08f;
		sprite.Scale = new Vector2(0.72f + pulse * 0.4f, 0.72f - pulse);

		if (Velocity.Length() <= 0.1f)
			return;

		sprite.Rotation = Mathf.LerpAngle(
			sprite.Rotation,
			Velocity.Angle() + Mathf.Pi * 0.5f + Mathf.Sin(pulseTime * 3.4f) * 0.08f,
			Mathf.Clamp(dt * 2.4f, 0f, 1f)
		);
	}

	private void RandomizeDirection()
	{
		wanderDirection = Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));
		directionTimer = rng.RandfRange(1.2f, 3.2f);
	}
}
