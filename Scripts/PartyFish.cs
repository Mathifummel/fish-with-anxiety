using Godot;

public partial class PartyFish : CharacterBody2D
{
	public enum VisualKind
	{
		Player,
		EnemyOne,
		EnemyTwo,
		Passive,
		Drunk
	}

	public enum ControlMode
	{
		Wasd,
		Arrows,
		Ai,
		None
	}

	[Export] public float BaseSpeed = 230f;
	[Export] public float CollisionRadius = 32f;
	[Export] public int GamepadDevice = -1;

	public ControlMode Controls = ControlMode.None;
	public VisualKind Visual = VisualKind.Player;
	public Vector2 AiDirection = Vector2.Zero;
	public bool AlwaysPerfectBoost = false;
	public bool UsesStressBoost = false;
	public bool IsEliminated = false;
	public float CurrentStress = 0f;
	public bool IsBoosting => AlwaysPerfectBoost || boostTimer > 0f;
	public float BoostMeterValue
	{
		get
		{
			if (AlwaysPerfectBoost || boostTimer > 0f)
				return 100f;

			if (boostCooldown <= 0f || maxBoostCooldown <= 0f)
				return 100f;

			return (1f - Mathf.Clamp(boostCooldown / maxBoostCooldown, 0f, 1f)) * 100f;
		}
	}
	public Rect2 Bounds = new Rect2(new Vector2(-1600f, -1000f), new Vector2(3200f, 2000f));
	public bool UseBounds = true;

	private Sprite2D sprite;
	private Texture2D[] leftFrames;
	private Texture2D[] rightFrames;
	private Texture2D[] drunkFrames;
	private Vector2 currentVelocity = Vector2.Zero;
	private float swimTime = 0f;
	private float boostTimer = 0f;
	private float boostCooldown = 0f;
	private float boostDuration = 0f;
	private float boostElapsed = 0f;
	private float maxBoostCooldown = 1.15f;
	private float currentBoostMultiplier = 1f;
	private float stunTimer = 0f;
	private int facingDirection = 1;
	private const float GamepadMoveDeadzone = 0.22f;

	public override void _Ready()
	{
		EnsureNodes();
		LoadFrames();
		UpdateFrame();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (IsEliminated)
		{
			Velocity = Vector2.Zero;
			return;
		}

		if (boostCooldown > 0f)
			boostCooldown -= dt;

		if (boostTimer > 0f)
			boostTimer -= dt;

		if (stunTimer > 0f)
			stunTimer -= dt;

		if (!AlwaysPerfectBoost && boostCooldown <= 0f && IsBoostPressed())
		{
			StartBoost();
		}

		Vector2 direction = GetMoveDirection();
		float speed = BaseSpeed;

		if (AlwaysPerfectBoost)
		{
			speed *= 3.15f;
		}
		else if (boostTimer > 0f)
		{
			boostElapsed += dt;
			float progress = boostDuration > 0f
				? Mathf.Clamp(boostElapsed / boostDuration, 0f, 1f)
				: 1f;
			float ease = Mathf.Clamp((progress - 0.58f) / 0.42f, 0f, 1f);
			ease = ease * ease * (3f - 2f * ease);
			speed *= Mathf.Lerp(currentBoostMultiplier, 1f, ease);
		}

		if (stunTimer > 0f)
			speed *= 0.28f;

		Vector2 targetVelocity = direction * speed;
		currentVelocity = currentVelocity.Lerp(targetVelocity, Mathf.Clamp(7.2f * dt, 0f, 1f));
		Velocity = currentVelocity;
		MoveAndSlide();
		ClampToBounds();
		UpdateVisual(dt);
	}

	public void Configure(VisualKind visual, ControlMode controls)
	{
		Visual = visual;
		Controls = controls;
		LoadFrames();
		UpdateFrame();
	}

	public void ApplyStun(float duration)
	{
		stunTimer = Mathf.Max(stunTimer, duration);
	}

	public void Kill()
	{
		SetEliminated(true);
	}

	public void SetEliminated(bool eliminated)
	{
		IsEliminated = eliminated;
		Visible = !eliminated;
		SetPhysicsProcess(!eliminated);
		Velocity = Vector2.Zero;
		currentVelocity = Vector2.Zero;
	}

	public void Respawn(Vector2 position)
	{
		GlobalPosition = position;
		SetEliminated(false);
	}

	private void EnsureNodes()
	{
		if (sprite != null)
			return;

		CollisionLayer = 0;
		CollisionMask = 0;

		CollisionShape2D collision = new CollisionShape2D();
		CircleShape2D circle = new CircleShape2D();
		circle.Radius = CollisionRadius;
		collision.Shape = circle;
		AddChild(collision);

		sprite = new Sprite2D();
		AddChild(sprite);
	}

	private void LoadFrames()
	{
		if (sprite == null)
			return;

		drunkFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Bessofenerfisch1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Besoffenerfisch2.png")
		};

		switch (Visual)
		{
			case VisualKind.EnemyOne:
				leftFrames = rightFrames = new Texture2D[]
				{
					ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe1.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe2.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe3.png")
				};
				sprite.Scale = new Vector2(1.22f, 1.22f);
				CollisionRadius = 30f;
				break;

			case VisualKind.EnemyTwo:
				leftFrames = rightFrames = new Texture2D[]
				{
					ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame1.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame2.png")
				};
				sprite.Scale = new Vector2(1.18f, 1.18f);
				CollisionRadius = 30f;
				break;

			case VisualKind.Passive:
				leftFrames = new Texture2D[]
				{
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2.png")
				};
				rightFrames = new Texture2D[]
				{
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1 1.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2 1.png")
				};
				sprite.Scale = new Vector2(0.72f, 0.72f);
				CollisionRadius = 22f;
				break;

			default:
				leftFrames = new Texture2D[]
				{
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2.png")
				};
				rightFrames = new Texture2D[]
				{
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1 1.png"),
					ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2 1.png")
				};
				sprite.Scale = new Vector2(1.12f, 1.12f);
				CollisionRadius = 32f;
				break;
		}
	}

	private Vector2 GetMoveDirection()
	{
		Vector2 direction = Controls switch
		{
			ControlMode.Wasd => GetKeyDirection(Key.W, Key.S, Key.A, Key.D),
			ControlMode.Arrows => GetKeyDirection(Key.Up, Key.Down, Key.Left, Key.Right),
			ControlMode.Ai => AiDirection.Normalized(),
			_ => Vector2.Zero
		};

		if (Controls == ControlMode.Wasd || Controls == ControlMode.Arrows)
		{
			Vector2 gamepadDirection = GetGamepadDirection();

			if (gamepadDirection != Vector2.Zero)
				direction = gamepadDirection;
		}

		return direction;
	}

	private bool IsBoostPressed()
	{
		if ((Controls == ControlMode.Wasd || Controls == ControlMode.Arrows) &&
			Input.IsActionJustPressed("ui_accept"))
		{
			return true;
		}

		return Controls switch
		{
			ControlMode.Wasd => Input.IsKeyPressed(Key.Space),
			ControlMode.Arrows => Input.IsKeyPressed(Key.Enter),
			_ => false
		};
	}

	private Vector2 GetGamepadDirection()
	{
		if (GamepadDevice == -2)
			return Vector2.Zero;

		foreach (int device in Input.GetConnectedJoypads())
		{
			if (GamepadDevice >= 0 && device != GamepadDevice)
				continue;

			Vector2 axis = new Vector2(
				Input.GetJoyAxis(device, JoyAxis.LeftX),
				Input.GetJoyAxis(device, JoyAxis.LeftY)
			);

			if (axis.Length() >= GamepadMoveDeadzone)
				return axis.LimitLength(1f);
		}

		return Vector2.Zero;
	}

	private void StartBoost()
	{
		if (!UsesStressBoost)
		{
			boostTimer = 0.62f;
			boostDuration = 0.62f;
			boostElapsed = 0f;
			boostCooldown = 1.15f;
			maxBoostCooldown = boostCooldown;
			currentBoostMultiplier = 2.15f;
			return;
		}

		if (CurrentStress >= 42f && CurrentStress <= 58f)
		{
			boostTimer = 0.9f;
			boostDuration = 0.9f;
			currentBoostMultiplier = 3.4f;
		}
		else if (CurrentStress >= 30f && CurrentStress <= 75f)
		{
			boostTimer = 0.62f;
			boostDuration = 0.62f;
			currentBoostMultiplier = 2.0f;
		}
		else if (CurrentStress > 75f)
		{
			boostTimer = 0.38f;
			boostDuration = 0.38f;
			currentBoostMultiplier = 1.25f;
		}
		else
		{
			return;
		}

		boostElapsed = 0f;
		boostCooldown = 1.5f;
		maxBoostCooldown = boostCooldown;
	}

	private Vector2 GetKeyDirection(Key up, Key down, Key left, Key right)
	{
		Vector2 direction = Vector2.Zero;

		if (Input.IsKeyPressed(right))
			direction += Vector2.Right;

		if (Input.IsKeyPressed(left))
			direction += Vector2.Left;

		if (Input.IsKeyPressed(down))
			direction += Vector2.Down;

		if (Input.IsKeyPressed(up))
			direction += Vector2.Up;

		return direction.Normalized();
	}

	private void ClampToBounds()
	{
		if (!UseBounds)
			return;

		Vector2 position = GlobalPosition;
		position.X = Mathf.Clamp(position.X, Bounds.Position.X, Bounds.End.X);
		position.Y = Mathf.Clamp(position.Y, Bounds.Position.Y, Bounds.End.Y);
		GlobalPosition = position;
	}

	private void UpdateVisual(float dt)
	{
		if (sprite == null)
			return;

		if (Velocity.Length() <= 0.1f)
		{
			sprite.Position = Vector2.Zero;
			return;
		}

		swimTime += dt;

		if (Velocity.X > 8f)
			facingDirection = 1;
		else if (Velocity.X < -8f)
			facingDirection = -1;

		UpdateFrame();

		float moveAngle = Velocity.Angle();
		float targetRotation = facingDirection > 0
			? moveAngle
			: Mathf.Wrap(moveAngle - Mathf.Pi, -Mathf.Pi, Mathf.Pi);
		float wiggle = Mathf.Sin(swimTime * 7.2f) * (AlwaysPerfectBoost ? 0.34f : 0.18f);
		float shake = AlwaysPerfectBoost
			? Mathf.Sin(swimTime * 22f + GetInstanceId()) * 0.12f
			: 0f;

		sprite.Rotation = Mathf.LerpAngle(sprite.Rotation, targetRotation + wiggle + shake, Mathf.Clamp(dt * 6f, 0f, 1f));
		sprite.Position = AlwaysPerfectBoost
			? new Vector2(0f, Mathf.Sin(swimTime * 28f + GetInstanceId()) * 3.5f)
			: Vector2.Zero;
	}

	private void UpdateFrame()
	{
		if (sprite == null)
			return;

		Texture2D[] frames = AlwaysPerfectBoost && drunkFrames != null && drunkFrames.Length > 0
			? drunkFrames
			: facingDirection > 0
				? rightFrames
				: leftFrames;

		if (frames == null || frames.Length == 0)
			return;

		int frameIndex = Mathf.PosMod((int)(swimTime * (AlwaysPerfectBoost ? 13f : 7.5f)), frames.Length);
		Texture2D texture = frames[frameIndex];

		if (texture != null)
			sprite.Texture = texture;

		bool usesLeftOnlyFrames = Visual == VisualKind.EnemyOne || Visual == VisualKind.EnemyTwo || AlwaysPerfectBoost;
		sprite.FlipH = usesLeftOnlyFrames && facingDirection > 0;
	}
}
