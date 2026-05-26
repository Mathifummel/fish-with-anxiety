using Godot;

public partial class Leaderboard : Control
{
	private const int MaxRows = 10;
	private const int BoardWidth = 620;

	private VBoxContainer RowsContainer;
	private Label SummaryLabel;
	private VideoStreamPlayer backgroundVideo;
	private float visualTime = 0f;

	public override void _Ready()
	{
		BuildLayout();
		LoadLeaderboard();
		SceneTransition.FadeIn(GetTree(), 0.28f);
	}

	public override void _Process(double delta)
	{
		visualTime += (float)delta;
		AnimateBackground();
	}

	private void BuildLayout()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		AddVideoBackground();
		AddTintOverlay();

		MarginContainer pageMargin = new MarginContainer();
		pageMargin.SetAnchorsPreset(LayoutPreset.FullRect);
		pageMargin.AddThemeConstantOverride("margin_left", 36);
		pageMargin.AddThemeConstantOverride("margin_top", 24);
		pageMargin.AddThemeConstantOverride("margin_right", 36);
		pageMargin.AddThemeConstantOverride("margin_bottom", 24);
		AddChild(pageMargin);

		VBoxContainer page = new VBoxContainer();
		page.Alignment = BoxContainer.AlignmentMode.Center;
		page.AddThemeConstantOverride("separation", 16);
		page.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		page.SizeFlagsVertical = SizeFlags.ExpandFill;
		pageMargin.AddChild(page);

		TextureRect logo = new TextureRect();
		logo.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Logo.png");
		logo.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		logo.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		logo.CustomMinimumSize = new Vector2(420, 72);
		logo.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		logo.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		page.AddChild(logo);

		CenterContainer boardCenter = new CenterContainer();
		boardCenter.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		boardCenter.SizeFlagsVertical = SizeFlags.ExpandFill;
		boardCenter.CustomMinimumSize = new Vector2(BoardWidth, 0);
		page.AddChild(boardCenter);

		PanelContainer board = new PanelContainer();
		board.CustomMinimumSize = new Vector2(BoardWidth, 420);
		board.AddThemeStyleboxOverride("panel", CreateBoardStyle());
		board.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		board.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		boardCenter.AddChild(board);

		MarginContainer boardMargin = new MarginContainer();
		boardMargin.AddThemeConstantOverride("margin_left", 30);
		boardMargin.AddThemeConstantOverride("margin_top", 24);
		boardMargin.AddThemeConstantOverride("margin_right", 30);
		boardMargin.AddThemeConstantOverride("margin_bottom", 24);
		board.AddChild(boardMargin);

		VBoxContainer content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 14);
		content.SizeFlagsHorizontal = SizeFlags.Fill;
		boardMargin.AddChild(content);

		Label title = CreateLabel("Bestenliste", 42, new Color(0.92f, 0.98f, 1f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

		SummaryLabel = CreateLabel("", 16, new Color(0.62f, 0.78f, 0.84f));
		SummaryLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(SummaryLabel);

		content.AddChild(CreateHeaderRow());

		ScrollContainer scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		content.AddChild(scroll);

		RowsContainer = new VBoxContainer();
		RowsContainer.AddThemeConstantOverride("separation", 7);
		RowsContainer.SizeFlagsHorizontal = SizeFlags.Fill;
		RowsContainer.CustomMinimumSize = new Vector2(BoardWidth - 60, 0);
		scroll.AddChild(RowsContainer);

		HBoxContainer buttons = new HBoxContainer();
		buttons.Alignment = BoxContainer.AlignmentMode.Center;
		buttons.AddThemeConstantOverride("separation", 12);
		content.AddChild(buttons);

		Button backButton = CreateButton("Zurück");
		backButton.Pressed += OnBackButtonPressed;
		buttons.AddChild(backButton);

		Button playButton = CreateButton("Nochmal spielen");
		playButton.Pressed += OnPlayButtonPressed;
		buttons.AddChild(playButton);
	}

	private void AddVideoBackground()
	{
		backgroundVideo = new VideoStreamPlayer();
		backgroundVideo.Stream = ResourceLoader.Load<VideoStream>("res://Assets/underwater.ogv");
		backgroundVideo.SpeedScale = 2.57f;
		backgroundVideo.Autoplay = true;
		backgroundVideo.Expand = true;
		backgroundVideo.Loop = true;
		backgroundVideo.SetAnchorsPreset(LayoutPreset.FullRect);
		backgroundVideo.PivotOffset = GetViewportRect().Size * 0.5f;
		AddChild(backgroundVideo);
	}

	private void AnimateBackground()
	{
		if (backgroundVideo == null)
			return;

		float driftX = Mathf.Sin(visualTime * 0.11f) * 24f;
		float driftY = Mathf.Cos(visualTime * 0.09f) * 16f;
		float zoom = 1.055f + Mathf.Sin(visualTime * 0.075f) * 0.018f;

		backgroundVideo.OffsetLeft = -42f + driftX;
		backgroundVideo.OffsetTop = -32f + driftY;
		backgroundVideo.OffsetRight = 42f + driftX;
		backgroundVideo.OffsetBottom = 32f + driftY;
		backgroundVideo.Scale = new Vector2(zoom, zoom);
	}

	private void AddTintOverlay()
	{
		ColorRect overlay = new ColorRect();
		overlay.Color = new Color(0.01f, 0.06f, 0.09f, 0.28f);
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(overlay);
	}

	private PanelContainer CreateHeaderRow()
	{
		PanelContainer panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(BoardWidth - 60, 38);
		panel.SizeFlagsHorizontal = SizeFlags.Fill;
		panel.AddThemeStyleboxOverride("panel", CreateRowStyle(new Color(0.08f, 0.22f, 0.28f, 0.95f), 1f));

		MarginContainer margin = CreateRowMargin();
		panel.AddChild(margin);

		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		margin.AddChild(row);

		row.AddChild(CreateCell("Rang", 0.55f, HorizontalAlignment.Left, true));
		row.AddChild(CreateCell("Name", 2.0f, HorizontalAlignment.Left, true));
		row.AddChild(CreateCell("Punkte", 1.0f, HorizontalAlignment.Right, true));

		return panel;
	}

	private void LoadLeaderboard()
	{
		var sm = GetNode<ScoreManager>("/root/ScoreManager");
		var scores = sm.LoadScores();

		if (scores.Count == 0)
		{
			SummaryLabel.Text = "Noch keine Scores gespeichert";
			RowsContainer.AddChild(CreateEmptyState());
			return;
		}

		int rows = Mathf.Min(scores.Count, MaxRows);
		int bestScore = 0;

		for (int i = 0; i < rows; i++)
		{
			var dict = scores[i].AsGodotDictionary();
			string name = FormatName(dict["name"].ToString());
			int score = (int)dict["score"];

			if (i == 0)
				bestScore = score;

			RowsContainer.AddChild(CreateScoreRow(i + 1, name, score));
		}

		SummaryLabel.Text = $"Top {rows} Spieler  |  Bester Score: {bestScore}";
	}

	private PanelContainer CreateScoreRow(int rank, string name, int score)
	{
		Color bgColor = rank == 1
			? new Color(0.12f, 0.36f, 0.42f, 0.92f)
			: rank % 2 == 0
				? new Color(0.05f, 0.15f, 0.2f, 0.82f)
				: new Color(0.04f, 0.12f, 0.17f, 0.82f);

		PanelContainer panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(BoardWidth - 60, 42);
		panel.SizeFlagsHorizontal = SizeFlags.Fill;
		panel.AddThemeStyleboxOverride("panel", CreateRowStyle(bgColor, rank == 1 ? 1.2f : 0.35f));

		MarginContainer margin = CreateRowMargin();
		panel.AddChild(margin);

		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		margin.AddChild(row);

		row.AddChild(CreateCell($"#{rank}", 0.55f, HorizontalAlignment.Left, rank <= 3));
		row.AddChild(CreateCell(name, 2.0f, HorizontalAlignment.Left, rank <= 3));
		row.AddChild(CreateCell(score.ToString(), 1.0f, HorizontalAlignment.Right, rank <= 3));

		return panel;
	}

	private Label CreateEmptyState()
	{
		Label label = CreateLabel("Spiele eine Runde, speichere deinen Namen und hier erscheint dein Score.", 18, new Color(0.7f, 0.84f, 0.88f));
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.SizeFlagsVertical = SizeFlags.ExpandFill;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		return label;
	}

	private Label CreateCell(string text, float ratio, HorizontalAlignment alignment, bool bold)
	{
		Label label = CreateLabel(text, bold ? 19 : 18, bold ? new Color(0.94f, 1f, 0.98f) : new Color(0.8f, 0.92f, 0.95f));
		label.HorizontalAlignment = alignment;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		label.SizeFlagsStretchRatio = ratio;
		return label;
	}

	private Label CreateLabel(string text, int fontSize, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(170, 42);
		button.AddThemeFontSizeOverride("font_size", 18);
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.03f, 0.16f, 0.22f, 0.88f)));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.07f, 0.27f, 0.34f, 0.94f)));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.02f, 0.11f, 0.16f, 0.96f)));
		button.AddThemeColorOverride("font_color", new Color(0.9f, 0.98f, 1f));
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 1f));
		return button;
	}

	private MarginContainer CreateRowMargin()
	{
		MarginContainer margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		return margin;
	}

	private StyleBoxFlat CreateBoardStyle()
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(0.02f, 0.1f, 0.15f, 0.76f);
		style.BorderColor = new Color(0.45f, 0.82f, 0.9f, 0.48f);
		style.BorderWidthLeft = 2;
		style.BorderWidthTop = 2;
		style.BorderWidthRight = 2;
		style.BorderWidthBottom = 2;
		style.CornerRadiusTopLeft = 8;
		style.CornerRadiusTopRight = 8;
		style.CornerRadiusBottomLeft = 8;
		style.CornerRadiusBottomRight = 8;
		return style;
	}

	private StyleBoxFlat CreateButtonStyle(Color color)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = color;
		style.BorderColor = new Color(0.54f, 0.86f, 0.94f, 0.48f);
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 7;
		style.CornerRadiusTopRight = 7;
		style.CornerRadiusBottomLeft = 7;
		style.CornerRadiusBottomRight = 7;
		return style;
	}

	private StyleBoxFlat CreateRowStyle(Color color, float borderAlpha)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = color;
		style.BorderColor = new Color(0.5f, 0.82f, 0.9f, borderAlpha);
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 6;
		style.CornerRadiusTopRight = 6;
		style.CornerRadiusBottomLeft = 6;
		style.CornerRadiusBottomRight = 6;
		return style;
	}

	private string FormatName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return "Player";

		name = name.Trim();

		if (name.Length <= 18)
			return name;

		return name.Substring(0, 15) + "...";
	}

	private void OnBackButtonPressed()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.32f);
	}

	private void OnPlayButtonPressed()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn", 0.32f);
	}
}
