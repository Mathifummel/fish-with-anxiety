using Godot;

public partial class Leaderboard : Control
{
	private const int MaxRows = 10;

	private VBoxContainer RowsContainer;
	private Label SummaryLabel;

	public override void _Ready()
	{
		BuildLayout();
		LoadLeaderboard();
	}

	private void BuildLayout()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		ColorRect background = new ColorRect();
		background.Color = new Color(0.02f, 0.08f, 0.12f);
		background.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(background);

		MarginContainer pageMargin = new MarginContainer();
		pageMargin.SetAnchorsPreset(LayoutPreset.FullRect);
		pageMargin.AddThemeConstantOverride("margin_left", 42);
		pageMargin.AddThemeConstantOverride("margin_top", 34);
		pageMargin.AddThemeConstantOverride("margin_right", 42);
		pageMargin.AddThemeConstantOverride("margin_bottom", 34);
		AddChild(pageMargin);

		CenterContainer center = new CenterContainer();
		center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		center.SizeFlagsVertical = SizeFlags.ExpandFill;
		pageMargin.AddChild(center);

		PanelContainer board = new PanelContainer();
		board.CustomMinimumSize = new Vector2(680, 560);
		board.AddThemeStyleboxOverride("panel", CreateBoardStyle());
		center.AddChild(board);

		MarginContainer boardMargin = new MarginContainer();
		boardMargin.AddThemeConstantOverride("margin_left", 34);
		boardMargin.AddThemeConstantOverride("margin_top", 28);
		boardMargin.AddThemeConstantOverride("margin_right", 34);
		boardMargin.AddThemeConstantOverride("margin_bottom", 28);
		board.AddChild(boardMargin);

		VBoxContainer content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 14);
		boardMargin.AddChild(content);

		Label title = CreateLabel("Bestenliste", 42, new Color(0.92f, 0.98f, 1f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(title);

		SummaryLabel = CreateLabel("", 16, new Color(0.62f, 0.78f, 0.84f));
		SummaryLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(SummaryLabel);

		content.AddChild(CreateHeaderRow());

		RowsContainer = new VBoxContainer();
		RowsContainer.AddThemeConstantOverride("separation", 7);
		RowsContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
		content.AddChild(RowsContainer);

		HBoxContainer buttons = new HBoxContainer();
		buttons.Alignment = BoxContainer.AlignmentMode.Center;
		buttons.AddThemeConstantOverride("separation", 12);
		content.AddChild(buttons);

		Button backButton = CreateButton("Zurueck");
		backButton.Pressed += OnBackButtonPressed;
		buttons.AddChild(backButton);

		Button playButton = CreateButton("Nochmal spielen");
		playButton.Pressed += OnPlayButtonPressed;
		buttons.AddChild(playButton);
	}

	private PanelContainer CreateHeaderRow()
	{
		PanelContainer panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(0, 38);
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
		panel.CustomMinimumSize = new Vector2(0, 42);
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
		style.BgColor = new Color(0.03f, 0.12f, 0.17f, 0.94f);
		style.BorderColor = new Color(0.36f, 0.72f, 0.82f, 0.55f);
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
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	private void OnPlayButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}
}
