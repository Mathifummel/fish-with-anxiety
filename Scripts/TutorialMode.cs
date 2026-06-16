using Godot;

public partial class TutorialMode : Node2D
{
	[Export] public PlayerFish Player;
	[Export] public PackedScene NPCFishScene;
	[Export] public PackedScene AlcoholItemScene;
	[Export] public PackedScene ChorusFruitItemScene;
	[Export] public PackedScene TrashItemScene;

	private enum TutorialPhase
	{
		Move,
		Coin,
		Boost,
		Dodge,
		CrossingWarning,
		GoodItem,
		Trash,
		Finish
	}

	private const string TutorialSeenPath = "user://tutorial_seen.save";
	private const float TargetRadius = 72f;
	private const float CollectRadius = 58f;

	private CanvasLayer ui;
	private Label titleLabel;
	private Label instructionLabel;
	private Label progressLabel;
	private Label feedbackLabel;
	private ProgressBar stressBar;
	private TextureRect hintArrow;
	private Node2D targetMarker;
	private Node2D activePickup;
	private NPCFish trainingNpc;
	private NPCFish crossingNpc;
	private Sprite2D warningMarker;
	private Texture2D warningTexture;

	private TutorialPhase phase = TutorialPhase.Move;
	private Vector2 targetPosition;
	private float phaseTimer = 0f;
	private float safeTimer = 0f;
	private float stress = 0f;
	private float visualTime = 0f;
	private bool trashCollected = false;
	private bool goodItemCollected = false;
	private bool boostUsed = false;
	private bool crossingFishSpawned = false;
	private OceanMapBackground backgroundMap;

	public override void _Ready()
	{
		PlayerFish.LoadControlSettings();
		warningTexture =
			ResourceLoader.Load<Texture2D>("res://Assets/exclamation_spritesheet_01.png");
		CreateBackgroundLayer();
		CreateTargetMarker();
		CreateUi();

		Player.Position = Vector2.Zero;
		Player.CurrentStress = 0f;
		Player.SetInvincible(false);
		Player.SpeedMultiplier = 1f;
		Player.SetPhysicsProcess(true);

		StartPhase(TutorialPhase.Move);
		SceneTransition.FadeIn(GetTree(), 0.28f);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		visualTime += dt;
		phaseTimer += dt;

		AnimateTargetMarker();
		UpdateHintArrow();
		UpdatePhase(dt);
		UpdateUi();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (GameUi.IsCancelPressed(inputEvent))
		{
			GetViewport().SetInputAsHandled();
			SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.25f);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateTutorialStress((float)delta);
	}

	private void StartPhase(TutorialPhase nextPhase)
	{
		ClearPhaseObjects();

		phase = nextPhase;
		phaseTimer = 0f;
		safeTimer = 0f;
		trashCollected = false;
		goodItemCollected = false;
		boostUsed = false;
		crossingFishSpawned = false;

		switch (phase)
		{
			case TutorialPhase.Move:
				stress = 0f;
				SetTarget(Player.Position + new Vector2(430f, -80f), true);
				SetText(
					"1 / 7  Bewegung",
					"Schwimm durch den leuchtenden Kreis.",
					"Benutze deine eingestellte Steuerung. Im Tutorial passiert nichts Schlimmes, solange du ausprobierst."
				);
				break;

			case TutorialPhase.Coin:
				stress = 0f;
				activePickup = CreateSpritePickup("res://Assets/m\u00FCnze.png", Player.Position + new Vector2(390f, 120f), new Vector2(1f, 1f));
				SetTarget(activePickup.Position, false);
				SetText(
					"2 / 7  Münzen",
					"Sammle die Münze ein.",
					"Münzen geben Bonuspunkte. In diesem Tutorial landet sie nur als Übung in deiner Tasche."
				);
				break;

			case TutorialPhase.Boost:
				stress = 50f;
				Player.CurrentStress = stress;
				SetTarget(Player.Position + new Vector2(520f, 0f), true);
				SetText(
					"3 / 7  Boost",
					"Löse einen Boost aus und schiess zum Zielkreis.",
					$"Boost: {GetBoostHint()}. Bei mittlerem Stress ist der Boost am stärksten."
				);
				break;

			case TutorialPhase.Dodge:
				stress = 25f;
				SpawnTrainingNpc();
				SetTarget(Vector2.Zero, false);
				SetText(
					"4 / 7  Stress & Gegner",
					"Weich dem Gegner aus, bis der Timer voll ist.",
					"Wenn er nah kommt, steigt Stress. Abstand halten senkt ihn wieder."
				);
				break;

			case TutorialPhase.CrossingWarning:
				stress = 20f;
				SetTarget(Vector2.Zero, false);
				CreateWarningMarker();
				SetText(
					"5 / 7  Achtung",
					"Achte auf das Warnzeichen und bleib aus der Bahn.",
					"Wenn Achtung auftaucht, rast gleich ein schneller Fisch quer durch den Screen."
				);
				break;

			case TutorialPhase.GoodItem:
				stress = 66f;
				activePickup = CreateSpritePickup("res://Assets/Alkohol.png", Player.Position + new Vector2(380f, -150f), new Vector2(1f, 1f));
				SetTarget(activePickup.Position, false);
				SetText(
					"6 / 7  Gute Items",
					"Folge dem Pfeil und sammle das gute Item.",
					"Nützliche Items wie Alkohol oder Chorusfrucht können dich retten. Der Pfeil zeigt auf solche Items."
				);
				break;

			case TutorialPhase.Trash:
				stress = 18f;
				activePickup = CreateSpritePickup("res://Assets/M\u00FCll.png", Player.Position + new Vector2(360f, 130f), new Vector2(1f, 1f));
				SetTarget(activePickup.Position, false);
				SetText(
					"7 / 7  Müll",
					"Sammle den Müll ein und schwimm danach zum Kreis.",
					"Müll ist ein schlechtes Item: Es bremst dich kurz aus."
				);
				break;

			case TutorialPhase.Finish:
				Player.SetPhysicsProcess(false);
				Player.SpeedMultiplier = 1f;
				Player.SetInvincible(false);
				stress = 0f;
				SaveTutorialSeen();
				SetTarget(Vector2.Zero, false);
				SetText(
					"Training geschafft",
					"Du bist bereit für den offenen Ozean.",
					"Du kennst jetzt Bewegung, Münzen, Boost, Stress, Achtung-Warnungen, hilfreiche Items und Müll."
				);
				ShowFinishButtons();
				break;
		}
	}

	private void UpdatePhase(float dt)
	{
		switch (phase)
		{
			case TutorialPhase.Move:
				if (Player.Position.DistanceTo(targetPosition) <= TargetRadius)
				{
					feedbackLabel.Text = "Gut. Genau so bewegst du dich aus Gefahr heraus.";
					StartPhase(TutorialPhase.Coin);
				}
				break;

			case TutorialPhase.Coin:
				if (IsPickupCollected())
				{
					feedbackLabel.Text = "Eingesammelt. Im echten Spiel zählt das direkt für den Score.";
					StartPhase(TutorialPhase.Boost);
				}
				break;

			case TutorialPhase.Boost:
				if (Player.IsBoosting)
				{
					boostUsed = true;
					feedbackLabel.Text = "Boost aktiv. Jetzt raus aus der engen Stelle.";
				}

				if (boostUsed && Player.Position.DistanceTo(targetPosition) <= TargetRadius && phaseTimer > 0.6f)
					StartPhase(TutorialPhase.Dodge);
				break;

			case TutorialPhase.Dodge:
				if (trainingNpc == null || !IsInstanceValid(trainingNpc))
					SpawnTrainingNpc();

				if (stress >= 92f)
				{
					feedbackLabel.Text = "Zu nah. Nochmal Abstand finden.";
					stress = 55f;
					ResetTrainingNpc();
					safeTimer = 0f;
					return;
				}

				if (stress > 70f)
				{
					safeTimer = Mathf.Max(0f, safeTimer - dt * 1.6f);
					feedbackLabel.Text = "Noch zu nah. Erst Abstand finden, dann zählt die Zeit.";
				}
				else
				{
					safeTimer += dt;
				}

				progressLabel.Text = $"Abstand halten: {Mathf.CeilToInt(Mathf.Max(0f, 4.5f - safeTimer))}s";

				if (safeTimer >= 4.5f)
					StartPhase(TutorialPhase.CrossingWarning);
				break;

			case TutorialPhase.CrossingWarning:
				UpdateCrossingWarningStep();

				if (phaseTimer >= 4.2f)
					StartPhase(TutorialPhase.GoodItem);
				break;

			case TutorialPhase.GoodItem:
				if (!goodItemCollected && IsPickupCollected())
				{
					goodItemCollected = true;
					phaseTimer = 0f;
					Player.SetInvincible(true);
					stress = 0f;
					feedbackLabel.Text = "Gutes Item! Stress weg, kurz unverwundbar.";
				}

				if (goodItemCollected && phaseTimer > 1.2f)
					StartPhase(TutorialPhase.Trash);
				break;

			case TutorialPhase.Trash:
				if (!trashCollected && IsPickupCollected())
				{
					trashCollected = true;
					phaseTimer = 0f;
					Player.ApplySlow(0.52f, 2.4f);
					SetTarget(Player.Position + new Vector2(360f, -80f), true);
					feedbackLabel.Text = "Müll bremst. Du kommst noch weg, aber langsam.";
				}

				if (trashCollected && Player.Position.DistanceTo(targetPosition) <= TargetRadius)
					StartPhase(TutorialPhase.Finish);
				break;
		}
	}

	private bool IsPickupCollected()
	{
		if (activePickup == null)
			return false;

		if (!IsInstanceValid(activePickup) || activePickup.IsQueuedForDeletion())
		{
			activePickup = null;
			return true;
		}

		if (Player.Position.DistanceTo(activePickup.Position) > CollectRadius)
			return false;

		activePickup.QueueFree();
		activePickup = null;
		return true;
	}

	private void SpawnTrainingNpc()
	{
		trainingNpc = NPCFishScene.Instantiate<NPCFish>();
		trainingNpc.Player = Player;
		trainingNpc.Speed = 95f;
		trainingNpc.Position = Player.Position + new Vector2(620f, 0f);
		AddChild(trainingNpc);
	}

	private void ResetTrainingNpc()
	{
		if (trainingNpc == null || !IsInstanceValid(trainingNpc))
			return;

		Vector2 side = Player.Velocity.LengthSquared() > 1f
			? Player.Velocity.Normalized().Rotated(Mathf.Pi * 0.5f)
			: Vector2.Up;

		trainingNpc.Position = Player.Position - side * 520f;
	}

	private void CreateWarningMarker()
	{
		warningMarker = new Sprite2D();
		warningMarker.Texture = warningTexture;
		warningMarker.Hframes = 8;
		warningMarker.Frame = 6;
		warningMarker.Centered = true;
		warningMarker.ZIndex = 120;
		warningMarker.Scale = new Vector2(2.45f, 2.45f);
		warningMarker.Modulate = new Color(1f, 1f, 1f, 0.95f);
		warningMarker.Position = GetCrossingWarningPosition();
		AddChild(warningMarker);
	}

	private void UpdateCrossingWarningStep()
	{
		if (warningMarker != null && IsInstanceValid(warningMarker))
		{
			float progress = Mathf.Clamp(phaseTimer / 1.55f, 0f, 1f);
			float pulse = Mathf.Sin(progress * Mathf.Tau * 2.5f);
			float scale = 2.3f + pulse * 0.28f;

			warningMarker.Position = GetCrossingWarningPosition();
			warningMarker.Scale = new Vector2(scale, scale);
			warningMarker.Frame = Mathf.PosMod((int)(progress * 16f), 8);
			warningMarker.Modulate =
				new Color(1f, 1f, 1f, 0.72f + Mathf.Abs(pulse) * 0.24f);
		}

		if (!crossingFishSpawned && phaseTimer >= 1.55f)
		{
			crossingFishSpawned = true;
			feedbackLabel.Text = "Da ist er. Seitlich ausweichen, nicht in die Bahn schwimmen.";

			if (warningMarker != null && IsInstanceValid(warningMarker))
			{
				warningMarker.QueueFree();
				warningMarker = null;
			}

			SpawnCrossingTutorialNpc();
		}
	}

	private Vector2 GetCrossingWarningPosition()
	{
		Vector2 viewport = GetViewportRect().Size;
		return Player.Position + new Vector2(-Mathf.Max(viewport.X * 0.42f, 360f), -38f);
	}

	private void SpawnCrossingTutorialNpc()
	{
		crossingNpc = NPCFishScene.Instantiate<NPCFish>();
		crossingNpc.Player = Player;
		crossingNpc.Mode = NPCFish.MovementMode.Crossing;
		crossingNpc.CrossingDirection = Vector2.Right;
		crossingNpc.CrossingLifetime = 3.2f;
		crossingNpc.Speed = 520f;

		Vector2 viewport = GetViewportRect().Size;
		crossingNpc.Position =
			Player.Position + new Vector2(-Mathf.Max(viewport.X * 0.58f, 520f), -38f);
		crossingNpc.SetPhysicsProcess(true);
		AddChild(crossingNpc);
	}

	private void UpdateTutorialStress(float dt)
	{
		float targetStress = 0f;

		if (phase == TutorialPhase.Dodge && trainingNpc != null && IsInstanceValid(trainingNpc))
		{
			float distance = Player.Position.DistanceTo(trainingNpc.Position);
			float pressure = 1f - Mathf.Clamp(distance / 440f, 0f, 1f);
			targetStress = pressure * pressure * 100f;
		}
		else
		{
			targetStress = stress;
		}

		float speed = targetStress > stress ? 95f : 38f;
		stress = Mathf.MoveToward(stress, targetStress, speed * dt);

		if (Player.IsBoosting)
			stress = Mathf.MoveToward(stress, 0f, Player.GetStressDrain() * dt);

		if (Player.IsInvincible)
			stress = Mathf.MoveToward(stress, 0f, 120f * dt);

		stress = Mathf.Clamp(stress, 0f, 100f);
		Player.CurrentStress = stress;

		if (stressBar != null)
			stressBar.Value = stress;
	}

	private Node2D CreateSpritePickup(string texturePath, Vector2 position, Vector2 scale)
	{
		Node2D pickup = new Node2D();
		pickup.Position = position;

		Sprite2D sprite = new Sprite2D();
		sprite.Texture = ResourceLoader.Load<Texture2D>(texturePath);

		if (sprite.Texture == null)
			sprite.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Herz.png");

		sprite.Scale = scale;
		pickup.AddChild(sprite);

		AddChild(pickup);
		return pickup;
	}

	private void SetTarget(Vector2 position, bool visible)
	{
		targetPosition = position;
		targetMarker.Position = position;
		targetMarker.Visible = visible;
	}

	private void SetText(string title, string instruction, string feedback)
	{
		titleLabel.Text = title;
		instructionLabel.Text = instruction;
		feedbackLabel.Text = feedback;
		progressLabel.Text = "";
	}

	private string GetBoostHint()
	{
		return PlayerFish.CurrentControlScheme == PlayerFish.ControlScheme.Custom
			? PlayerFish.GetCustomInputLabel(PlayerFish.CustomBoost)
			: "Leertaste oder Enter";
	}

	private void ClearPhaseObjects()
	{
		if (activePickup != null && IsInstanceValid(activePickup))
			activePickup.QueueFree();

		activePickup = null;

		if (trainingNpc != null && IsInstanceValid(trainingNpc))
			trainingNpc.QueueFree();

		trainingNpc = null;

		if (crossingNpc != null && IsInstanceValid(crossingNpc))
			crossingNpc.QueueFree();

		crossingNpc = null;

		if (warningMarker != null && IsInstanceValid(warningMarker))
			warningMarker.QueueFree();

		warningMarker = null;

		if (Player != null)
		{
			Player.SetInvincible(false);
			Player.SpeedMultiplier = 1f;
		}
	}

	private void CreateBackgroundLayer()
	{
		backgroundMap = new OceanMapBackground();
		backgroundMap.ConfigureForWorld(Player);
		AddChild(backgroundMap);
		MoveChild(backgroundMap, 0);
	}

	private void CreateTargetMarker()
	{
		targetMarker = new Node2D();
		AddChild(targetMarker);

		Line2D ring = new Line2D();
		ring.Width = 6f;
		ring.DefaultColor = new Color(0.36f, 1f, 0.76f, 0.78f);
		ring.Closed = true;

		for (int i = 0; i < 42; i++)
		{
			float angle = i / 42f * Mathf.Tau;
			ring.AddPoint(Vector2.FromAngle(angle) * TargetRadius);
		}

		targetMarker.AddChild(ring);
		targetMarker.Hide();
	}

	private void AnimateTargetMarker()
	{
		if (targetMarker == null || !targetMarker.Visible)
			return;

		float pulse = 1f + Mathf.Sin(visualTime * 5f) * 0.08f;
		targetMarker.Scale = new Vector2(pulse, pulse);
	}

	private void CreateUi()
	{
		ui = new CanvasLayer();
		AddChild(ui);

		Panel panel = new Panel();
		panel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		panel.OffsetLeft = 24f;
		panel.OffsetTop = 18f;
		panel.OffsetRight = -24f;
		panel.OffsetBottom = 154f;
		panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.02f, 0.1f, 0.15f, 0.9f)));
		ui.AddChild(panel);

		VBoxContainer layout = new VBoxContainer();
		layout.AnchorRight = 1f;
		layout.AnchorBottom = 1f;
		layout.OffsetLeft = 24f;
		layout.OffsetTop = 16f;
		layout.OffsetRight = -24f;
		layout.OffsetBottom = -16f;
		layout.AddThemeConstantOverride("separation", 5);
		panel.AddChild(layout);

		titleLabel = CreateLabel(24, GameUi.DarkText);
		layout.AddChild(titleLabel);

		instructionLabel = CreateLabel(28, new Color(0.48f, 0.36f, 0.02f));
		layout.AddChild(instructionLabel);

		feedbackLabel = CreateLabel(17, new Color(0.05f, 0.22f, 0.34f, 0.82f));
		feedbackLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		layout.AddChild(feedbackLabel);

		progressLabel = CreateLabel(18, new Color(0.7f, 1f, 0.8f));
		ui.AddChild(progressLabel);
		progressLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		progressLabel.OffsetLeft = 280f;
		progressLabel.OffsetRight = -280f;
		progressLabel.OffsetTop = -72f;
		progressLabel.OffsetBottom = -28f;
		progressLabel.HorizontalAlignment = HorizontalAlignment.Center;

		stressBar = new ProgressBar();
		stressBar.MinValue = 0f;
		stressBar.MaxValue = 100f;
		stressBar.Value = 0f;
		stressBar.ShowPercentage = false;
		stressBar.CustomMinimumSize = new Vector2(340f, 22f);
		stressBar.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		stressBar.OffsetLeft = 24f;
		stressBar.OffsetTop = 170f;
		stressBar.OffsetRight = 364f;
		stressBar.OffsetBottom = 192f;
		ui.AddChild(stressBar);

		Button menuButton = CreateButton("Hauptmenü");
		menuButton.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		menuButton.OffsetLeft = -168f;
		menuButton.OffsetTop = 170f;
		menuButton.OffsetRight = -24f;
		menuButton.OffsetBottom = 212f;
		menuButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.25f);
		ui.AddChild(menuButton);

		hintArrow = new TextureRect();
		hintArrow.Texture = ResourceLoader.Load<Texture2D>("res://Assets/Pfeil.png");
		hintArrow.CustomMinimumSize = new Vector2(96f, 58f);
		hintArrow.PivotOffset = new Vector2(48f, 29f);
		hintArrow.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		hintArrow.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		hintArrow.MouseFilter = Control.MouseFilterEnum.Ignore;
		hintArrow.Hide();
		ui.AddChild(hintArrow);
	}

	private void UpdateUi()
	{
		if (stressBar == null)
			return;

		StyleBoxFlat fill = new StyleBoxFlat();
		fill.CornerRadiusTopLeft = 6;
		fill.CornerRadiusTopRight = 6;
		fill.CornerRadiusBottomLeft = 6;
		fill.CornerRadiusBottomRight = 6;
		fill.BgColor = stress < 40f
			? new Color(0.25f, 0.67f, 1f)
			: stress < 70f
				? new Color(0.35f, 0.95f, 0.48f)
				: new Color(1f, 0.34f, 0.31f);
		stressBar.AddThemeStyleboxOverride("fill", fill);
	}

	private void UpdateHintArrow()
	{
		if (hintArrow == null)
			return;

		bool shouldShow = activePickup != null &&
			IsInstanceValid(activePickup) &&
			(phase == TutorialPhase.GoodItem || phase == TutorialPhase.Trash || phase == TutorialPhase.Coin);

		if (!shouldShow)
		{
			hintArrow.Hide();
			return;
		}

		Vector2 direction = activePickup.GlobalPosition - Player.GlobalPosition;

		if (direction.LengthSquared() < 1f)
			direction = Vector2.Right;

		direction = direction.Normalized();
		Vector2 viewport = GetViewportRect().Size;
		Vector2 center = viewport * 0.5f + direction * 154f;
		hintArrow.Position = center - new Vector2(48f, 29f);
		hintArrow.Rotation = direction.Angle();
		hintArrow.Show();
	}

	private Label CreateLabel(int fontSize, Color color)
	{
		Label label = new Label();
		GameUi.ApplyLabel(label, fontSize, color);
		return label;
	}

	private Button CreateButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.FocusMode = Control.FocusModeEnum.None;
		GameUi.ApplyButton(button, 15);
		return button;
	}

	private StyleBoxFlat CreatePanelStyle(Color color)
	{
		return GameUi.CreatePanelStyle();
	}

	private StyleBoxFlat CreateButtonStyle(Color color)
	{
		return GameUi.CreateButtonStyle(color, GameUi.ButtonBorder);
	}

	private void ShowFinishButtons()
	{
		HBoxContainer buttons = new HBoxContainer();
		buttons.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
		buttons.OffsetLeft = 390f;
		buttons.OffsetRight = -390f;
		buttons.OffsetTop = -96f;
		buttons.OffsetBottom = -42f;
		buttons.AddThemeConstantOverride("separation", 14);
		ui.AddChild(buttons);

		Button playButton = CreateButton("Spiel starten");
		playButton.CustomMinimumSize = new Vector2(190f, 48f);
		playButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		playButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/main.tscn", 0.32f);
		buttons.AddChild(playButton);

		Button menuButton = CreateButton("Hauptmenü");
		menuButton.CustomMinimumSize = new Vector2(190f, 48f);
		menuButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		menuButton.Pressed += () => SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.32f);
		buttons.AddChild(menuButton);

		ControllerHintBar controllerHints = GameUi.CreateControllerHintBar();
		GameUi.PlaceControllerHintOverlay(controllerHints, 10f);
		ui.AddChild(controllerHints);

		GameUi.FocusFirstButton(buttons);
	}

	private void SaveTutorialSeen()
	{
		var file = FileAccess.Open(TutorialSeenPath, FileAccess.ModeFlags.Write);

		if (file == null)
		{
			GD.PushWarning("Konnte Tutorial-Status nicht speichern.");
			return;
		}

		file.StoreString("1");
		file.Close();
	}
}
