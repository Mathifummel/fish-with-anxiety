using Godot;

public partial class NPCFish : CharacterBody2D
{
	public enum MovementMode
	{
		Chase,
		Crossing
	}

	public enum EnemySkin
	{
		Gegnerfisch,
		Gegnerfisch2
	}

	[Export] public float Speed = 120f;
	[Export] public float RotationSpeed = 3f;

	[Export] public float SwimAmplitude = 0.15f;
	[Export] public float SwimFrequency = 4f;
	[Export] public float SwimFrameRate = 8f;
	[Export] public float ShakeAmplitude = 0.08f;
	[Export] public float BoostShakeAmplitude = 0.17f;
	[Export] public float ShakeFrequency = 17f;
	[Export] public float PositionShakePixels = 2.4f;

	// Boost
	[Export] public float BoostMultiplier = 1.85f;
	[Export] public float BoostDuration = 0.82f;
	[Export] public float BoostChance = 0.3f;
	[Export] public float BoostCooldown = 1.65f;

	// Separation
	[Export] public float SeparationRadius = 80f;
	[Export] public float SeparationStrength = 200f;

	// Offset movement
	[Export] public float ApproachOffsetStrength = 100f;
	[Export] public float CollisionRadius = 40f;
	[Export] public float SandBoundaryExtraPadding = 4f;
	[Export] public float WaterSurfaceExtraPadding = 12f;
	[Export] public float SandBoundaryPushSpeed = 210f;
	[Export] public EnemySkin Skin = EnemySkin.Gegnerfisch;
	[Export] public float CrossingDespawnDistance = 2300f;
	[Export] public float CrossingMaxLifetime = 18f;
	[Export] public float CrossingObstacleTurnStrength = 0.72f;
	[Export] public float CrossingBoundaryTurnStrength = 0.38f;

	public Node2D Player; // assigned from Main
	public MovementMode Mode = MovementMode.Chase;
	public Vector2 CrossingDirection = Vector2.Right;

	private Sprite2D fishSprite;
	private float swimTime = 0f;
	private int facingDirection = -1;
	private Texture2D[] swimFrames;
	private bool skinStatsApplied = false;

	private float boostTimer = 0f;
	private float boostCooldownTimer = 0f;
	private float crossingAge = 0f;
	private float crossingAvoidSign = 0f;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		fishSprite = GetNode<Sprite2D>("Sprite2D");
		LoadSkinFrames();
		UpdateSwimFrame();
		rng.Randomize();

		AddToGroup("fish");
	}

	public void ConfigureSkin(EnemySkin skin)
	{
		Skin = skin;
		ApplySkinStats();
		LoadSkinFrames();
		UpdateSwimFrame();
	}

	private void ApplySkinStats()
	{
		if (skinStatsApplied)
			return;

		skinStatsApplied = true;

		if (Skin == EnemySkin.Gegnerfisch2)
		{
			Speed *= 1.08f;
			BoostChance *= 0.72f;
			BoostMultiplier *= 0.92f;
			ApproachOffsetStrength *= 1.35f;
			SeparationRadius *= 0.92f;
			SwimFrameRate *= 0.85f;
			return;
		}

		BoostChance *= 1.05f;
		BoostMultiplier *= 1.04f;
		PositionShakePixels *= 1.08f;
	}

	private void LoadSkinFrames()
	{
		swimFrames = Skin == EnemySkin.Gegnerfisch2
			? new Texture2D[]
			{
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame1.png"),
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame2.png")
			}
			: new Texture2D[]
			{
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe1.png"),
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe2.png"),
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe3.png")
			};
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

		if (boostCooldownTimer > 0f)
			boostCooldownTimer -= dt;

		// Random boost, checked per second so it stays fair at every frame rate.
		if (rng.Randf() < BoostChance * dt &&
			boostTimer <= 0f &&
			boostCooldownTimer <= 0f)
		{
			boostTimer = BoostDuration;
			boostCooldownTimer = BoostCooldown;
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
		ApplySandBoundary();

		UpdateRotation(dt);
	}

	private void UpdateCrossingMovement(float dt)
	{
		crossingAge += dt;

		if (ShouldDespawnCrossingFish())
		{
			QueueFree();
			return;
		}

		Vector2 direction = GetCrossingDirectionWithBoundaryAvoidance(dt);
		Velocity = direction * Speed;
		MoveAndSlide();
		ApplySandBoundary();
		SteerCrossingAroundObstacles(direction);
		UpdateRotation(dt);
	}

	private bool ShouldDespawnCrossingFish()
	{
		if (Player == null || !IsInstanceValid(Player))
			return crossingAge >= CrossingMaxLifetime;

		Vector2 fromPlayer = GlobalPosition - Player.GlobalPosition;
		Vector2 forward = CrossingDirection.LengthSquared() > 0.01f
			? CrossingDirection.Normalized()
			: Vector2.Right;
		bool passedPlayer = fromPlayer.Dot(forward) > CrossingDespawnDistance * 0.62f;
		bool farAway = fromPlayer.LengthSquared() > CrossingDespawnDistance * CrossingDespawnDistance;

		return (passedPlayer && farAway) || crossingAge >= CrossingMaxLifetime;
	}

	private Vector2 GetCrossingDirectionWithBoundaryAvoidance(float dt)
	{
		Vector2 direction = CrossingDirection.LengthSquared() > 0.01f
			? CrossingDirection.Normalized()
			: Vector2.Right;

		float minY = OceanMapBackground.WorldPlayerMinY + WaterSurfaceExtraPadding + 32f;
		float maxY = SandBoundary.GetMaxSwimY(this, GlobalPosition.X, CollisionRadius + SandBoundaryExtraPadding + 32f);
		float boundaryPush = 0f;

		if (GlobalPosition.Y < minY + 120f)
			boundaryPush = 1f - ((GlobalPosition.Y - minY) / 120f);
		else if (GlobalPosition.Y > maxY - 120f)
			boundaryPush = -1f + ((maxY - GlobalPosition.Y) / 120f);

		if (Mathf.Abs(boundaryPush) > 0.01f)
		{
			Vector2 target = (direction + Vector2.Down * Mathf.Clamp(boundaryPush, -1f, 1f)).Normalized();
			direction = direction.Lerp(target, Mathf.Clamp(dt * 5f * CrossingBoundaryTurnStrength, 0f, 1f)).Normalized();
			CrossingDirection = direction;
		}

		return direction;
	}

	private void SteerCrossingAroundObstacles(Vector2 previousDirection)
	{
		int collisionCount = GetSlideCollisionCount();

		if (collisionCount <= 0)
			return;

		Vector2 direction = previousDirection.LengthSquared() > 0.01f
			? previousDirection.Normalized()
			: CrossingDirection.Normalized();
		Vector2 steer = Vector2.Zero;

		for (int i = 0; i < collisionCount; i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			if (collision == null)
				continue;

			Vector2 normal = collision.GetNormal();
			Vector2 tangent = new Vector2(-normal.Y, normal.X);

			if (tangent.LengthSquared() < 0.01f)
				continue;

			if (crossingAvoidSign == 0f)
				crossingAvoidSign = rng.Randf() < 0.5f ? -1f : 1f;

			Vector2 preferredTangent = tangent * crossingAvoidSign;

			if (preferredTangent.Dot(direction) < -0.12f)
				preferredTangent = -preferredTangent;

			steer += preferredTangent.Normalized() * 1.25f + normal * 0.28f;
		}

		if (steer.LengthSquared() <= 0.01f)
			return;

		Vector2 targetDirection = (direction * 0.72f + steer.Normalized()).Normalized();
		CrossingDirection = direction.Lerp(
			targetDirection,
			Mathf.Clamp(CrossingObstacleTurnStrength, 0.05f, 1f)
		).Normalized();
	}

	private void UpdateFleeMovement(float dt, Main main)
	{
		Vector2 awayFromPlayer = Position - Player.Position;

		if (awayFromPlayer.LengthSquared() < 1f)
			awayFromPlayer = Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau));

		float horizontalSign = awayFromPlayer.X < 0f ? -1f : 1f;
		Vector2 fleeDir = new Vector2(horizontalSign, 0f);

		float minY = OceanMapBackground.WorldPlayerMinY + WaterSurfaceExtraPadding;
		float maxY = SandBoundary.GetMaxSwimY(this, Position.X, CollisionRadius + SandBoundaryExtraPadding);
		float centerY = (minY + maxY) * 0.5f;
		float verticalPressure = 0f;

		if (Position.Y < minY + 90f)
			verticalPressure = 0.42f;
		else if (Position.Y > maxY - 90f)
			verticalPressure = -0.42f;
		else
			verticalPressure = Mathf.Clamp((centerY - Position.Y) / 620f, -0.18f, 0.18f);

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

		fleeDir = (fleeDir + Vector2.Up * -verticalPressure + separation * 0.0025f).Normalized();
		float fleeSpeed = Speed * main.NpcFleeSpeedMultiplier;

		Velocity = fleeDir * fleeSpeed;
		MoveAndSlide();
		ApplySandBoundary();
		UpdateRotation(dt);
	}

	private void ApplySandBoundary()
	{
		Velocity = SandBoundary.ClampCharacterInsideWater(
			this,
			Velocity,
			CollisionRadius + SandBoundaryExtraPadding,
			WaterSurfaceExtraPadding,
			SandBoundaryPushSpeed
		);
	}

	private void UpdateRotation(float dt)
	{
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
			float boostAmount = boostTimer > 0f ? 1f : 0f;
			float shakeStrength = Mathf.Lerp(ShakeAmplitude, BoostShakeAmplitude, boostAmount);
			float shake =
				Mathf.Sin(swimTime * ShakeFrequency + GetInstanceId()) * shakeStrength +
				Mathf.Sin(swimTime * ShakeFrequency * 1.7f) * shakeStrength * 0.35f;

			fishSprite.Rotation = Mathf.LerpAngle(
				fishSprite.Rotation,
				targetRotation + wiggle + shake,
				dt * RotationSpeed
			);
			fishSprite.FlipH = facingDirection > 0;

			float positionShake = Mathf.Lerp(PositionShakePixels, PositionShakePixels * 2.1f, boostAmount);
			fishSprite.Position = new Vector2(
				0f,
				Mathf.Sin(swimTime * ShakeFrequency * 1.25f + GetInstanceId()) * positionShake
			);
		}
		else
		{
			fishSprite.Position = Vector2.Zero;
		}
	}

	private void UpdateFacingDirection()
	{
		if (Velocity.X > 8f)
			facingDirection = 1;
		else if (Velocity.X < -8f)
			facingDirection = -1;
	}

	private void UpdateSwimFrame()
	{
		if (fishSprite == null || swimFrames == null || swimFrames.Length == 0)
			return;

		int frameIndex = Mathf.PosMod((int)(swimTime * SwimFrameRate), swimFrames.Length);
		Texture2D texture = swimFrames[frameIndex];

		if (texture != null && fishSprite.Texture != texture)
			fishSprite.Texture = texture;
	}
}
