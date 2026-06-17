using Godot;
using System;
using System.Collections.Generic;

public partial class Settings : Control
{
	private VBoxContainer customBindings;
	private Label captureLabel;
	private Label statusLabel;
	private ConfirmationDialog resetSaveDialog;
	private string pendingCustomAction = "";

	private readonly Dictionary<PlayerFish.ControlScheme, Button> modeButtons =
		new Dictionary<PlayerFish.ControlScheme, Button>();
	private readonly Dictionary<string, Button> customButtons = new Dictionary<string, Button>();

	public override void _Ready()
	{
		GameAudio.EnsureMenuMusic(this);
		PlayerFish.LoadControlSettings();
		BuildUi();
		UpdateModeButtonLabels();
		UpdateCustomButtonLabels();
		UpdateStatus("Bereit");
		GameUi.FocusFirstButton(this);
		SceneTransition.FadeIn(GetTree(), 0.24f);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!string.IsNullOrEmpty(pendingCustomAction))
		{
			if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
			{
				if (IsEscapeKey(keyEvent))
				{
					CancelCapture();
					GetViewport().SetInputAsHandled();
					return;
				}

				InputEventKey captureKey = new InputEventKey();
				captureKey.PhysicalKeycode = keyEvent.PhysicalKeycode;
				captureKey.Keycode = keyEvent.Keycode;
				PlayerFish.SetCustomInput(pendingCustomAction, captureKey);
				FinishCapture("Taste gespeichert");
				GetViewport().SetInputAsHandled();
				return;
			}

			if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				InputEventMouseButton captureMouse = new InputEventMouseButton();
				captureMouse.ButtonIndex = mouseEvent.ButtonIndex;
				PlayerFish.SetCustomInput(pendingCustomAction, captureMouse);
				FinishCapture("Mausknopf gespeichert");
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (GameUi.IsCancelPressed(inputEvent))
		{
			GetViewport().SetInputAsHandled();
			GoBack();
		}
	}

	private void BuildUi()
	{
		OceanMapBackground background = new OceanMapBackground();
		background.ConfigureForScreen();
		AddChild(background);
		MoveChild(background, 0);

		ColorRect overlay = new ColorRect();
		overlay.Color = new Color(0.01f, 0.06f, 0.09f, 0.34f);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(overlay);

		Panel panel = new Panel();
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -330f;
		panel.OffsetTop = -352f;
		panel.OffsetRight = 330f;
		panel.OffsetBottom = 352f;
		panel.AddThemeStyleboxOverride("panel", GameUi.CreatePanelStyle());
		AddChild(panel);

		VBoxContainer layout = new VBoxContainer();
		layout.SetAnchorsPreset(LayoutPreset.FullRect);
		layout.OffsetLeft = 28f;
		layout.OffsetTop = 24f;
		layout.OffsetRight = -28f;
		layout.OffsetBottom = -24f;
		layout.AddThemeConstantOverride("separation", 12);
		panel.AddChild(layout);

		Label title = CreateLabel("Einstellungen", 30, GameUi.LightText);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(title);

		AddSoundSettings(layout);

		Label controlsTitle = CreateLabel("Steuerung", 20, GameUi.AccentText);
		controlsTitle.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(controlsTitle);

		GridContainer modes = new GridContainer();
		modes.Columns = 2;
		modes.AddThemeConstantOverride("h_separation", 10);
		modes.AddThemeConstantOverride("v_separation", 10);
		layout.AddChild(modes);

		AddModeButton(modes, "Pfeiltasten", PlayerFish.ControlScheme.ArrowKeys);
		AddModeButton(modes, "WASD", PlayerFish.ControlScheme.WASD);
		AddModeButton(modes, "Maus", PlayerFish.ControlScheme.Mouse);
		AddModeButton(modes, "Eigene Tasten", PlayerFish.ControlScheme.Custom);

		customBindings = new VBoxContainer();
		customBindings.AddThemeConstantOverride("separation", 7);
		layout.AddChild(customBindings);

		AddCustomBindingButton("Hoch", PlayerFish.CustomMoveUp);
		AddCustomBindingButton("Runter", PlayerFish.CustomMoveDown);
		AddCustomBindingButton("Links", PlayerFish.CustomMoveLeft);
		AddCustomBindingButton("Rechts", PlayerFish.CustomMoveRight);
		AddCustomBindingButton("Boost", PlayerFish.CustomBoost);
		AddCustomBindingButton("Item", PlayerFish.CustomUseItem);

		captureLabel = CreateLabel("", 15, GameUi.AccentText);
		captureLabel.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(captureLabel);

		statusLabel = CreateLabel("", 16, new Color(0.7f, 1f, 0.86f));
		statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		layout.AddChild(statusLabel);

		HBoxContainer actions = new HBoxContainer();
		actions.Alignment = BoxContainer.AlignmentMode.Center;
		actions.AddThemeConstantOverride("separation", 12);
		layout.AddChild(actions);

		Button resetButton = CreateMenuButton("Tasten resetten");
		resetButton.Pressed += ResetCustomBindings;
		actions.AddChild(resetButton);

		Button backButton = CreateMenuButton("Zurück");
		backButton.Pressed += GoBack;
		actions.AddChild(backButton);

		Button deleteSaveButton = CreateDangerButton("Spielstand löschen");
		deleteSaveButton.Pressed += ShowResetSaveDialog;
		layout.AddChild(deleteSaveButton);

		CreateResetSaveDialog();

		ControllerHintBar controllerHints = GameUi.CreateControllerHintBar(GameUi.ControllerHintMode.BackOnly);
		controllerHints.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		layout.AddChild(controllerHints);
	}

	private void AddSoundSettings(VBoxContainer parent)
	{
		Label soundTitle = CreateLabel("Sound", 20, GameUi.AccentText);
		soundTitle.HorizontalAlignment = HorizontalAlignment.Center;
		parent.AddChild(soundTitle);

		VBoxContainer soundBox = new VBoxContainer();
		soundBox.AddThemeConstantOverride("separation", 5);
		parent.AddChild(soundBox);

		AddVolumeSlider(soundBox, "Musik", GameAudio.MusicVolume, GameAudio.SetMusicVolume);
		AddVolumeSlider(soundBox, "Effekte", GameAudio.SfxVolume, GameAudio.SetSfxVolume);
	}

	private void AddVolumeSlider(VBoxContainer parent, string labelText, float value, Action<float> setter)
	{
		HBoxContainer row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		parent.AddChild(row);

		Label label = CreateLabel(labelText, 15, GameUi.LightText);
		label.CustomMinimumSize = new Vector2(92f, 0f);
		row.AddChild(label);

		HSlider slider = new HSlider();
		slider.MinValue = 0;
		slider.MaxValue = 100;
		slider.Step = 1;
		slider.Value = Mathf.RoundToInt(value * 100f);
		slider.CustomMinimumSize = new Vector2(280f, 28f);
		slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(slider);

		Label valueLabel = CreateLabel($"{slider.Value:0}%", 14, GameUi.AccentText);
		valueLabel.CustomMinimumSize = new Vector2(58f, 0f);
		valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
		row.AddChild(valueLabel);

		slider.ValueChanged += newValue =>
		{
			float normalized = (float)newValue / 100f;
			setter(normalized);
			valueLabel.Text = $"{newValue:0}%";
			UpdateStatus("Sound gespeichert");
		};
	}

	private Label CreateLabel(string text, int size, Color color)
	{
		Label label = new Label();
		label.Text = text;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		GameUi.ApplyLabel(label, size, color);
		return label;
	}

	private Button CreateMenuButton(string text)
	{
		Button button = new Button();
		button.Text = text;
		button.CustomMinimumSize = new Vector2(220f, 42f);
		button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		GameUi.ApplyButton(button, 16);
		return button;
	}

	private Button CreateDangerButton(string text)
	{
		Button button = CreateMenuButton(text);
		button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		button.CustomMinimumSize = new Vector2(260f, 42f);
		button.AddThemeStyleboxOverride(
			"normal",
			GameUi.CreateButtonStyle(new Color(1f, 0.48f, 0.48f, 0.58f), new Color(1f, 0.74f, 0.74f, 0.92f))
		);
		button.AddThemeStyleboxOverride(
			"hover",
			GameUi.CreateButtonStyle(new Color(1f, 0.62f, 0.62f, 0.78f), new Color(1f, 0.88f, 0.88f, 1f))
		);
		button.AddThemeStyleboxOverride(
			"pressed",
			GameUi.CreateButtonStyle(new Color(0.92f, 0.28f, 0.28f, 0.9f), new Color(1f, 0.9f, 0.9f, 1f))
		);
		return button;
	}

	private void CreateResetSaveDialog()
	{
		resetSaveDialog = new ConfirmationDialog();
		resetSaveDialog.Title = "Spielstand löschen";
		resetSaveDialog.DialogText =
			"Willst du wirklich alles zurücksetzen?\nHighscores, Münzen, Shop, Missionen und Tutorial-Fortschritt gehen verloren.";
		resetSaveDialog.OkButtonText = "Ja, löschen";
		resetSaveDialog.CancelButtonText = "Abbrechen";
		resetSaveDialog.Confirmed += ResetSaveData;
		AddChild(resetSaveDialog);
	}

	private void AddModeButton(GridContainer parent, string text, PlayerFish.ControlScheme scheme)
	{
		Button button = CreateMenuButton(text);
		button.Pressed += () => SelectControlScheme(scheme);
		parent.AddChild(button);
		modeButtons[scheme] = button;
	}

	private void AddCustomBindingButton(string label, string action)
	{
		Button button = CreateMenuButton("");
		button.Pressed += () =>
		{
			pendingCustomAction = action;
			captureLabel.Text = $"{label}: Taste oder Mausklick drücken";
			UpdateStatus("Esc bricht ab");
		};

		customBindings.AddChild(button);
		customButtons[action] = button;
	}

	private void SelectControlScheme(PlayerFish.ControlScheme scheme)
	{
		PlayerFish.SetControlScheme(scheme);
		UpdateModeButtonLabels();
		UpdateCustomButtonLabels();
		UpdateStatus("Gespeichert");
	}

	private void UpdateModeButtonLabels()
	{
		foreach (KeyValuePair<PlayerFish.ControlScheme, Button> pair in modeButtons)
			GameUi.ApplyButton(pair.Value, 16, pair.Key == PlayerFish.CurrentControlScheme);

		if (customBindings != null)
			customBindings.Visible = PlayerFish.CurrentControlScheme == PlayerFish.ControlScheme.Custom;
	}

	private void UpdateCustomButtonLabels()
	{
		if (customButtons.Count == 0)
			return;

		customButtons[PlayerFish.CustomMoveUp].Text =
			$"Hoch: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveUp)}";
		customButtons[PlayerFish.CustomMoveDown].Text =
			$"Runter: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveDown)}";
		customButtons[PlayerFish.CustomMoveLeft].Text =
			$"Links: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveLeft)}";
		customButtons[PlayerFish.CustomMoveRight].Text =
			$"Rechts: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomMoveRight)}";
		customButtons[PlayerFish.CustomBoost].Text =
			$"Boost: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomBoost)}";
		customButtons[PlayerFish.CustomUseItem].Text =
			$"Item: {PlayerFish.GetCustomInputLabel(PlayerFish.CustomUseItem)}";
	}

	private void ResetCustomBindings()
	{
		SetKey(PlayerFish.CustomMoveUp, Key.W);
		SetKey(PlayerFish.CustomMoveDown, Key.S);
		SetKey(PlayerFish.CustomMoveLeft, Key.A);
		SetKey(PlayerFish.CustomMoveRight, Key.D);
		SetKey(PlayerFish.CustomBoost, Key.Space);
		SetKey(PlayerFish.CustomUseItem, Key.P);
		PlayerFish.SaveControlSettings();
		UpdateCustomButtonLabels();
		UpdateStatus("Standard-Tasten wiederhergestellt");
	}

	private void ShowResetSaveDialog()
	{
		resetSaveDialog?.PopupCentered();
	}

	private void ResetSaveData()
	{
		ScoreManager scoreManager = GetNodeOrNull<ScoreManager>("/root/ScoreManager");
		if (scoreManager == null)
		{
			UpdateStatus("ScoreManager fehlt");
			return;
		}

		scoreManager.ResetAllProgress();
		UpdateStatus("Spielstand gelöscht");
	}

	private void SetKey(string action, Key key)
	{
		InputEventKey keyEvent = new InputEventKey();
		keyEvent.PhysicalKeycode = key;
		keyEvent.Keycode = key;
		PlayerFish.SetCustomInput(action, keyEvent);
	}

	private void FinishCapture(string message)
	{
		pendingCustomAction = "";
		captureLabel.Text = "";
		UpdateCustomButtonLabels();
		UpdateStatus(message);
	}

	private void CancelCapture()
	{
		pendingCustomAction = "";
		captureLabel.Text = "";
		UpdateStatus("Abgebrochen");
	}

	private void UpdateStatus(string text)
	{
		if (statusLabel != null)
			statusLabel.Text = $"> {text}";
	}

	private bool IsEscapeKey(InputEventKey keyEvent)
	{
		return keyEvent.PhysicalKeycode == Key.Escape ||
			keyEvent.Keycode == Key.Escape;
	}

	private void GoBack()
	{
		SceneTransition.FadeToScene(GetTree(), "res://Scenes/MainMenu.tscn", 0.28f);
	}
}
