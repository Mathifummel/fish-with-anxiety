using Godot;
using System.Collections.Generic;

public partial class LevelBackground : Node2D
{
	public enum BackgroundMode
	{
		Screen,
		World
	}

	public const float WATER_SURFACE_Y = -80f;
	public const float SAND_Y = 760f;
	public const float WATER_SURFACE_STRIP_HEIGHT = 190f;
	public const float PLAYER_SURFACE_PADDING = 54f;
	public const float PLAYER_SAND_PADDING = 92f;
	public const float PLAYER_MIN_Y = WATER_SURFACE_Y + PLAYER_SURFACE_PADDING;
	public const float PLAYER_MAX_Y = SAND_Y - PLAYER_SAND_PADDING;

	private const string SkyTexturePath = "res://Assets/Himmelneu.png";
	private const string WaterTexturePath = "res://Assets/wasserneu2.png";
	private const string SandTexturePath = "res://Assets/sandneu2.png";
	private const string WaterSurfaceTexturePath = "res://Assets/Wasserrand.png";
	private const string DetailPackTexturePath = "res://Assets/Detailpack1.png";

	private const float ScreenWaterSurfaceRatio = 0.30f;
	private const float ScreenSandRatio = 0.86f;
	private const float SkySourceY = 680f;
	private const float WaterSourceY = 0f;
	private const float WaterSurfaceSourceY = 220f;
	private const float WaterSurfaceVisualOffset = 86f;
	private const float HorizontalMargin = 960f;
	private const float VerticalMargin = 480f;
	private const float DecorationRepeatWidth = 1254f;

	[Export] public BackgroundMode Mode = BackgroundMode.Screen;
	[Export] public float WaterSurfaceY = WATER_SURFACE_Y;
	[Export] public float SandY = SAND_Y;
	[Export] public float SurfaceStripHeight = WATER_SURFACE_STRIP_HEIGHT;
	[Export] public float PlayerSurfacePadding = PLAYER_SURFACE_PADDING;
	[Export] public float PlayerSandPadding = PLAYER_SAND_PADDING;
	[Export] public bool EnableDecorations = true;

	private Texture2D skyTexture;
	private Texture2D waterTexture;
	private Texture2D sandTexture;
	private Texture2D waterSurfaceTexture;
	private Texture2D detailPackTexture;

	private Sprite2D skyLayer;
	private Sprite2D waterLayer;
	private Sprite2D sandLayer;
	private Sprite2D waterSurfaceLayer;
	private Node2D decorationRoot;
	private Node2D followTarget;
	private WaterLevelBounds swimBounds;
	private float time = 0f;

	private static readonly Dictionary<string, Texture2D> keyedTextureCache = new Dictionary<string, Texture2D>();

	private readonly List<DecorationEntry> decorationEntries = new List<DecorationEntry>();

	private enum DecorationAnchor
	{
		Water,
		Sand
	}

	private sealed class DecorationSpec
	{
		public Rect2 Source;
		public float LocalX;
		public float LocalY;
		public float Scale;
		public DecorationAnchor Anchor;
		public int ZIndex;

		public DecorationSpec(Rect2 source, float localX, float localY, float scale, DecorationAnchor anchor, int zIndex)
		{
			Source = source;
			LocalX = localX;
			LocalY = localY;
			Scale = scale;
			Anchor = anchor;
			ZIndex = zIndex;
		}
	}

	private sealed class DecorationEntry
	{
		public Sprite2D Sprite;
		public DecorationSpec Spec;
		public int CycleOffset;
	}

	private readonly DecorationSpec[] decorationSpecs = new DecorationSpec[]
	{
		new DecorationSpec(new Rect2(150f, 120f, 280f, 350f), 190f, -160f, 0.58f, DecorationAnchor.Sand, 6),
		new DecorationSpec(new Rect2(545f, 150f, 230f, 335f), 640f, -145f, 0.55f, DecorationAnchor.Sand, 6),
		new DecorationSpec(new Rect2(850f, 555f, 265f, 235f), 985f, -98f, 0.58f, DecorationAnchor.Sand, 6),
		new DecorationSpec(new Rect2(135f, 560f, 310f, 210f), 390f, -62f, 0.52f, DecorationAnchor.Sand, 5),
		new DecorationSpec(new Rect2(535f, 535f, 275f, 245f), 770f, -78f, 0.56f, DecorationAnchor.Sand, 5),
		new DecorationSpec(new Rect2(155f, 900f, 235f, 190f), 1130f, -42f, 0.44f, DecorationAnchor.Sand, 7),
		new DecorationSpec(new Rect2(535f, 900f, 260f, 200f), 70f, -46f, 0.42f, DecorationAnchor.Sand, 7),
		new DecorationSpec(new Rect2(875f, 895f, 250f, 190f), 520f, -38f, 0.40f, DecorationAnchor.Sand, 7),
		new DecorationSpec(new Rect2(890f, 165f, 165f, 305f), 1040f, 235f, 0.46f, DecorationAnchor.Water, 4)
	};

	public override void _Ready()
	{
		ZIndex = -1000;
		ZAsRelative = false;
		ProcessMode = ProcessModeEnum.Always;

		LoadTextures();
		BuildLayers();
		UpdateLayers();
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		UpdateLayers();
	}

	public void ConfigureForWorld(Node2D target)
	{
		Mode = BackgroundMode.World;
		followTarget = target;
		EnsureSwimBounds(target);
		UpdateLayers();
	}

	public void ConfigureForScreen()
	{
		Mode = BackgroundMode.Screen;
		followTarget = null;
		UpdateLayers();
	}

	private void LoadTextures()
	{
		skyTexture = LoadTexture(SkyTexturePath);
		waterTexture = LoadTexture(WaterTexturePath);
		sandTexture = LoadTexture(SandTexturePath);
		waterSurfaceTexture = LoadTextureWithCheckerTransparency(WaterSurfaceTexturePath, 8);
		detailPackTexture = LoadTextureWithCheckerTransparency(DetailPackTexturePath, -1);
	}

	private void BuildLayers()
	{
		skyLayer = CreateRepeatingLayer("Sky", skyTexture, -40, null);
		waterLayer = CreateRepeatingLayer("WaterBackground", waterTexture, -30, null);
		sandLayer = CreateRepeatingLayer("Sand", sandTexture, -20, null);
		waterSurfaceLayer = CreateRepeatingLayer("WaterSurface", waterSurfaceTexture, -10, null);

		decorationRoot = new Node2D();
		decorationRoot.Name = "Decorations";
		decorationRoot.ZIndex = -5;
		AddChild(decorationRoot);
		BuildDecorations();
	}

	private Sprite2D CreateRepeatingLayer(string layerName, Texture2D texture, int zIndex, Material material)
	{
		Sprite2D sprite = new Sprite2D();
		sprite.Name = layerName;
		sprite.Texture = texture;
		sprite.Centered = false;
		sprite.RegionEnabled = true;
		sprite.TextureRepeat = TextureRepeatEnum.Enabled;
		sprite.TextureFilter = TextureFilterEnum.Nearest;
		sprite.ZIndex = zIndex;
		sprite.Material = material;
		AddChild(sprite);
		return sprite;
	}

	private void BuildDecorations()
	{
		if (!EnableDecorations || detailPackTexture == null || decorationRoot == null)
			return;

		for (int cycle = -3; cycle <= 3; cycle++)
		{
			foreach (DecorationSpec spec in decorationSpecs)
			{
				Sprite2D sprite = new Sprite2D();
				sprite.Name = $"Deco_{spec.Anchor}_{decorationEntries.Count:00}";
				sprite.Texture = detailPackTexture;
				sprite.RegionEnabled = true;
				sprite.RegionRect = spec.Source;
				sprite.Centered = true;
				sprite.TextureFilter = TextureFilterEnum.Nearest;
				sprite.ZIndex = spec.ZIndex;
				sprite.Scale = new Vector2(spec.Scale, spec.Scale);
				decorationRoot.AddChild(sprite);

				decorationEntries.Add(new DecorationEntry
				{
					Sprite = sprite,
					Spec = spec,
					CycleOffset = cycle
				});
			}
		}
	}

	private void UpdateLayers()
	{
		if (skyLayer == null || waterLayer == null || sandLayer == null || waterSurfaceLayer == null)
			return;

		if (Mode == BackgroundMode.World)
			UpdateWorldLayers();
		else
			UpdateScreenLayers();
	}

	private void UpdateWorldLayers()
	{
		Vector2 viewportSize = GetViewportSize();
		Vector2 center = GetWorldViewCenter();
		Vector2 visibleSize = GetWorldVisibleSize(viewportSize);
		Rect2 area = new Rect2(
			center - visibleSize * 0.5f - new Vector2(HorizontalMargin, VerticalMargin),
			visibleSize + new Vector2(HorizontalMargin * 2f, VerticalMargin * 2f)
		);

		float scrollX = center.X * 0.08f + time * 6f;
		float surfaceTop = WaterSurfaceY - WaterSurfaceVisualOffset;

		ApplyLayer(skyLayer, area.Position.X, area.Position.Y, area.Size.X, WaterSurfaceY - area.Position.Y, scrollX * 0.18f, SkySourceY);
		ApplyLayer(waterLayer, area.Position.X, WaterSurfaceY, area.Size.X, SandY - WaterSurfaceY, scrollX, WaterSourceY);
		ApplyLayer(sandLayer, area.Position.X, SandY, area.Size.X, area.End.Y - SandY, scrollX * 0.45f, 0f);
		ApplyLayer(waterSurfaceLayer, area.Position.X, surfaceTop, area.Size.X, SurfaceStripHeight, scrollX * 0.72f, WaterSurfaceSourceY);
		UpdateDecorations(area, center, WaterSurfaceY, SandY);
		UpdateSwimBounds();
	}

	private void UpdateScreenLayers()
	{
		Vector2 viewport = GetViewportSize();
		float surfaceY = Mathf.Round(viewport.Y * ScreenWaterSurfaceRatio);
		float sandY = Mathf.Round(viewport.Y * ScreenSandRatio);
		float scrollX = time * 15f;
		float surfaceOffset = Mathf.Clamp(viewport.Y * 0.12f, 64f, WaterSurfaceVisualOffset);
		float surfaceHeight = Mathf.Clamp(viewport.Y * 0.27f, 150f, SurfaceStripHeight);

		Rect2 area = new Rect2(Vector2.Zero, viewport);

		ApplyLayer(skyLayer, 0f, 0f, viewport.X, surfaceY, scrollX * 0.18f, SkySourceY);
		ApplyLayer(waterLayer, 0f, surfaceY, viewport.X, sandY - surfaceY, scrollX, WaterSourceY + Mathf.Sin(time * 0.18f) * 8f);
		ApplyLayer(sandLayer, 0f, sandY, viewport.X, viewport.Y - sandY, scrollX * 0.45f, 0f);
		ApplyLayer(waterSurfaceLayer, 0f, surfaceY - surfaceOffset, viewport.X, surfaceHeight, scrollX * 0.72f, WaterSurfaceSourceY);
		UpdateDecorations(area, viewport * 0.5f, surfaceY, sandY);
	}

	private void ApplyLayer(Sprite2D sprite, float x, float y, float width, float height, float sourceX, float sourceY)
	{
		if (sprite == null || sprite.Texture == null || width <= 1f || height <= 1f)
		{
			if (sprite != null)
				sprite.Visible = false;
			return;
		}

		sprite.Visible = true;
		sprite.Position = new Vector2(x, y);
		sprite.RegionRect = new Rect2(
			new Vector2(sourceX, sourceY),
			new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height))
		);
	}

	private void UpdateDecorations(Rect2 area, Vector2 center, float waterSurfaceY, float sandY)
	{
		if (!EnableDecorations || decorationEntries.Count == 0)
		{
			if (decorationRoot != null)
				decorationRoot.Visible = false;
			return;
		}

		decorationRoot.Visible = true;
		int baseCycle = Mathf.FloorToInt(area.Position.X / DecorationRepeatWidth);

		foreach (DecorationEntry entry in decorationEntries)
		{
			float x = (baseCycle + entry.CycleOffset) * DecorationRepeatWidth + entry.Spec.LocalX;
			float y = entry.Spec.Anchor == DecorationAnchor.Sand
				? sandY + entry.Spec.LocalY
				: waterSurfaceY + entry.Spec.LocalY + Mathf.Sin(time * 0.45f + entry.Spec.LocalX * 0.01f) * 10f;

			entry.Sprite.Position = new Vector2(Mathf.Round(x), Mathf.Round(y));
		}
	}

	private void EnsureSwimBounds(Node2D target)
	{
		if (target is not PlayerFish player)
			return;

		if (swimBounds == null || !IsInstanceValid(swimBounds))
		{
			swimBounds = new WaterLevelBounds();
			swimBounds.Name = "WaterSwimBounds";
			AddChild(swimBounds);
		}

		swimBounds.Configure(player, WaterSurfaceY, SandY, PlayerSurfacePadding, PlayerSandPadding);
	}

	private void UpdateSwimBounds()
	{
		if (followTarget is PlayerFish player)
			EnsureSwimBounds(player);
	}

	private Texture2D LoadTexture(string path)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(path);

		if (texture == null)
			GD.PushWarning($"Level background texture missing: {path}");

		return texture;
	}

	private Texture2D LoadTextureWithCheckerTransparency(string path, int preserveRadius)
	{
		if (keyedTextureCache.TryGetValue(path, out Texture2D cachedTexture))
			return cachedTexture;

		Texture2D sourceTexture = LoadTexture(path);
		Image image = sourceTexture?.GetImage();

		if (image == null || image.IsEmpty())
			return sourceTexture;

		if (image.GetFormat() != Image.Format.Rgba8)
			image.Convert(Image.Format.Rgba8);

		RemoveCheckerBackground(image, preserveRadius);
		Texture2D texture = ImageTexture.CreateFromImage(image);
		keyedTextureCache[path] = texture;
		return texture;
	}

	private void RemoveCheckerBackground(Image image, int preserveRadius)
	{
		int width = image.GetWidth();
		int height = image.GetHeight();

		if (preserveRadius < 0)
		{
			RemoveEdgeConnectedCheckerBackground(image, width, height);
			return;
		}

		bool[] objectMask = new bool[width * height];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Color color = image.GetPixel(x, y);
				objectMask[y * width + x] = !IsCheckerPixel(color);
			}
		}

		bool[] preservedCheckerPixels = DilateMask(objectMask, width, height, preserveRadius);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				Color color = image.GetPixel(x, y);

				if (IsCheckerPixel(color) && !preservedCheckerPixels[index])
					color.A = 0f;
				else
					color.A = 1f;

				image.SetPixel(x, y, color);
			}
		}
	}

	private void RemoveEdgeConnectedCheckerBackground(Image image, int width, int height)
	{
		bool[] backgroundMask = new bool[width * height];
		Queue<int> pending = new Queue<int>();

		for (int x = 0; x < width; x++)
		{
			QueueCheckerPixel(image, backgroundMask, pending, x, 0, width);
			QueueCheckerPixel(image, backgroundMask, pending, x, height - 1, width);
		}

		for (int y = 1; y < height - 1; y++)
		{
			QueueCheckerPixel(image, backgroundMask, pending, 0, y, width);
			QueueCheckerPixel(image, backgroundMask, pending, width - 1, y, width);
		}

		while (pending.Count > 0)
		{
			int index = pending.Dequeue();
			int x = index % width;
			int y = index / width;

			if (x > 0)
				QueueCheckerPixel(image, backgroundMask, pending, x - 1, y, width);
			if (x < width - 1)
				QueueCheckerPixel(image, backgroundMask, pending, x + 1, y, width);
			if (y > 0)
				QueueCheckerPixel(image, backgroundMask, pending, x, y - 1, width);
			if (y < height - 1)
				QueueCheckerPixel(image, backgroundMask, pending, x, y + 1, width);
		}

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				Color color = image.GetPixel(x, y);
				color.A = backgroundMask[index] ? 0f : 1f;
				image.SetPixel(x, y, color);
			}
		}
	}

	private void QueueCheckerPixel(
		Image image,
		bool[] backgroundMask,
		Queue<int> pending,
		int x,
		int y,
		int width
	)
	{
		int index = y * width + x;

		if (backgroundMask[index] || !IsCheckerPixel(image.GetPixel(x, y)))
			return;

		backgroundMask[index] = true;
		pending.Enqueue(index);
	}

	private bool[] DilateMask(bool[] source, int width, int height, int radius)
	{
		bool[] horizontal = new bool[source.Length];
		bool[] result = new bool[source.Length];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (!source[y * width + x])
					continue;

				int fromX = Mathf.Max(0, x - radius);
				int toX = Mathf.Min(width - 1, x + radius);

				for (int markX = fromX; markX <= toX; markX++)
					horizontal[y * width + markX] = true;
			}
		}

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (!horizontal[y * width + x])
					continue;

				int fromY = Mathf.Max(0, y - radius);
				int toY = Mathf.Min(height - 1, y + radius);

				for (int markY = fromY; markY <= toY; markY++)
					result[markY * width + x] = true;
			}
		}

		return result;
	}

	private bool IsCheckerPixel(Color color)
	{
		float maxChannel = Mathf.Max(color.R, Mathf.Max(color.G, color.B));
		float minChannel = Mathf.Min(color.R, Mathf.Min(color.G, color.B));
		float saturation = maxChannel - minChannel;

		return maxChannel > 0.82f && saturation < 0.06f;
	}

	private Vector2 GetWorldViewCenter()
	{
		Camera2D camera = GetViewport().GetCamera2D();

		if (camera != null)
			return camera.GlobalPosition;

		if (followTarget != null && IsInstanceValid(followTarget))
			return followTarget.GlobalPosition;

		return Vector2.Zero;
	}

	private Vector2 GetWorldVisibleSize(Vector2 viewportSize)
	{
		Camera2D camera = GetViewport().GetCamera2D();

		if (camera == null)
			return viewportSize;

		return new Vector2(
			viewportSize.X / Mathf.Max(camera.Zoom.X, 0.01f),
			viewportSize.Y / Mathf.Max(camera.Zoom.Y, 0.01f)
		);
	}

	private Vector2 GetViewportSize()
	{
		Vector2 size = GetViewportRect().Size;
		return size.X > 1f && size.Y > 1f ? size : new Vector2(1280f, 720f);
	}
}
