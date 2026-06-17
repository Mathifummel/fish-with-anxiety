using Godot;

public partial class MissionsPage : Control
{
	private Label coinLabel;
	private Label statusLabel;
	private VBoxContainer missionList;
	private ScoreManager scoreManager;

	public override void _Ready()
	{
		GameAudio.EnsureMenuMusic(this);
		scoreManager = GetNode<ScoreManager>("/root/ScoreManager");
		BuildUi();
		RefreshMissions();
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!GameUi.IsCancelPressed(inputEvent))
			return;

		GetViewport().SetInputAsHandled();
		GoBack();
	}

	private void BuildUi()
	{
		OceanMapBackground background = new OceanMapBackground();
		background.ConfigureForScreen();
		AddChild(background);
		MoveChild(background, 0);

		ColorRect overlay = new ColorRect();
		overlay.Color = new Color(0.01f, 0.05f, 0.08f, 0.3f);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(overlay);

		Panel panel = new Panel();
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -500f;
		panel.OffsetTop = -318f;
		panel.OffsetRight = 500f;
		panel.OffsetBottom = 318f;
		panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		AddChild(panel);

		VBoxContainer layout = new VBoxContainer();
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		layout.OffsetLeft = 28f;
		layout.OffsetTop = 24f;
		layout.OffsetRight = -28f;
		layout.OffsetBottom = -24f;
		layout.AddThemeConstantOverride("separation", 10);
		panel.AddChild(layout);

		HBoxContainer header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 12);
		layout.AddChild(header);

		Label title = CreateLabel("Missionen", 30, GameUi.LightText);
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);

		coinLabel = CreateLabel("", 22, new Color(1f, 0.9f, 0.34f));
		coinLabel.HorizontalAlignment = HorizontalAlignment.Right;
		header.AddChild(coinLabel);

		ScrollContainer scroll = new ScrollContainer();
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		layout.AddChild(scroll);

		missionList = new VBoxContainer();
		missionList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		missionList.AddThemeConstantOverride("separation", 8);
		scroll.AddChild(missionList);

		statusLabel = CreateLabel("", 16, new Color(0.72f, 1f, 0.84f));
		statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(statusLabel);

		HBoxContainer footer = new HBoxContainer();
		footer.Alignment = BoxContainer.AlignmentMode.Center;
		layout.AddChild(footer);

		Button backButton = CreateMenuButton("Zurueck");
		backButton.CustomMinimumSize = new Vector2(240f, 42f);
		backButton.Pressed += GoBack;
		footer.AddChild(backButton);

		ControllerHintBar controllerHints = GameUi.CreateControllerHintBar(GameUi.ControllerHintMode.BackOnly);
		controllerHints.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		layout.AddChild(controllerHints);
	}

	private void RefreshMissions()
	{
		coinLabel.Text = $"Muenzen: {scoreManager.TotalCoins}";
		ClearChildren(missionList);

		foreach (MissionDefinition mission in MissionCatalog.Missions)
			missionList.AddChild(CreateMissionRow(mission));
	}

	private Control CreateMissionRow(MissionDefinition mission)
	{
		PanelContainer panel = new PanelContainer();
		panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panel.AddThemeStyleboxOverride("panel", CreateRowStyle(scoreManager.CanClaimMission(mission)));

		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		panel.AddChild(row);

		VBoxContainer text = new VBoxContainer();
		text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		text.AddThemeConstantOverride("separation", 2);
		row.AddChild(text);

		Label title = CreateLabel(mission.Title, 17, GameUi.LightText);
		text.AddChild(title);

		Label description = CreateLabel(mission.Description, 13, new Color(0.72f, 0.9f, 0.96f));
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(description);

		Label progress = CreateLabel(GetProgressText(mission), 13, GameUi.AccentText);
		text.AddChild(progress);

		Button claim = CreateMenuButton(GetClaimText(mission));
		claim.CustomMinimumSize = new Vector2(116f, 42f);
		claim.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		claim.Disabled = !scoreManager.CanClaimMission(mission);
		claim.Pressed += () => ClaimMission(mission);
		row.AddChild(claim);

		return panel;
	}

	private string GetProgressText(MissionDefinition mission)
	{
		int progress = Mathf.Min(scoreManager.GetMissionProgress(mission), mission.Target);
		string suffix = mission.UsesSeconds ? "s" : "";
		return $"{progress}{suffix} / {mission.Target}{suffix}  (+{MissionCatalog.Reward} M)";
	}

	private string GetClaimText(MissionDefinition mission)
	{
		if (scoreManager.IsMissionClaimed(mission.Id))
			return "Erledigt";

		return scoreManager.CanClaimMission(mission) ? "Abholen" : "Offen";
	}

	private void ClaimMission(MissionDefinition mission)
	{
		if (!scoreManager.TryClaimMission(mission.Id))
			return;

		statusLabel.Text = $"+{MissionCatalog.Reward} Muenzen";
		GameAudio.PlayOneShot(this, GameAudio.UiButtonPath, -8f);
		RefreshMissions();
	}

	private Label CreateLabel(string text, int size, Color color)
	{
		Label label = new Label();
		label.Text = text;
		GameUi.ApplyLabel(label, size, color);
		return label;
	}

	private Button CreateMenuButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FocusMode = FocusModeEnum.All;
		GameUi.ApplyButton(button, 13);
		return button;
	}

	private StyleBoxFlat CreateRowStyle(bool ready)
	{
		StyleBoxFlat style = GameUi.CreateButtonStyle(
			ready ? new Color(0.04f, 0.24f, 0.2f, 0.76f) : new Color(0.02f, 0.14f, 0.2f, 0.64f),
			ready ? new Color(0.76f, 1f, 0.66f, 0.72f) : new Color(0.72f, 0.96f, 1f, 0.38f)
		);
		style.ContentMarginLeft = 14;
		style.ContentMarginTop = 8;
		style.ContentMarginRight = 14;
		style.ContentMarginBottom = 8;
		return style;
	}

	private void ClearChildren(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void GoBack()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
	}
}
