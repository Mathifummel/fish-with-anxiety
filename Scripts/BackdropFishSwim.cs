using Godot;

public enum FishSwimPath
{
	LeftToRight,
	RightToLeft,
	TopLeftToBottomRight,
	TopRightToBottomLeft,
	TopToBottom,
	BottomToTop,
}

public static class BackdropFishSwim
{
	private static readonly FishSwimPath[] AllPaths =
	{
		FishSwimPath.LeftToRight,
		FishSwimPath.RightToLeft,
		FishSwimPath.TopLeftToBottomRight,
		FishSwimPath.TopRightToBottomLeft,
		FishSwimPath.TopToBottom,
		FishSwimPath.BottomToTop,
	};

	public static float GetPathLength(FishSwimPath path, Vector2 viewport)
	{
		GetEndpoints(path, viewport, out Vector2 start, out Vector2 end);
		return start.DistanceTo(end);
	}

	public static FishSwimPath PickNextPath(FishSwimPath current)
	{
		if (AllPaths.Length <= 1)
			return current;

		FishSwimPath next = current;

		for (int attempt = 0; attempt < 6 && next == current; attempt++)
			next = AllPaths[(int)(GD.Randi() % AllPaths.Length)];

		return next;
	}

	public static Vector2 SamplePosition(
		FishSwimPath path,
		Vector2 viewport,
		float progress,
		float wobblePhase,
		float wobbleStrength,
		out Vector2 tangent)
	{
		GetEndpoints(path, viewport, out Vector2 start, out Vector2 end);
		float t = Mathf.Clamp(progress, 0f, 1f);
		Vector2 core = start.Lerp(end, t);
		Vector2 direction = (end - start).Normalized();
		Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
		float wave = Mathf.Sin(wobblePhase * 2.05f + t * Mathf.Tau) * wobbleStrength;
		float sway = Mathf.Cos(wobblePhase * 1.55f + t * 4.2f) * wobbleStrength * 0.35f;

		tangent = direction + perpendicular * sway * 0.2f;
		if (tangent.LengthSquared() < 0.0001f)
			tangent = direction;

		return core + perpendicular * wave;
	}

	public static float GetLeaderRotation(Vector2 previousPosition, Vector2 currentPosition, Vector2 fallbackTangent)
	{
		Vector2 delta = currentPosition - previousPosition;

		if (delta.LengthSquared() > 0.25f)
			return delta.Angle() + Mathf.Pi;

		return fallbackTangent.Angle() + Mathf.Pi;
	}

	public static void PlaceFollowers(
		Vector2 leaderPosition,
		float leaderRotation,
		Vector2 leaderTangent,
		Sprite2D[] followers,
		float wobblePhase,
		float lagBase,
		float lagStep,
		float laneSpread,
		float wobbleStrength)
	{
		Vector2 backDirection = -leaderTangent.Normalized();
		Vector2 sideDirection = new Vector2(-backDirection.Y, backDirection.X);

		for (int i = 0; i < followers.Length; i++)
		{
			float lag = lagBase + i * lagStep + Mathf.Sin(wobblePhase * 1.65f + i) * 7f;
			float lane = (i - (followers.Length - 1) * 0.5f) * laneSpread;
			float wave = Mathf.Sin(wobblePhase * 1.45f + i * 1.12f) * wobbleStrength;

			followers[i].Position =
				leaderPosition +
				backDirection * lag +
				sideDirection * lane +
				new Vector2(0f, wave);

			followers[i].Rotation =
				leaderRotation + Mathf.Sin(wobblePhase * 1.8f + i) * 0.08f;
		}
	}

	private static void GetEndpoints(FishSwimPath path, Vector2 viewport, out Vector2 start, out Vector2 end)
	{
		float padX = viewport.X * 0.14f;
		float padY = viewport.Y * 0.16f;

		switch (path)
		{
			case FishSwimPath.RightToLeft:
				start = new Vector2(viewport.X + padX, viewport.Y * 0.42f);
				end = new Vector2(-padX, viewport.Y * 0.36f);
				break;
			case FishSwimPath.TopLeftToBottomRight:
				start = new Vector2(-padX, -padY);
				end = new Vector2(viewport.X + padX, viewport.Y + padY);
				break;
			case FishSwimPath.TopRightToBottomLeft:
				start = new Vector2(viewport.X + padX, -padY);
				end = new Vector2(-padX, viewport.Y + padY);
				break;
			case FishSwimPath.TopToBottom:
				start = new Vector2(viewport.X * 0.46f, -padY);
				end = new Vector2(viewport.X * 0.54f, viewport.Y + padY);
				break;
			case FishSwimPath.BottomToTop:
				start = new Vector2(viewport.X * 0.52f, viewport.Y + padY);
				end = new Vector2(viewport.X * 0.48f, -padY);
				break;
			default:
				start = new Vector2(-padX, viewport.Y * 0.4f);
				end = new Vector2(viewport.X + padX, viewport.Y * 0.44f);
				break;
		}
	}
}
