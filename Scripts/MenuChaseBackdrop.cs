using Godot;

public partial class MenuChaseBackdrop : Node2D
{
	private const int EnemyCount = 13;
	private const float RouteStartPadding = 260f;
	private const float RouteEndPadding = 720f;
	private const float RouteSpeed = 0.055f;
	private readonly Sprite2D[] enemies = new Sprite2D[EnemyCount];
	private readonly Sprite2D[] bubbles = new Sprite2D[32];
	private readonly Vector2[] enemyOffsets = new Vector2[EnemyCount];
	private readonly float[] enemyPhase = new float[EnemyCount];
	private readonly float[] bubblePhase = new float[32];
	private readonly RandomNumberGenerator rng = new RandomNumberGenerator();

	private OceanMapBackground backgroundMap;
	private Sprite2D playerFish;
	private Texture2D[] playerFrames;
	private Texture2D[][] enemyFrames;
	private float time = 0f;

	public override void _Ready()
	{
		rng.Randomize();
		BuildBackground();
		LoadFrames();
		BuildFish();
		BuildBubbles();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		time += dt;

		AnimateFish();
		AnimateBubbles();
	}

	private void BuildBackground()
	{
		CanvasLayer layer = new CanvasLayer();
		layer.Layer = -20;
		AddChild(layer);

		backgroundMap = new OceanMapBackground();
		backgroundMap.ConfigureForScreen();
		layer.AddChild(backgroundMap);

		ColorRect tint = new ColorRect();
		tint.Color = new Color(0.02f, 0.08f, 0.12f, 0.16f);
		tint.MouseFilter = Control.MouseFilterEnum.Ignore;
		tint.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(tint);
	}

	private void LoadFrames()
	{
		playerFrames = new Texture2D[]
		{
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_1 1.png"),
			ResourceLoader.Load<Texture2D>("res://Assets/Fisch_2 1.png")
		};

		enemyFrames = new Texture2D[][]
		{
			new Texture2D[]
			{
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe1.png"),
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe2.png"),
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfischframe3.png")
			},
			new Texture2D[]
			{
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame1.png"),
				ResourceLoader.Load<Texture2D>("res://Assets/Gegnerfisch2frame2.png")
			}
		};
	}

	private void BuildFish()
	{
		playerFish = new Sprite2D();
		playerFish.Scale = new Vector2(1.3f, 1.3f);
		playerFish.ZIndex = 6;
		AddChild(playerFish);

		for (int i = 0; i < EnemyCount; i++)
		{
			Sprite2D enemy = new Sprite2D();
			enemy.Scale = new Vector2(1.06f + (i % 3) * 0.08f, 1.06f + (i % 3) * 0.08f);
			enemy.Modulate = new Color(1f, 1f, 1f, 0.7f + (i % 4) * 0.06f);
			enemy.ZIndex = 4 - i % 3;
			enemy.FlipH = true;
			AddChild(enemy);
			enemies[i] = enemy;

			float column = i / 3f;
			float lane = (i % 3) - 1f;
			enemyOffsets[i] = new Vector2(-190f - column * 62f, lane * 66f + rng.RandfRange(-28f, 28f));
			enemyPhase[i] = rng.RandfRange(0f, Mathf.Tau);
		}
	}

	private void BuildBubbles()
	{
		for (int i = 0; i < bubbles.Length; i++)
		{
			Sprite2D bubble = new Sprite2D();
			bubble.Texture = CreateBubbleTexture();
			float scale = rng.RandfRange(0.28f, 0.9f);
			bubble.Scale = new Vector2(scale, scale);
			bubble.Modulate = new Color(0.78f, 0.96f, 1f, rng.RandfRange(0.14f, 0.34f));
			bubble.ZIndex = -2;
			AddChild(bubble);
			bubbles[i] = bubble;
			bubblePhase[i] = rng.RandfRange(0f, 1f);
		}
	}

	private Texture2D CreateBubbleTexture()
	{
		Image image = Image.CreateEmpty(18, 18, false, Image.Format.Rgba8);
		Vector2 center = new Vector2(8.5f, 8.5f);

		for (int y = 0; y < 18; y++)
		{
			for (int x = 0; x < 18; x++)
			{
				float distance = new Vector2(x, y).DistanceTo(center);
				float ring = Mathf.Abs(distance - 7.2f);
				float alpha = ring < 1.3f ? 0.62f * (1f - ring / 1.3f) : 0f;
				if (distance < 2.2f)
					alpha = Mathf.Max(alpha, 0.35f);
				image.SetPixel(x, y, new Color(0.82f, 0.96f, 1f, alpha));
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private void AnimateFish()
	{
		Vector2 viewport = GetViewportRect().Size;
		if (viewport.X <= 1f || viewport.Y <= 1f)
			viewport = new Vector2(1280f, 720f);

		float progress = Mathf.PosMod(time * RouteSpeed, 1f);
		float x = Mathf.Lerp(-RouteStartPadding, viewport.X + RouteEndPadding, progress);
		float y = viewport.Y * 0.5f + Mathf.Sin(progress * Mathf.Tau * 1.2f + 0.45f) * viewport.Y * 0.18f;
		Vector2 leader = new Vector2(x, y);
		float fade = Mathf.Clamp(Mathf.Sin(progress * Mathf.Pi), 0f, 1f);

		playerFish.Position = leader;
		playerFish.Rotation = Mathf.Sin(time * 5.3f) * 0.09f;
		playerFish.Texture = playerFrames[Mathf.PosMod((int)(time * 8.4f), playerFrames.Length)];
		playerFish.Modulate = new Color(1f, 1f, 1f, 0.55f + fade * 0.45f);

		for (int i = 0; i < enemies.Length; i++)
		{
			Sprite2D enemy = enemies[i];
			Texture2D[] frames = enemyFrames[i % enemyFrames.Length];
			enemy.Texture = frames[Mathf.PosMod((int)(time * (7.5f + i * 0.2f)), frames.Length)];

			float surge = Mathf.Sin(time * 2.2f + enemyPhase[i]) * 22f;
			float wobble = Mathf.Sin(time * 4.1f + enemyPhase[i]) * 18f;
			enemy.Position = leader + enemyOffsets[i] + new Vector2(surge, wobble);
			enemy.Rotation = Mathf.Sin(time * 6.4f + enemyPhase[i]) * 0.14f;
			enemy.Modulate = new Color(1f, 1f, 1f, (0.32f + fade * 0.56f) * (1f - i * 0.018f));
		}
	}

	private void AnimateBubbles()
	{
		Vector2 viewport = GetViewportRect().Size;
		if (viewport.X <= 1f || viewport.Y <= 1f)
			viewport = new Vector2(1280f, 720f);

		for (int i = 0; i < bubbles.Length; i++)
		{
			float t = Mathf.PosMod(bubblePhase[i] + time * (0.03f + i % 5 * 0.006f), 1f);
			float x = (i * 97f + Mathf.Sin(time * 0.4f + i) * 36f) % viewport.X;
			float y = Mathf.Lerp(viewport.Y + 40f, -40f, t);
			bubbles[i].Position = new Vector2(x, y);
		}
	}
}
