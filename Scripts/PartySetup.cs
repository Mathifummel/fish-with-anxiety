using Godot;

public partial class PartySetup : Control
{
	private enum SetupStep
	{
		Mode,
		Roster
	}

	private OceanMapBackground backgroundMap;
	private VBoxContainer content;
	private Label titleLabel;
	private Label infoLabel;
	private SetupStep step = SetupStep.Mode;
	private ScoreManager scoreManager;

	public override void _Ready()
	{
		GameAudio.EnsureMenuMusic(this);
		scoreManager = GetNode<ScoreManager>("/root/ScoreManager");
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildBackground();
		BuildPanel();
		ShowModeStep();
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!GameUi.IsCancelPressed(inputEvent))
			return;

		GetViewport().SetInputAsHandled();

		if (step == SetupStep.Mode)
			SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
		else
			ShowModeStep();
	}

	private void BuildBackground()
	{
		backgroundMap = new OceanMapBackground();
		backgroundMap.ConfigureForScreen();
		AddChild(backgroundMap);
		MoveChild(backgroundMap, 0);

		ColorRect tint = new ColorRect();
		tint.Color = new Color(0.72f, 0.94f, 1f, 0.16f);
		tint.MouseFilter = MouseFilterEnum.Ignore;
		tint.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(tint);
	}

	private void BuildPanel()
	{
		CenterContainer center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		PanelContainer panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(1040f, 650f);
		panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		center.AddChild(panel);

		MarginContainer margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 34);
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_right", 34);
		margin.AddThemeConstantOverride("margin_bottom", 28);
		panel.AddChild(margin);

		content = new VBoxContainer();
		content.Alignment = BoxContainer.AlignmentMode.Center;
		content.AddThemeConstantOverride("separation", 12);
		margin.AddChild(content);

		titleLabel = CreateLabel("", 34, GameUi.DarkText);
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(titleLabel);

		infoLabel = CreateLabel("", 16, new Color(0.05f, 0.22f, 0.34f, 0.82f));
		infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
		infoLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		content.AddChild(infoLabel);
	}

	private void ShowModeStep()
	{
		step = SetupStep.Mode;
		ClearDynamicRows();
		titleLabel.Text = "2-Spieler Modus";
		infoLabel.Text = "Fangen ist spielbar. Die anderen Modi bleiben sichtbar, sind aber noch Work in Progress.";

		Button catchButton = CreateButton("Fangen starten");
		catchButton.Pressed += ShowRosterStep;
		content.AddChild(catchButton);

		AddWipButton("Party-Modus");
		AddWipButton("Muenzen sammeln");
		AddWipButton("Raeuber und Gendarm");
		AddWipButton("Betrunkener Run");

		Button backButton = CreateButton("Zurueck");
		backButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
		content.AddChild(backButton);

		AddControllerHints();
		GameUi.FocusFirstButton(this);
	}

	private void AddWipButton(string title)
	{
		Button button = CreateButton($"{title}  -  WIP");
		button.Disabled = true;
		button.TooltipText = "Work in Progress";
		content.AddChild(button);
	}

	private void ShowRosterStep()
	{
		step = SetupStep.Roster;
		PartyState.SelectedGame = PartyState.GameSelection.Catch;
		PartyState.Rounds = 1;
		EnsureSelectedSkinIsOwned();
		ClearDynamicRows();

		titleLabel.Text = "Roster";
		infoLabel.Text = "Links waehlt Spieler 1 einen freigeschalteten Fisch. Rechts waehlt Spieler 2 den Jaeger.";

		HBoxContainer columns = new HBoxContainer();
		columns.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		columns.SizeFlagsVertical = SizeFlags.ExpandFill;
		columns.AddThemeConstantOverride("separation", 18);
		content.AddChild(columns);

		columns.AddChild(CreatePlayerRoster());
		columns.AddChild(CreateOpponentRoster());

		HBoxContainer actions = new HBoxContainer();
		actions.Alignment = BoxContainer.AlignmentMode.Center;
		actions.AddThemeConstantOverride("separation", 14);
		content.AddChild(actions);

		Button startButton = CreateButton("Fangen starten");
		startButton.CustomMinimumSize = new Vector2(240f, 46f);
		startButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/PartyMode.tscn", 0.32f);
		actions.AddChild(startButton);

		Button backButton = CreateButton("Zurueck");
		backButton.CustomMinimumSize = new Vector2(190f, 46f);
		backButton.Pressed += ShowModeStep;
		actions.AddChild(backButton);

		AddControllerHints();
		GameUi.FocusFirstButton(this);
	}

	private Control CreatePlayerRoster()
	{
		VBoxContainer column = new VBoxContainer();
		column.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		column.SizeFlagsVertical = SizeFlags.ExpandFill;
		column.AddThemeConstantOverride("separation", 8);

		Label label = CreateLabel("Spielerfische", 21, GameUi.AccentText);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(label);

		ScrollContainer scroll = new ScrollContainer();
		scroll.CustomMinimumSize = new Vector2(610f, 380f);
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		scroll.FollowFocus = true;
		column.AddChild(scroll);

		GridContainer grid = new GridContainer();
		grid.Columns = 3;
		grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		grid.AddThemeConstantOverride("h_separation", 10);
		grid.AddThemeConstantOverride("v_separation", 10);
		scroll.AddChild(grid);

		foreach (SkinDefinition skin in ShopCatalog.Skins)
		{
			if (!scoreManager.IsSkinOwned(skin.Id))
				continue;

			grid.AddChild(CreatePlayerSkinCard(skin));
		}

		return column;
	}

	private Control CreatePlayerSkinCard(SkinDefinition skin)
	{
		PanelContainer card = new PanelContainer();
		card.CustomMinimumSize = new Vector2(188f, 190f);
		card.AddThemeStyleboxOverride("panel", CreateCardStyle(PartyState.PlayerSkinId == skin.Id));

		VBoxContainer layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 5);
		card.AddChild(layout);

		layout.AddChild(CreateSkinPreview(skin.Frame1Path, new Vector2(148f, 99f)));

		Label name = CreateLabel(skin.DisplayName, 12, GameUi.LightText);
		name.HorizontalAlignment = HorizontalAlignment.Center;
		name.ClipText = true;
		layout.AddChild(name);

		Button select = CreateButton(PartyState.PlayerSkinId == skin.Id ? "Aktiv" : "Waehlen");
		select.CustomMinimumSize = new Vector2(0f, 34f);
		select.Disabled = PartyState.PlayerSkinId == skin.Id;
		select.Pressed += () =>
		{
			PartyState.PlayerSkinId = skin.Id;
			ShowRosterStep();
		};
		layout.AddChild(select);

		return card;
	}

	private TextureRect CreateSkinPreview(string texturePath, Vector2 size)
	{
		TextureRect preview = new TextureRect();
		preview.Texture = ResourceLoader.Load<Texture2D>(texturePath);
		preview.CustomMinimumSize = size;
		preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		return preview;
	}

	private Control CreateOpponentRoster()
	{
		VBoxContainer column = new VBoxContainer();
		column.CustomMinimumSize = new Vector2(310f, 0f);
		column.SizeFlagsVertical = SizeFlags.ExpandFill;
		column.AddThemeConstantOverride("separation", 8);

		Label label = CreateLabel("Jaeger", 21, GameUi.AccentText);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(label);

		column.AddChild(CreateOpponentCard(
			PartyState.OpponentSelection.EnemyOne,
			"Gegnerfisch 1",
			"res://Assets/Gegnerfischframe1.png"
		));
		column.AddChild(CreateOpponentCard(
			PartyState.OpponentSelection.EnemyTwo,
			"Gegnerfisch 2",
			"res://Assets/Gegnerfisch2frame1.png"
		));
		column.AddChild(CreateOpponentCard(
			PartyState.OpponentSelection.Jellyfish,
			"Qualle",
			"res://Assets/Qualle.png"
		));

		return column;
	}

	private Control CreateOpponentCard(PartyState.OpponentSelection opponent, string title, string texturePath)
	{
		PanelContainer card = new PanelContainer();
		bool selected = PartyState.Opponent == opponent;
		card.AddThemeStyleboxOverride("panel", CreateCardStyle(selected));

		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		card.AddChild(row);

		TextureRect preview = new TextureRect();
		preview.Texture = ResourceLoader.Load<Texture2D>(texturePath);
		preview.CustomMinimumSize = new Vector2(78f, 66f);
		preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		row.AddChild(preview);

		VBoxContainer copy = new VBoxContainer();
		copy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		copy.AddThemeConstantOverride("separation", 6);
		row.AddChild(copy);

		Label name = CreateLabel(title, 15, GameUi.LightText);
		copy.AddChild(name);

		Button select = CreateButton(selected ? "Aktiv" : "Waehlen");
		select.CustomMinimumSize = new Vector2(0f, 34f);
		select.Disabled = selected;
		select.Pressed += () =>
		{
			PartyState.Opponent = opponent;
			PartyState.OpponentSkin = opponent == PartyState.OpponentSelection.EnemyTwo
				? NPCFish.EnemySkin.Gegnerfisch2
				: NPCFish.EnemySkin.Gegnerfisch;
			ShowRosterStep();
		};
		copy.AddChild(select);

		return card;
	}

	private void EnsureSelectedSkinIsOwned()
	{
		if (scoreManager == null || scoreManager.IsSkinOwned(PartyState.PlayerSkinId))
			return;

		PartyState.PlayerSkinId = ShopCatalog.DefaultSkinId;
	}

	private void ClearDynamicRows()
	{
		if (content == null)
			return;

		for (int i = content.GetChildCount() - 1; i >= 2; i--)
			content.GetChild(i).QueueFree();
	}

	private void AddControllerHints()
	{
		ControllerHintBar controllerHints = GameUi.CreateControllerHintBar(GameUi.ControllerHintMode.BackOnly);
		controllerHints.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		content.AddChild(controllerHints);
	}

	private Label CreateLabel(string text, int fontSize, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		GameUi.ApplyLabel(label, fontSize, color);
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(230f, 46f);
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameUi.ApplyButton(button, 16);
		return button;
	}

	private StyleBoxFlat CreateCardStyle(bool selected)
	{
		StyleBoxFlat style = GameUi.CreateButtonStyle(
			selected ? new Color(0.03f, 0.24f, 0.2f, 0.78f) : new Color(0.02f, 0.14f, 0.2f, 0.64f),
			selected ? new Color(0.76f, 1f, 0.66f, 0.78f) : new Color(0.72f, 0.96f, 1f, 0.42f)
		);
		style.ContentMarginLeft = 10;
		style.ContentMarginTop = 8;
		style.ContentMarginRight = 10;
		style.ContentMarginBottom = 8;
		return style;
	}
}
