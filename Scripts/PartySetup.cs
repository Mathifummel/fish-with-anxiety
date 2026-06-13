using Godot;

public partial class PartySetup : Control
{
	private enum SetupStep
	{
		Mode,
		Rounds,
		Fish
	}

	private const int MinRounds = 3;
	private const int MaxRounds = 10;

	private VideoStreamPlayer backgroundVideo;
	private VBoxContainer content;
	private Label titleLabel;
	private Label infoLabel;
	private Label roundsLabel;
	private float visualTime = 0f;
	private SetupStep step = SetupStep.Mode;
	private bool singleMiniGameSelected = false;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildBackground();
		BuildPanel();
		ShowModeStep();
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _Process(double delta)
	{
		visualTime += (float)delta;
		if (backgroundVideo == null)
			return;

		float driftX = Mathf.Sin(visualTime * 0.1f) * 18f;
		float driftY = Mathf.Cos(visualTime * 0.08f) * 12f;
		backgroundVideo.OffsetLeft = -44f + driftX;
		backgroundVideo.OffsetTop = -34f + driftY;
		backgroundVideo.OffsetRight = 44f + driftX;
		backgroundVideo.OffsetBottom = 34f + driftY;
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!GameUi.IsCancelPressed(inputEvent))
			return;

		GetViewport().SetInputAsHandled();

		if (step == SetupStep.Mode)
			SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
		else if (step == SetupStep.Rounds)
			ShowModeStep();
		else if (singleMiniGameSelected)
			ShowModeStep();
		else
			ShowRoundsStep();
	}

	private void BuildBackground()
	{
		backgroundVideo = new VideoStreamPlayer();
		backgroundVideo.Stream = ResourceLoader.Load<VideoStream>("res://Assets/underwater.ogv");
		backgroundVideo.SpeedScale = 2.57f;
		backgroundVideo.Autoplay = true;
		backgroundVideo.Expand = true;
		backgroundVideo.Loop = true;
		backgroundVideo.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(backgroundVideo);

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
		panel.CustomMinimumSize = new Vector2(560f, 520f);
		panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		center.AddChild(panel);

		MarginContainer margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 34);
		margin.AddThemeConstantOverride("margin_top", 30);
		margin.AddThemeConstantOverride("margin_right", 34);
		margin.AddThemeConstantOverride("margin_bottom", 30);
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
		singleMiniGameSelected = false;
		ClearDynamicRows();
		titleLabel.Text = "2-Spieler Modus";
		infoLabel.Text = "Spielt die ganze Party oder startet direkt ein einzelnes Minispiel.";

		Button partyButton = CreateButton("Party-Modus");
		partyButton.Pressed += () =>
		{
			PartyState.SelectedGame = PartyState.GameSelection.Party;
			ShowRoundsStep();
		};
		content.AddChild(partyButton);

		Button catchButton = CreateButton("Fangen");
		catchButton.Pressed += () => SelectSingleMiniGame(PartyState.GameSelection.Catch);
		content.AddChild(catchButton);

		Button coinsButton = CreateButton("Münzen sammeln");
		coinsButton.Pressed += () => SelectSingleMiniGame(PartyState.GameSelection.Coins);
		content.AddChild(coinsButton);

		Button copsButton = CreateButton("Räuber und Gendarm");
		copsButton.Pressed += () => SelectSingleMiniGame(PartyState.GameSelection.Cops);
		content.AddChild(copsButton);

		Button drunkButton = CreateButton("Betrunkener Run");
		drunkButton.Pressed += () => SelectSingleMiniGame(PartyState.GameSelection.DrunkRun);
		content.AddChild(drunkButton);

		Button backButton = CreateButton("Zurück");
		backButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
		content.AddChild(backButton);
		GameUi.FocusFirstButton(this);
	}

	private void ShowRoundsStep()
	{
		step = SetupStep.Rounds;
		singleMiniGameSelected = false;
		PartyState.Rounds = Mathf.Clamp(PartyState.Rounds, MinRounds, MaxRounds);
		ClearDynamicRows();
		titleLabel.Text = "Party-Modus";
		infoLabel.Text = "Eine Party mischt kurze Runden aus Fangen, Münzjagd, Räuber und Gendarm und Betrunkenen Run.";

		roundsLabel = CreateLabel("", 28, GameUi.DarkText);
		roundsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(roundsLabel);
		UpdateRoundsLabel();

		HBoxContainer row = new HBoxContainer();
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddThemeConstantOverride("separation", 12);
		content.AddChild(row);

		Button minus = CreateButton("-");
		minus.CustomMinimumSize = new Vector2(72f, 46f);
		minus.Pressed += () =>
		{
			PartyState.Rounds = Mathf.Max(MinRounds, PartyState.Rounds - 1);
			UpdateRoundsLabel();
		};
		row.AddChild(minus);

		Button plus = CreateButton("+");
		plus.CustomMinimumSize = new Vector2(72f, 46f);
		plus.Pressed += () =>
		{
			PartyState.Rounds = Mathf.Min(MaxRounds, PartyState.Rounds + 1);
			UpdateRoundsLabel();
		};
		row.AddChild(plus);

		Button nextButton = CreateButton("Weiter");
		nextButton.Pressed += ShowFishStep;
		content.AddChild(nextButton);

		Button backButton = CreateButton("Zurück");
		backButton.Pressed += ShowModeStep;
		content.AddChild(backButton);
		GameUi.FocusFirstButton(this);
	}

	private void ShowFishStep()
	{
		step = SetupStep.Fish;
		ClearDynamicRows();
		titleLabel.Text = singleMiniGameSelected ? GetSelectionName(PartyState.SelectedGame) : "Fischwahl";
		infoLabel.Text = "Spieler 1 schwimmt mit WASD. Spieler 2 jagt mit den Pfeiltasten.";

		Label p1 = CreateLabel("Spieler 1: kleiner Fisch", 20, GameUi.DarkText);
		p1.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(p1);

		Label p2 = CreateLabel("Spieler 2: Gegnerfisch", 20, GameUi.DarkText);
		p2.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(p2);

		HBoxContainer row = new HBoxContainer();
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddThemeConstantOverride("separation", 12);
		content.AddChild(row);

		Button enemyOne = CreateButton("Gegnerfisch 1");
		enemyOne.Pressed += () =>
		{
			PartyState.OpponentSkin = NPCFish.EnemySkin.Gegnerfisch;
			ShowFishStep();
		};
		GameUi.ApplyButton(enemyOne, 16, PartyState.OpponentSkin == NPCFish.EnemySkin.Gegnerfisch);
		row.AddChild(enemyOne);

		Button enemyTwo = CreateButton("Gegnerfisch 2");
		enemyTwo.Pressed += () =>
		{
			PartyState.OpponentSkin = NPCFish.EnemySkin.Gegnerfisch2;
			ShowFishStep();
		};
		GameUi.ApplyButton(enemyTwo, 16, PartyState.OpponentSkin == NPCFish.EnemySkin.Gegnerfisch2);
		row.AddChild(enemyTwo);

		Button startButton = CreateButton(singleMiniGameSelected ? "Minispiel starten" : "Party starten");
		startButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/PartyMode.tscn", 0.32f);
		content.AddChild(startButton);

		Button backButton = CreateButton("Zurück");
		backButton.Pressed += () =>
		{
			if (singleMiniGameSelected)
				ShowModeStep();
			else
				ShowRoundsStep();
		};
		content.AddChild(backButton);
		GameUi.FocusFirstButton(this);
	}

	private void SelectSingleMiniGame(PartyState.GameSelection selection)
	{
		singleMiniGameSelected = true;
		PartyState.SelectedGame = selection;
		PartyState.Rounds = 1;
		ShowFishStep();
	}

	private string GetSelectionName(PartyState.GameSelection selection)
	{
		return selection switch
		{
			PartyState.GameSelection.Coins => "Münzen sammeln",
			PartyState.GameSelection.Cops => "Räuber und Gendarm",
			PartyState.GameSelection.DrunkRun => "Betrunkener Run",
			_ => "Fangen"
		};
	}

	private void UpdateRoundsLabel()
	{
		if (roundsLabel != null)
			roundsLabel.Text = $"{PartyState.Rounds} Runden";
	}

	private void ClearDynamicRows()
	{
		if (content == null)
			return;

		for (int i = content.GetChildCount() - 1; i >= 2; i--)
			content.GetChild(i).QueueFree();
	}

	private Label CreateLabel(string text, int fontSize, Color color)
	{
		Label label = new Label();
		label.Text = text;
		GameUi.ApplyLabel(label, fontSize, color);
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(230f, 46f);
		button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		GameUi.ApplyButton(button, 16);
		return button;
	}
}
