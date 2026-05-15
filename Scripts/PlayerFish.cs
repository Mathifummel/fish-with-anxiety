using Godot;

public partial class PlayerFish : CharacterBody2D
{
	// =========================================
	// MOVEMENT
	// =========================================

	[Export] public float Speed = 200f;
	[Export] public float RotationSpeed = 5f;

	[Export] public float SwimAmplitude = 0.2f;
	[Export] public float SwimFrequency = 5f;

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

	// =========================================
	// PLAYER
	// =========================================

	[Export] public float CollisionRadius = 40f;

	private float boostCooldownTimer = 0f;
	private float boostTimer = 0f;

	private bool boostActive = false;

	private float currentBoostMultiplier = 1f;
	private float currentStressDrain = 0f;

	public bool IsBoosting = false;
	public float CurrentStress = 0f;

	private Sprite2D fishSprite;
	private float swimTime = 0f;

	// smoother movement
	private Vector2 currentVelocity = Vector2.Zero;

	public override void _Ready()
	{
		fishSprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// =========================================
		// INPUT
		// =========================================

		Vector2 dir = Vector2.Zero;

		if (Input.IsActionPressed("ui_right"))
			dir += Vector2.Right;

		if (Input.IsActionPressed("ui_left"))
			dir += Vector2.Left;

		if (Input.IsActionPressed("ui_down"))
			dir += Vector2.Down;

		if (Input.IsActionPressed("ui_up"))
			dir += Vector2.Up;

		dir = dir.Normalized();

		// =========================================
		// COOLDOWN
		// =========================================

		if (boostCooldownTimer > 0)
			boostCooldownTimer -= dt;

		IsBoosting = false;

		float targetSpeed = Speed;

		// =========================================
		// BOOST START
		// =========================================

		if (!boostActive &&
			boostCooldownTimer <= 0f &&
			Input.IsActionJustPressed("ui_accept"))
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
				boostTimer = MinBoostTime;
			}
		}

		// =========================================
		// BOOST ACTIVE
		// =========================================

		if (boostActive)
		{
			IsBoosting = true;

			targetSpeed =
				Speed * currentBoostMultiplier;

			boostTimer -= dt;

			// realistic ending
			if (boostTimer <= 0f)
			{
				// perfect boosts end smoothly
				if (CurrentStress < 25f)
				{
					boostActive = false;
				}

				// panic boosts collapse earlier
				if (currentBoostMultiplier ==
					PanicBoostMultiplier)
				{
					if (CurrentStress < 55f)
					{
						boostActive = false;
					}
				}
			}
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

		// =========================================
		// ROTATION + SWIM
		// =========================================

		if (Velocity.Length() > 0.1f)
		{
			swimTime += dt;

			float targetRotation =
				Velocity.Angle() + Mathf.Pi;

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

	// =========================================
	// USED BY MAIN.CS
	// =========================================

	public float GetStressDrain()
	{
		if (!boostActive)
			return 0f;

		return currentStressDrain;
	}
}
