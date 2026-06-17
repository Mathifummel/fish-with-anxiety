using Godot;

public static class SandBoundary
{
	public const string LevelBackgroundGroup = "level_background";

	public static float GetSandSurfaceY(Node context, float x)
	{
		LevelBackground background = FindWorldBackground(context);
		if (background != null)
			return background.GetSandSurfaceYAt(x);

		return LevelBackground.GetFallbackSandSurfaceY(x, LevelBackground.SAND_Y);
	}

	public static float GetMaxSwimY(Node context, float x, float padding)
	{
		return GetSandSurfaceY(context, x) - Mathf.Max(0f, padding);
	}

	public static Vector2 ClampCharacterAboveSand(
		CharacterBody2D body,
		Vector2 velocity,
		float padding,
		float pushSpeed
	)
	{
		if (body == null)
			return velocity;

		Vector2 position = body.GlobalPosition;
		float maxY = GetMaxSwimY(body, position.X, padding);

		if (position.Y <= maxY)
			return velocity;

		position.Y = maxY;
		body.GlobalPosition = position;
		velocity.Y = Mathf.Min(velocity.Y, -Mathf.Max(0f, pushSpeed));
		body.Velocity = velocity;
		return velocity;
	}

	public static Vector2 ClampCharacterInsideWater(
		CharacterBody2D body,
		Vector2 velocity,
		float sandPadding,
		float surfacePadding,
		float pushSpeed
	)
	{
		if (body == null)
			return velocity;

		Vector2 position = body.GlobalPosition;
		float minY = LevelBackground.WATER_SURFACE_Y + Mathf.Max(0f, surfacePadding);
		float maxY = GetMaxSwimY(body, position.X, sandPadding);

		if (position.Y < minY)
		{
			position.Y = minY;
			body.GlobalPosition = position;
			velocity.Y = Mathf.Max(velocity.Y, Mathf.Max(0f, pushSpeed));
			body.Velocity = velocity;
			return velocity;
		}

		if (position.Y <= maxY)
			return velocity;

		position.Y = maxY;
		body.GlobalPosition = position;
		velocity.Y = Mathf.Min(velocity.Y, -Mathf.Max(0f, pushSpeed));
		body.Velocity = velocity;
		return velocity;
	}

	private static LevelBackground FindWorldBackground(Node context)
	{
		if (context == null)
			return null;

		if (!context.IsInsideTree())
			return null;

		SceneTree tree = context.GetTree();
		if (tree == null)
			return null;

		foreach (Node node in tree.GetNodesInGroup(LevelBackgroundGroup))
		{
			if (node is LevelBackground background &&
				background.Mode == LevelBackground.BackgroundMode.World)
			{
				return background;
			}
		}

		return tree.GetFirstNodeInGroup(LevelBackgroundGroup) as LevelBackground;
	}
}
