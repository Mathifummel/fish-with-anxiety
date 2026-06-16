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
	public const float WATER_SURFACE_STRIP_HEIGHT = 724f;
	public const float PLAYER_SURFACE_PADDING = 38f;
	public const float PLAYER_SAND_PADDING = 36f;
	public const float PLAYER_MIN_Y = WATER_SURFACE_Y + PLAYER_SURFACE_PADDING;
	public const float PLAYER_MAX_Y = SAND_Y - PLAYER_SAND_PADDING;

	private static readonly string[] SkyTexturePaths = { "res://Assets/sky_background.png", "res://Assets/Himmelneu.png" };
	private static readonly string[] WaterTexturePaths = { "res://Assets/water_background.png", "res://Assets/wasserneu2.png" };
	private static readonly string[] SandFillTexturePaths = { "res://Assets/sand_fill.png", "res://Assets/sandneu2.png" };
	private static readonly string[] SandEdgeTexturePaths = { "res://Assets/sand_edge_strip.png", "res://Assets/sandneu2.png" };
	private static readonly string[] WaterSurfaceTexturePaths = { "res://Assets/water_surface_strip.png", "res://Assets/Wasserrand.png" };
	private static readonly string[] DecorationTexturePaths = { "res://Assets/decoration_pack.png", "res://Assets/Detailpack1.png" };

	private const float ScreenWaterSurfaceRatio = 0.30f;
	private const float ScreenSandRatio = 0.86f;
	private const float SkySourceY = 260f;
	private const float WaterSourceY = 0f;
	private const float WaterSurfaceOriginY = 355f;
	private const float SandEdgeOriginY = 420f;
	private const float SandFillOverlap = 260f;
	private const float HorizontalMargin = 960f;
	private const float VerticalMargin = 480f;
	private const float DecorationRepeatWidth = 1254f;
	private const float SandWaveA = 34f;
	private const float SandWaveB = 18f;
	private const float SandWaveC = 9f;

	[Export] public BackgroundMode Mode = BackgroundMode.Screen;
	[Export] public float WaterSurfaceY = WATER_SURFACE_Y;
	[Export] public float SandY = SAND_Y;
	[Export] public float SurfaceStripHeight = WATER_SURFACE_STRIP_HEIGHT;
	[Export] public float PlayerSurfacePadding = PLAYER_SURFACE_PADDING;
	[Export] public float PlayerSandPadding = PLAYER_SAND_PADDING;
	[Export] public bool EnableDecorations = true;

	private Texture2D skyTexture;
	private Texture2D waterTexture;
	private Texture2D sandFillTexture;
	private Texture2D sandEdgeTexture;
	private Texture2D waterSurfaceTexture;
	private Texture2D decorationPackTexture;

	private Sprite2D skyLayer;
	private Sprite2D waterLayer;
	private Sprite2D sandFillLayer;
	private Sprite2D sandEdgeLayer;
	private Sprite2D waterSurfaceLayer;
	private Node2D decorationRoot;
	private Node2D followTarget;
	private WaterLevelBounds swimBounds;
	private float time = 0f;
	private int[] sandEdgeTopPixels;

	private static readonly Dictionary<string, Texture2D> keyedTextureCache = new Dictionary<string, Texture2D>();
	private readonly Dictionary<string, Texture2D> decorationTextureCache = new Dictionary<string, Texture2D>();
	private readonly List<DecorationEntry> decorationEntries = new List<DecorationEntry>();

	private enum DecorationAnchor
	{
		SandBack,
		Sand,
		Bubble
	}

	private sealed class DecorationSpec
	{
		public Rect2 Source;
		public float LocalX;
		public float GroundOffset;
		public float Scale;
		public DecorationAnchor Anchor;
		public int ZIndex;
		public float Chance;
		public float JitterX;
		public float BubbleSpeed;
		public Color Modulate;

		public DecorationSpec(
			Rect2 source,
			float localX,
			float groundOffset,
			float scale,
			DecorationAnchor anchor,
			int zIndex,
			float chance = 1f,
			float jitterX = 80f,
			float bubbleSpeed = 0f,
			Color? modulate = null
		)
		{
			Source = source;
			LocalX = localX;
			GroundOffset = groundOffset;
			Scale = scale;
			Anchor = anchor;
			ZIndex = zIndex;
			Chance = chance;
			JitterX = jitterX;
			BubbleSpeed = bubbleSpeed;
			Modulate = modulate ?? Colors.White;
		}
	}

	private sealed class DecorationEntry
	{
		public Sprite2D Sprite;
		public DecorationSpec Spec;
		public int CycleOffset;
		public int SpecIndex;
		public float TextureHeight;
		public float VisibleBottomInset;
	}

	private readonly DecorationSpec[] decorationSpecs =
	{
		new DecorationSpec(new Rect2(120f, 350f, 235f, 220f), 260f, 16f, 0.50f, DecorationAnchor.Sand, -4, 0.55f, 95f),
		new DecorationSpec(new Rect2(480f, 350f, 245f, 220f), 610f, 14f, 0.48f, DecorationAnchor.Sand, -4, 0.56f, 95f),
		new DecorationSpec(new Rect2(810f, 350f, 280f, 220f), 935f, 18f, 0.50f, DecorationAnchor.Sand, -4, 0.55f, 95f),
		new DecorationSpec(new Rect2(65f, 625f, 260f, 155f), 160f, 18f, 0.48f, DecorationAnchor.Sand, -3, 0.62f, 105f),
		new DecorationSpec(new Rect2(375f, 625f, 225f, 150f), 500f, 18f, 0.43f, DecorationAnchor.Sand, -3, 0.54f, 105f),
		new DecorationSpec(new Rect2(685f, 600f, 210f, 175f), 820f, 20f, 0.46f, DecorationAnchor.Sand, -3, 0.50f, 105f),
		new DecorationSpec(new Rect2(940f, 625f, 250f, 150f), 1120f, 18f, 0.44f, DecorationAnchor.Sand, -3, 0.54f, 105f),
		new DecorationSpec(new Rect2(125f, 805f, 170f, 140f), 330f, 10f, 0.34f, DecorationAnchor.Sand, -2, 0.48f, 80f),
		new DecorationSpec(new Rect2(415f, 805f, 160f, 120f), 675f, 9f, 0.34f, DecorationAnchor.Sand, -2, 0.48f, 80f),
		new DecorationSpec(new Rect2(690f, 805f, 170f, 120f), 980f, 9f, 0.34f, DecorationAnchor.Sand, -2, 0.48f, 80f),
		new DecorationSpec(new Rect2(265f, 960f, 385f, 120f), 415f, 18f, 0.36f, DecorationAnchor.Sand, -2, 0.45f, 95f),
		new DecorationSpec(new Rect2(680f, 960f, 320f, 105f), 880f, 17f, 0.36f, DecorationAnchor.Sand, -2, 0.42f, 95f),
		new DecorationSpec(new Rect2(210f, 1095f, 125f, 145f), 210f, 0f, 0.42f, DecorationAnchor.Bubble, -6, 0.70f, 160f, 42f, new Color(1f, 1f, 1f, 0.62f)),
		new DecorationSpec(new Rect2(545f, 1085f, 160f, 160f), 610f, 0f, 0.44f, DecorationAnchor.Bubble, -6, 0.72f, 180f, 52f, new Color(1f, 1f, 1f, 0.66f)),
		new DecorationSpec(new Rect2(910f, 1090f, 150f, 155f), 1040f, 0f, 0.42f, DecorationAnchor.Bubble, -6, 0.70f, 160f, 46f, new Color(1f, 1f, 1f, 0.60f))
	};

	public override void _Ready()
	{
		ZIndex = -1000;
		ZAsRelative = false;
		ProcessMode = ProcessModeEnum.Always;
		AddToGroup(SandBoundary.LevelBackgroundGroup);

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
		skyTexture = LoadFirstTexture(SkyTexturePaths);
		waterTexture = LoadFirstTexture(WaterTexturePaths);
		sandFillTexture = LoadFirstTexture(SandFillTexturePaths);
		sandEdgeTexture = LoadFirstTextureWithCheckerTransparency(SandEdgeTexturePaths, -1);
		waterSurfaceTexture = LoadFirstTextureWithCheckerTransparency(WaterSurfaceTexturePaths, 8);
		decorationPackTexture = LoadFirstTextureWithCheckerTransparency(DecorationTexturePaths, -1);
		sandEdgeTopPixels = BuildSandEdgeTopCache(sandEdgeTexture);
	}

	private void BuildLayers()
	{
		skyLayer = CreateRepeatingLayer("Sky", skyTexture, -40);
		waterLayer = CreateRepeatingLayer("WaterBackground", waterTexture, -30);
		sandFillLayer = CreateRepeatingLayer("SandFill", sandFillTexture, -20);
		sandEdgeLayer = CreateRepeatingLayer("SandEdge", sandEdgeTexture, -12);
		waterSurfaceLayer = CreateRepeatingLayer("WaterSurface", waterSurfaceTexture, -10);

		decorationRoot = new Node2D();
		decorationRoot.Name = "Decorations";
		decorationRoot.ZIndex = -5;
		AddChild(decorationRoot);
		BuildDecorations();
	}

	private Sprite2D CreateRepeatingLayer(string layerName, Texture2D texture, int zIndex)
	{
		Sprite2D sprite = new Sprite2D();
		sprite.Name = layerName;
		sprite.Texture = texture;
		sprite.Centered = false;
		sprite.RegionEnabled = true;
		sprite.TextureRepeat = TextureRepeatEnum.Enabled;
		sprite.TextureFilter = TextureFilterEnum.Nearest;
		sprite.ZIndex = zIndex;
		AddChild(sprite);
		return sprite;
	}

	private void BuildDecorations()
	{
		if (!EnableDecorations || decorationPackTexture == null || decorationRoot == null)
			return;

		for (int cycle = -3; cycle <= 3; cycle++)
		{
			for (int i = 0; i < decorationSpecs.Length; i++)
			{
				DecorationSpec spec = decorationSpecs[i];
				Sprite2D sprite = new Sprite2D();
				sprite.Name = $"Deco_{spec.Anchor}_{decorationEntries.Count:00}";
				Texture2D texture = GetDecorationTexture(spec.Source);
				sprite.Texture = texture;
				sprite.Centered = true;
				sprite.TextureFilter = TextureFilterEnum.Nearest;
				sprite.ZIndex = spec.ZIndex;
				sprite.Scale = new Vector2(spec.Scale, spec.Scale);
				sprite.Modulate = spec.Modulate;
				decorationRoot.AddChild(sprite);

				decorationEntries.Add(new DecorationEntry
				{
					Sprite = sprite,
					Spec = spec,
					CycleOffset = cycle,
					SpecIndex = i,
					TextureHeight = texture?.GetHeight() ?? spec.Source.Size.Y,
					VisibleBottomInset = GetVisibleBottomInset(texture)
				});
			}
		}
	}

	private Texture2D GetDecorationTexture(Rect2 source)
	{
		string key = $"{source.Position.X:0},{source.Position.Y:0},{source.Size.X:0},{source.Size.Y:0}";

		if (decorationTextureCache.TryGetValue(key, out Texture2D cachedTexture))
			return cachedTexture;

		Image sourceImage = decorationPackTexture?.GetImage();

		if (sourceImage == null || sourceImage.IsEmpty())
			return decorationPackTexture;

		Rect2I sourceRect = new Rect2I(
			Mathf.RoundToInt(source.Position.X),
			Mathf.RoundToInt(source.Position.Y),
			Mathf.RoundToInt(source.Size.X),
			Mathf.RoundToInt(source.Size.Y)
		);
		Image cropped = Image.CreateEmpty(
			Mathf.Max(1, sourceRect.Size.X),
			Mathf.Max(1, sourceRect.Size.Y),
			false,
			Image.Format.Rgba8
		);
		cropped.BlitRect(sourceImage, sourceRect, Vector2I.Zero);

		Texture2D texture = ImageTexture.CreateFromImage(cropped);
		decorationTextureCache[key] = texture;
		return texture;
	}

	private int[] BuildSandEdgeTopCache(Texture2D texture)
	{
		Image image = texture?.GetImage();

		if (image == null || image.IsEmpty())
			return null;

		if (image.GetFormat() != Image.Format.Rgba8)
			image.Convert(Image.Format.Rgba8);

		int width = image.GetWidth();
		int height = image.GetHeight();
		int[] topPixels = new int[width];
		float totalTop = 0f;
		int foundColumns = 0;

		for (int x = 0; x < width; x++)
		{
			int top = height;

			for (int y = 0; y < height; y++)
			{
				if (image.GetPixel(x, y).A > 0.12f)
				{
					top = y;
					break;
				}
			}

			if (top < height)
			{
				topPixels[x] = top;
				totalTop += top;
				foundColumns++;
			}
			else
			{
				topPixels[x] = Mathf.RoundToInt(SandEdgeOriginY);
			}
		}

		if (foundColumns == 0)
			return null;

		float averageTop = totalTop / foundColumns;
		return averageTop < 80f ? null : topPixels;
	}

	private float GetVisibleBottomInset(Texture2D texture)
	{
		Image image = texture?.GetImage();

		if (image == null || image.IsEmpty())
			return 0f;

		if (image.GetFormat() != Image.Format.Rgba8)
			image.Convert(Image.Format.Rgba8);

		int width = image.GetWidth();
		int height = image.GetHeight();

		for (int y = height - 1; y >= 0; y--)
		{
			for (int x = 0; x < width; x++)
			{
				if (image.GetPixel(x, y).A > 0.12f)
					return height - 1 - y;
			}
		}

		return 0f;
	}

	private void UpdateLayers()
	{
		if (skyLayer == null || waterLayer == null || sandFillLayer == null || sandEdgeLayer == null || waterSurfaceLayer == null)
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
		float sandFillTop = sandEdgeTexture != null ? SandY + SandFillOverlap : SandY;

		ApplyLayer(skyLayer, area.Position.X, area.Position.Y, area.Size.X, WaterSurfaceY - area.Position.Y, scrollX * 0.18f, SkySourceY);
		ApplyLayer(waterLayer, area.Position.X, WaterSurfaceY, area.Size.X, SandY - WaterSurfaceY + SandFillOverlap, scrollX, WaterSourceY);
		ApplyLayer(sandFillLayer, area.Position.X, sandFillTop, area.Size.X, area.End.Y - sandFillTop, area.Position.X, 0f);
		ApplyLayer(sandEdgeLayer, area.Position.X, SandY - SandEdgeOriginY, area.Size.X, sandEdgeTexture?.GetHeight() ?? 1f, area.Position.X, 0f);
		ApplyLayer(waterSurfaceLayer, area.Position.X, WaterSurfaceY - WaterSurfaceOriginY, area.Size.X, SurfaceStripHeight, scrollX * 0.72f, 0f);
		UpdateDecorations(area, WaterSurfaceY, SandY);
		UpdateSwimBounds();
	}

	private void UpdateScreenLayers()
	{
		Vector2 viewport = GetViewportSize();
		float surfaceY = Mathf.Round(viewport.Y * ScreenWaterSurfaceRatio);
		float sandY = Mathf.Round(viewport.Y * ScreenSandRatio);
		float scrollX = time * 15f;
		float sandFillTop = sandEdgeTexture != null ? sandY + SandFillOverlap : sandY;

		Rect2 area = new Rect2(Vector2.Zero, viewport);

		ApplyLayer(skyLayer, 0f, 0f, viewport.X, surfaceY, scrollX * 0.18f, SkySourceY);
		ApplyLayer(waterLayer, 0f, surfaceY, viewport.X, sandY - surfaceY + SandFillOverlap, scrollX, WaterSourceY + Mathf.Sin(time * 0.18f) * 8f);
		ApplyLayer(sandFillLayer, 0f, sandFillTop, viewport.X, viewport.Y - sandFillTop, 0f, 0f);
		ApplyLayer(sandEdgeLayer, 0f, sandY - SandEdgeOriginY, viewport.X, sandEdgeTexture?.GetHeight() ?? 1f, 0f, 0f);
		ApplyLayer(waterSurfaceLayer, 0f, surfaceY - WaterSurfaceOriginY, viewport.X, SurfaceStripHeight, scrollX * 0.72f, 0f);
		UpdateDecorations(area, surfaceY, sandY);
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

	private void UpdateDecorations(Rect2 area, float waterSurfaceY, float sandY)
	{
		if (!EnableDecorations || decorationEntries.Count == 0)
		{
			if (decorationRoot != null)
				decorationRoot.Visible = false;
			return;
		}

		decorationRoot.Visible = true;
		int baseCycle = Mathf.FloorToInt(area.Position.X / DecorationRepeatWidth);
		float waterDepth = Mathf.Max(260f, sandY - waterSurfaceY - 180f);

		foreach (DecorationEntry entry in decorationEntries)
		{
			int chunk = baseCycle + entry.CycleOffset;
			DecorationSpec spec = entry.Spec;
			float chanceRoll = Hash01(chunk * 97 + entry.SpecIndex * 311);

			if (chanceRoll > spec.Chance)
			{
				entry.Sprite.Visible = false;
				continue;
			}

			entry.Sprite.Visible = true;
			float jitter = (Hash01(chunk * 131 + entry.SpecIndex * 47) - 0.5f) * spec.JitterX;
			float x = chunk * DecorationRepeatWidth + spec.LocalX + jitter;

			if (spec.Anchor == DecorationAnchor.Bubble)
			{
				float phase = Hash01(chunk * 173 + entry.SpecIndex * 83) * waterDepth;
				float travel = Mathf.PosMod(time * spec.BubbleSpeed + phase, waterDepth);
				float wobble = Mathf.Sin(time * 1.1f + chunk + entry.SpecIndex) * 12f;
				float y = sandY - 95f - travel;
				entry.Sprite.Position = new Vector2(Mathf.Round(x + wobble), Mathf.Round(y));
				continue;
			}

			float surfaceY = GetSandSurfaceY(x, sandY);
			float visualHeight = Mathf.Max(1f, entry.TextureHeight);
			float visibleBottomFromCenter = (visualHeight * 0.5f - entry.VisibleBottomInset) * spec.Scale;
			float depthNudge = spec.Anchor == DecorationAnchor.SandBack ? 14f : 0f;
			float yOnGround = surfaceY + spec.GroundOffset + depthNudge - visibleBottomFromCenter;
			entry.Sprite.Position = new Vector2(Mathf.Round(x), Mathf.Round(yOnGround));
		}
	}

	public float GetSandSurfaceYAt(float x)
	{
		return GetSandSurfaceY(x, SandY);
	}

	private float GetSandSurfaceY(float x, float sandBaseY)
	{
		if (sandEdgeTopPixels != null && sandEdgeTopPixels.Length > 0)
		{
			int textureX = Mathf.PosMod(Mathf.FloorToInt(x), sandEdgeTopPixels.Length);
			return sandBaseY - SandEdgeOriginY + sandEdgeTopPixels[textureX];
		}

		float t = x / DecorationRepeatWidth * Mathf.Tau;
		return sandBaseY +
			Mathf.Sin(t + 0.4f) * SandWaveA +
			Mathf.Sin(t * 2f + 2.1f) * SandWaveB +
			Mathf.Sin(t * 4f + 1.2f) * SandWaveC;
	}

	public static float GetFallbackSandSurfaceY(float x, float sandBaseY)
	{
		float t = x / DecorationRepeatWidth * Mathf.Tau;
		return sandBaseY +
			Mathf.Sin(t + 0.4f) * SandWaveA +
			Mathf.Sin(t * 2f + 2.1f) * SandWaveB +
			Mathf.Sin(t * 4f + 1.2f) * SandWaveC;
	}

	private float Hash01(int seed)
	{
		uint value = (uint)seed;
		value ^= value >> 16;
		value *= 0x7feb352dU;
		value ^= value >> 15;
		value *= 0x846ca68bU;
		value ^= value >> 16;
		return (value & 0x00ffffff) / 16777215f;
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

	private Texture2D LoadFirstTexture(string[] paths)
	{
		string path = GetFirstExistingPath(paths);
		Texture2D texture = path == "" ? null : ResourceLoader.Load<Texture2D>(path);

		if (texture == null)
			GD.PushWarning($"Level background texture missing: {string.Join(", ", paths)}");

		return texture;
	}

	private Texture2D LoadFirstTextureWithCheckerTransparency(string[] paths, int preserveRadius)
	{
		string path = GetFirstExistingPath(paths);

		if (path == "")
			return LoadFirstTexture(paths);

		string cacheKey = $"{path}:{preserveRadius}";
		if (keyedTextureCache.TryGetValue(cacheKey, out Texture2D cachedTexture))
			return cachedTexture;

		Texture2D sourceTexture = ResourceLoader.Load<Texture2D>(path);
		Image image = sourceTexture?.GetImage();

		if (image == null || image.IsEmpty())
			return sourceTexture;

		if (image.GetFormat() != Image.Format.Rgba8)
			image.Convert(Image.Format.Rgba8);

		RemoveCheckerBackground(image, preserveRadius);
		Texture2D texture = ImageTexture.CreateFromImage(image);
		keyedTextureCache[cacheKey] = texture;
		return texture;
	}

	private string GetFirstExistingPath(string[] paths)
	{
		foreach (string path in paths)
		{
			if (ResourceLoader.Exists(path))
				return path;
		}

		return "";
	}

	private void RemoveCheckerBackground(Image image, int preserveRadius)
	{
		int width = image.GetWidth();
		int height = image.GetHeight();

		if (HasUsefulAlpha(image, width, height))
			return;

		if (preserveRadius < 0)
		{
			RemoveEdgeConnectedCheckerBackground(image, width, height);
			return;
		}

		bool[] objectMask = new bool[width * height];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
				objectMask[y * width + x] = !IsCheckerPixel(image.GetPixel(x, y));
		}

		bool[] preservedCheckerPixels = DilateMask(objectMask, width, height, preserveRadius);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				Color color = image.GetPixel(x, y);
				color.A = IsCheckerPixel(color) && !preservedCheckerPixels[index] ? 0f : 1f;
				image.SetPixel(x, y, color);
			}
		}
	}

	private bool HasUsefulAlpha(Image image, int width, int height)
	{
		for (int y = 0; y < height; y += 6)
		{
			for (int x = 0; x < width; x += 6)
			{
				if (image.GetPixel(x, y).A < 0.92f)
					return true;
			}
		}

		return false;
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

	private void QueueCheckerPixel(Image image, bool[] backgroundMask, Queue<int> pending, int x, int y, int width)
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

		return maxChannel > 0.82f && saturation < 0.075f;
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
