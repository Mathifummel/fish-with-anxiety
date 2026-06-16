using Godot;

public partial class WaterLevelBounds : Node
{
	[Export] public NodePath PlayerPath;
	[Export] public bool ApplyContinuously = true;
	[Export] public float WaterSurfaceY = LevelBackground.WATER_SURFACE_Y;
	[Export] public float SandY = LevelBackground.SAND_Y;
	[Export] public float SurfacePadding = LevelBackground.PLAYER_SURFACE_PADDING;
	[Export] public float SandPadding = LevelBackground.PLAYER_SAND_PADDING;

	private PlayerFish player;

	public override void _Ready()
	{
		ResolvePlayer();
		ApplyBounds();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (ApplyContinuously)
			ApplyBounds();
	}

	public void Configure(PlayerFish target, float waterSurfaceY, float sandY, float surfacePadding, float sandPadding)
	{
		player = target;
		WaterSurfaceY = waterSurfaceY;
		SandY = sandY;
		SurfacePadding = surfacePadding;
		SandPadding = sandPadding;
		ApplyBounds();
	}

	private void ResolvePlayer()
	{
		if (player != null && IsInstanceValid(player))
			return;

		if (!PlayerPath.IsEmpty)
			player = GetNodeOrNull<PlayerFish>(PlayerPath);
	}

	private void ApplyBounds()
	{
		ResolvePlayer();

		if (player == null || !IsInstanceValid(player))
			return;

		player.UseSwimBounds = true;
		player.MinSwimY = WaterSurfaceY + SurfacePadding;
		player.MaxSwimY = SandY - SandPadding;
	}
}
