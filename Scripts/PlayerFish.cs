using Godot;

public partial class PlayerFish : CharacterBody2D
{
	public enum ControlScheme
	{
		ArrowKeys,
		WASD,
		Mouse,
		Custom
	}

	public static ControlScheme CurrentControlScheme = ControlScheme.ArrowKeys;

	public const string CustomMoveUp = "custom_move_up";
	public const string CustomMoveDown = "custom_move_down";
	public const string CustomMoveLeft = "custom_move_left";
	public const string CustomMoveRight = "custom_move_right";
	public const string CustomBoost = "custom_boost";
	public const string CustomUseItem = "custom_use_item";
	public const string UseItemAction = "use_item";

	private const string ControlsSavePath = "user://control_settings.cfg";
	private const string ControlsSection = "controls";
	private const string BindingsSection = "bindings";
	private static bool controlsLoaded = false;
	private const float GamepadMoveDeadzone = 0.22f;

	// =========================================
	// MOVEMENT
	// =========================================

	[Export] public float Speed = 200f;
	[Export] public float RotationSpeed = 5f;

	[Export] public float SwimAmplitude = 0.2f;
	[Export] public float SwimFrequency = 5f;
	[Export] public float SwimFrameRate = 7.5f;
	[Export] public float DirectionFlipDeadzone = 12f;
	[Export] public bool UseSwimBounds = true;
	[Export] public float MinSwimY = OceanMapBackground.WorldPlayerMinY;
	[Export] public float MaxSwimY = OceanMapBackground.WorldPlayerMaxY;
	[Export] public float BoundarySoftZone = 92f;
	[Export] public float BoundaryPushSpeed = 250f;

	// =========================================
	// BOOST ZONES
	// =========================================

	// 🟢 PERFECT TIMING
	[Export] public float GreenBoostMin = 42f;
	[Export] public float GreenBoostMax = 58f;

	// 🟡 NORMAL TIMING
	[Export] public float NormalBoostMin = 30f;
	[Export] public float NormalBoostMax = 75f;

	// =========================================
	// BOOST VALUES
	// =========================================

	[Export] public float PerfectBoostMultiplier = 3.4f;
	[Export] public float NormalBoostMultiplier = 2.0f;
	[Export] public float PanicBoostMultiplier = 1.25f;

	[Export] public float PerfectStressDrain = 38f;
	[Export] public float NormalStressDrain = 18f;
	[Export] public float PanicStressDrain = 6f;

	// =========================================
	// BOOST SETTINGS
	// =========================================

	[Export] public float BoostCooldown = 1.5f;
	[Export] public float MinBoostTime = 0.35f;
	[Export] public float PerfectBoostDuration = 0.9f;
	[Export] public float NormalBoostDuration = 0.62f;
	[Export] public float PanicBoostDuration = 0.38f;
	[Export] public float BoostEaseOutStart = 0.58f;

	// =========================================
	// PLAYER
	// =========================================

	[Export] public float CollisionRadius = 40f;

	private float boostCooldownTimer = 0f;
	private float boostTimer = 0f;
	private float boostDuration = 0f;
	private float boostElapsed = 0f;

	private bool boostActive = false;

	private float currentBoostMultiplier = 1f;
	private float currentStressDrain = 0f;

	public bool IsBoosting = false;
	public float CurrentStress = 0f;
	public bool IsInvincible = false;
	public float SpeedMultiplier = 1f;

	private Sprite2D fishSprite;
	private float slowTimer = 0f;
	private float swimTime = 0f;
	private Texture2D[] leftSwimFrames;
	private Texture2D[] rightSwimFrames;
	private Texture2D[] drunkSwimFrames;
	private int facingDirection = 1;
	private bool selectedSkinUsesMirroredFrames = false;

	// smoother movement
	private Vector2 currentVelocity = Vector2.Zero;

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
		drunkSwimFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Bessofenerfisch1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Besoffenerfisch2.png")
		};
		ApplySelectedSkinFrames();
		GameUi.EnsureInputDefaults();
		LoadControlSettings();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// =========================================
		// INPUT
		// =========================================

		Vector2 dir = Vector2.Zero;

		switch (CurrentControlScheme)
		{
			case ControlScheme.WASD:
				dir = GetKeyDirection(Key.W, Key.S, Key.A, Key.D);
				break;

			case ControlScheme.Mouse:
				Vector2 toMouse = GetGlobalMousePosition() - GlobalPosition;
				dir = toMouse.Length() > 12f ? toMouse.Normalized() : Vector2.Zero;
				break;

			case ControlScheme.Custom:
				dir = GetActionDirection(
					CustomMoveUp,
					CustomMoveDown,
					CustomMoveLeft,
					CustomMoveRight
				);
				break;

			default:
				dir = GetKeyDirection(Key.Up, Key.Down, Key.Left, Key.Right);
				break;
		}

		Vector2 gamepadDir = GetLeftStickDirection();

		if (gamepadDir != Vector2.Zero)
			dir = gamepadDir;

		// =========================================
		// COOLDOWN
		// =========================================

		if (boostCooldownTimer > 0)
			boostCooldownTimer -= dt;

		IsBoosting = false;

		if (slowTimer > 0f)
		{
			slowTimer -= dt;

			if (slowTimer <= 0f)
			{
				slowTimer = 0f;
				SpeedMultiplier = 1f;
			}
		}

		float targetSpeed = Speed * SpeedMultiplier;

		// =========================================
		// BOOST START
		// =========================================

		if (!boostActive &&
			boostCooldownTimer <= 0f &&
			IsBoostJustPressed())
		{
			// 🟢 PERFECT WINDOW
			if (CurrentStress >= GreenBoostMin &&
				CurrentStress <= GreenBoostMax)
			{
				boostActive = true;

				currentBoostMultiplier =
					PerfectBoostMultiplier;

				currentStressDrain =
					PerfectStressDrain;

				var sm = GetNode<ScoreManager>(
					"/root/ScoreManager"
				);

				sm.RegisterGreenBoost();

				GD.Print("PERFECT BOOST");
			}

			// 🟡 NORMAL BOOST
			else if (CurrentStress >= NormalBoostMin &&
					 CurrentStress <= NormalBoostMax)
			{
				boostActive = true;

				currentBoostMultiplier =
					NormalBoostMultiplier;

				currentStressDrain =
					NormalStressDrain;

				GD.Print("NORMAL BOOST");
			}

			// 🔴 PANIC BOOST
			else if (CurrentStress > NormalBoostMax)
			{
				boostActive = true;

				currentBoostMultiplier =
					PanicBoostMultiplier;

				currentStressDrain =
					PanicStressDrain;

				var sm = GetNode<ScoreManager>(
					"/root/ScoreManager"
				);

				sm.RegisterOverBoost();

				GD.Print("PANIC BOOST");
			}

			// activate timers
			if (boostActive)
			{
				boostCooldownTimer = BoostCooldown;
				StartBoostTimer();
				GameAudio.PlayBoost(this);
			}
		}

		// =========================================
		// BOOST ACTIVE
		// =========================================

		if (boostActive)
		{
			IsBoosting = true;

			boostElapsed += dt;
			boostTimer -= dt;

			float boostProgress =
				boostDuration > 0f
					? Mathf.Clamp(boostElapsed / boostDuration, 0f, 1f)
					: 1f;

			float easeProgress = Mathf.Clamp(
				(boostProgress - BoostEaseOutStart) /
				Mathf.Max(1f - BoostEaseOutStart, 0.01f),
				0f,
				1f
			);
			easeProgress = easeProgress * easeProgress * (3f - 2f * easeProgress);

			float activeBoostMultiplier =
				Mathf.Lerp(currentBoostMultiplier, 1f, easeProgress);

			targetSpeed =
				Speed * SpeedMultiplier * activeBoostMultiplier;

			if (boostTimer <= 0f)
				boostActive = false;
		}

		// =========================================
		// MOVEMENT SMOOTHING
		// =========================================

		Vector2 targetVelocity = dir * targetSpeed;

		currentVelocity =
			currentVelocity.Lerp(
				targetVelocity,
				7f * dt
			);

		Velocity = currentVelocity;

		MoveAndSlide();
		ApplyVerticalSwimBounds(dt);
		UpdateInvincibleVisual();

		// =========================================
		// ROTATION + SWIM
		// =========================================

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

			float wiggle =
				Mathf.Sin(
					swimTime * SwimFrequency
				) * SwimAmplitude;

			fishSprite.Rotation =
				Mathf.LerpAngle(
					fishSprite.Rotation,
					targetRotation + wiggle,
					dt * RotationSpeed
				);
		}
	}

	private void UpdateFacingDirection()
	{
		if (Velocity.X > DirectionFlipDeadzone)
			facingDirection = 1;
		else if (Velocity.X < -DirectionFlipDeadzone)
			facingDirection = -1;
	}

	private void ApplyVerticalSwimBounds(float dt)
	{
		if (!UseSwimBounds)
			return;

		Vector2 position = GlobalPosition;
		bool adjusted = false;

		if (position.Y < MinSwimY)
		{
			position.Y = MinSwimY;
			currentVelocity.Y = Mathf.Max(currentVelocity.Y, BoundaryPushSpeed);
			adjusted = true;
		}
		else if (position.Y < MinSwimY + BoundarySoftZone && currentVelocity.Y < BoundaryPushSpeed * 0.45f)
		{
			float pressure = 1f - ((position.Y - MinSwimY) / Mathf.Max(BoundarySoftZone, 1f));
			currentVelocity.Y = Mathf.Lerp(currentVelocity.Y, BoundaryPushSpeed * 0.58f, Mathf.Clamp(dt * 6.5f * pressure, 0f, 1f));
			adjusted = true;
		}

		if (position.Y > MaxSwimY)
		{
			position.Y = MaxSwimY;
			currentVelocity.Y = Mathf.Min(currentVelocity.Y, -BoundaryPushSpeed * 0.75f);
			adjusted = true;
		}
		else if (position.Y > MaxSwimY - BoundarySoftZone && currentVelocity.Y > -BoundaryPushSpeed * 0.35f)
		{
			float pressure = 1f - ((MaxSwimY - position.Y) / Mathf.Max(BoundarySoftZone, 1f));
			currentVelocity.Y = Mathf.Lerp(currentVelocity.Y, -BoundaryPushSpeed * 0.45f, Mathf.Clamp(dt * 5f * pressure, 0f, 1f));
			adjusted = true;
		}

		if (!adjusted)
			return;

		GlobalPosition = position;
		Velocity = currentVelocity;
	}

	private void UpdateSwimFrame()
	{
		bool useDrunkFrames = IsInvincible && drunkSwimFrames != null && drunkSwimFrames.Length > 0;
		Texture2D[] frames = useDrunkFrames
			? drunkSwimFrames
			: facingDirection > 0
				? rightSwimFrames
				: leftSwimFrames;

		if (frames == null || frames.Length == 0)
			return;

		int frameIndex = Mathf.PosMod((int)(swimTime * SwimFrameRate), frames.Length);
		Texture2D texture = frames[frameIndex];

		if (texture != null && fishSprite.Texture != texture)
			fishSprite.Texture = texture;

		fishSprite.FlipH = useDrunkFrames
			? facingDirection > 0
			: selectedSkinUsesMirroredFrames && facingDirection > 0;
	}

	private void ApplySelectedSkinFrames()
	{
		ScoreManager scoreManager = GetNodeOrNull<ScoreManager>("/root/ScoreManager");
		if (scoreManager == null ||
			scoreManager.SelectedSkinId == ShopCatalog.DefaultSkinId)
		{
			selectedSkinUsesMirroredFrames = false;
			return;
		}

		SkinDefinition skin = ShopCatalog.GetSkin(scoreManager.SelectedSkinId);
		if (skin == null)
			return;

		Texture2D frame1 = ResourceLoader.Load<Texture2D>(skin.Frame1Path);
		Texture2D frame2 = ResourceLoader.Load<Texture2D>(skin.Frame2Path);
		if (frame1 == null || frame2 == null)
			return;

		leftSwimFrames = new Texture2D[] { frame1, frame2 };
		rightSwimFrames = leftSwimFrames;
		selectedSkinUsesMirroredFrames = true;
		UpdateSwimFrame();
	}

	// =========================================
	// USED BY MAIN.CS
	// =========================================

	public float GetStressDrain()
	{
		if (!boostActive)
			return 0f;

		return currentStressDrain;
	}

	private void StartBoostTimer()
	{
		if (Mathf.IsEqualApprox(currentBoostMultiplier, PerfectBoostMultiplier))
			boostDuration = PerfectBoostDuration;
		else if (Mathf.IsEqualApprox(currentBoostMultiplier, NormalBoostMultiplier))
			boostDuration = NormalBoostDuration;
		else
			boostDuration = PanicBoostDuration;

		boostDuration = Mathf.Max(boostDuration, MinBoostTime);
		boostTimer = boostDuration;
		boostElapsed = 0f;
	}

	public void ApplySlow(float multiplier, float duration)
	{
		SpeedMultiplier = multiplier;
		slowTimer = duration;
	}

	public void SetInvincible(bool active)
	{
		IsInvincible = active;

		if (!active && fishSprite != null)
		{
			fishSprite.Modulate = Colors.White;
			fishSprite.FlipH = false;
		}

		if (fishSprite != null)
			UpdateSwimFrame();
	}

	private void UpdateInvincibleVisual()
	{
		if (!IsInvincible || fishSprite == null)
			return;

		float pulse = 0.82f + Mathf.Sin(Time.GetTicksMsec() * 0.008f) * 0.12f;
		fishSprite.Modulate = new Color(0.75f, 1f, 0.95f, pulse);
	}

	private Vector2 GetKeyDirection(Key up, Key down, Key left, Key right)
	{
		Vector2 dir = Vector2.Zero;

		if (Input.IsKeyPressed(right))
			dir += Vector2.Right;

		if (Input.IsKeyPressed(left))
			dir += Vector2.Left;

		if (Input.IsKeyPressed(down))
			dir += Vector2.Down;

		if (Input.IsKeyPressed(up))
			dir += Vector2.Up;

		return dir.Normalized();
	}

	private Vector2 GetActionDirection(string up, string down, string left, string right)
	{
		Vector2 dir = Vector2.Zero;

		if (Input.IsActionPressed(right))
			dir += Vector2.Right;

		if (Input.IsActionPressed(left))
			dir += Vector2.Left;

		if (Input.IsActionPressed(down))
			dir += Vector2.Down;

		if (Input.IsActionPressed(up))
			dir += Vector2.Up;

		return dir.Normalized();
	}

	private Vector2 GetLeftStickDirection()
	{
		foreach (int device in Input.GetConnectedJoypads())
		{
			Vector2 axis = new Vector2(
				Input.GetJoyAxis(device, JoyAxis.LeftX),
				Input.GetJoyAxis(device, JoyAxis.LeftY)
			);

			if (axis.Length() >= GamepadMoveDeadzone)
				return axis.LimitLength(1f);
		}

		return Vector2.Zero;
	}

	private bool IsBoostJustPressed()
	{
		GameUi.EnsureInputDefaults();

		if (Input.IsActionJustPressed("ui_accept"))
			return true;

		if (CurrentControlScheme == ControlScheme.Custom)
			return Input.IsActionJustPressed(CustomBoost);

		return false;
	}

	public static bool IsUseItemJustPressed()
	{
		GameUi.EnsureInputDefaults();
		EnsureCustomInputDefaults();

		if (Input.IsActionJustPressed(UseItemAction))
			return true;

		if (CurrentControlScheme == ControlScheme.Custom)
			return Input.IsActionJustPressed(CustomUseItem);

		return false;
	}

	public static void EnsureCustomInputDefaults()
	{
		EnsureAction(CustomMoveUp, Key.W);
		EnsureAction(CustomMoveDown, Key.S);
		EnsureAction(CustomMoveLeft, Key.A);
		EnsureAction(CustomMoveRight, Key.D);
		EnsureAction(CustomBoost, Key.Space);
		EnsureAction(CustomUseItem, Key.P);
	}

	public static void SetCustomInput(string action, InputEvent inputEvent)
	{
		EnsureAction(action, Key.None);
		InputMap.ActionEraseEvents(action);
		InputMap.ActionAddEvent(action, inputEvent);
		SaveControlSettings();
	}

	public static void SetControlScheme(ControlScheme scheme)
	{
		LoadControlSettings();
		CurrentControlScheme = scheme;
		SaveControlSettings();
	}

	public static string GetCustomInputLabel(string action)
	{
		EnsureAction(action, Key.None);

		var events = InputMap.ActionGetEvents(action);
		if (events.Count == 0)
			return "-";

		return events[0].AsText();
	}

	private static void EnsureAction(string action, Key defaultKey)
	{
		if (!InputMap.HasAction(action))
			InputMap.AddAction(action);

		if (defaultKey == Key.None || InputMap.ActionGetEvents(action).Count > 0)
			return;

		InputEventKey keyEvent = new InputEventKey();
		keyEvent.PhysicalKeycode = defaultKey;
		InputMap.ActionAddEvent(action, keyEvent);
	}

	public static void LoadControlSettings()
	{
		EnsureCustomInputDefaults();

		if (controlsLoaded)
			return;

		controlsLoaded = true;

		ConfigFile config = new ConfigFile();
		if (config.Load(ControlsSavePath) != Error.Ok)
			return;

		CurrentControlScheme = (ControlScheme)config
			.GetValue(ControlsSection, "scheme", (int)ControlScheme.ArrowKeys)
			.AsInt32();

		LoadAction(config, CustomMoveUp);
		LoadAction(config, CustomMoveDown);
		LoadAction(config, CustomMoveLeft);
		LoadAction(config, CustomMoveRight);
		LoadAction(config, CustomBoost);
		LoadAction(config, CustomUseItem);
	}

	public static void SaveControlSettings()
	{
		EnsureCustomInputDefaults();

		ConfigFile config = new ConfigFile();
		config.SetValue(ControlsSection, "scheme", (int)CurrentControlScheme);

		SaveAction(config, CustomMoveUp);
		SaveAction(config, CustomMoveDown);
		SaveAction(config, CustomMoveLeft);
		SaveAction(config, CustomMoveRight);
		SaveAction(config, CustomBoost);
		SaveAction(config, CustomUseItem);

		config.Save(ControlsSavePath);
	}

	private static void SaveAction(ConfigFile config, string action)
	{
		var events = InputMap.ActionGetEvents(action);

		if (events.Count == 0)
			return;

		InputEvent inputEvent = events[0];

		if (inputEvent is InputEventKey keyEvent)
		{
			config.SetValue(BindingsSection, $"{action}_type", "key");
			config.SetValue(BindingsSection, $"{action}_value", (long)keyEvent.PhysicalKeycode);
		}
		else if (inputEvent is InputEventMouseButton mouseEvent)
		{
			config.SetValue(BindingsSection, $"{action}_type", "mouse");
			config.SetValue(BindingsSection, $"{action}_value", (int)mouseEvent.ButtonIndex);
		}
	}

	private static void LoadAction(ConfigFile config, string action)
	{
		if (!config.HasSectionKey(BindingsSection, $"{action}_type") ||
			!config.HasSectionKey(BindingsSection, $"{action}_value"))
		{
			return;
		}

		string type = config.GetValue(BindingsSection, $"{action}_type").AsString();
		long value = config.GetValue(BindingsSection, $"{action}_value").AsInt64();

		EnsureAction(action, Key.None);
		InputMap.ActionEraseEvents(action);

		if (type == "key")
		{
			InputEventKey keyEvent = new InputEventKey();
			keyEvent.PhysicalKeycode = (Key)value;
			InputMap.ActionAddEvent(action, keyEvent);
		}
		else if (type == "mouse")
		{
			InputEventMouseButton mouseEvent = new InputEventMouseButton();
			mouseEvent.ButtonIndex = (MouseButton)value;
			InputMap.ActionAddEvent(action, mouseEvent);
		}
	}
}
