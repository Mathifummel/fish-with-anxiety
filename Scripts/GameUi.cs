using Godot;
using System.Collections.Generic;

public static class GameUi
{
	public enum ControllerFamily
	{
		Generic,
		PlayStation,
		Xbox
	}

	public enum ControllerHintMode
	{
		Menu,
		GameOver,
		BackOnly
	}

	private static Font pixelFont;
	private static bool inputDefaultsReady = false;
	private static bool controllerNoticeShownThisRun = false;
	private const float ShortRumbleDuration = 0.16f;

	public static readonly Color PanelBackground = new Color(1f, 1f, 1f, 0.24f);
	public static readonly Color PanelBorder = new Color(1f, 1f, 1f, 0.62f);
	public static readonly Color ButtonNormal = new Color(1f, 1f, 1f, 0.62f);
	public static readonly Color ButtonHover = new Color(1f, 1f, 1f, 0.82f);
	public static readonly Color ButtonPressed = new Color(0.84f, 0.96f, 1f, 0.86f);
	public static readonly Color ButtonFocus = new Color(0.38f, 0.94f, 1f, 0.95f);
	public static readonly Color ButtonBorder = new Color(1f, 1f, 1f, 0.86f);
	public static readonly Color ButtonHoverBorder = new Color(0.82f, 0.96f, 1f, 0.95f);
	public static readonly Color DarkText = new Color(0.05f, 0.22f, 0.34f);
	public static readonly Color LightText = new Color(0.93f, 0.98f, 1f);
	public static readonly Color MutedText = new Color(0.72f, 0.88f, 0.92f);
	public static readonly Color AccentText = new Color(1f, 0.94f, 0.62f);

	public static Font PixelFont
	{
		get
		{
			if (pixelFont != null)
				return pixelFont;

			if (ResourceLoader.Exists("res://Assets/Fonts/pixel.ttf"))
			{
				pixelFont = ResourceLoader.Load<Font>("res://Assets/Fonts/pixel.ttf");
				return pixelFont;
			}

			SystemFont systemFont = new SystemFont();
			systemFont.FontNames = new string[] { "Cascadia Mono", "Consolas", "Courier New", "monospace" };
			pixelFont = systemFont;
			return pixelFont;
		}
	}

	public static void ApplyFont(Control control, int fontSize)
	{
		control.AddThemeFontOverride("font", PixelFont);
		control.AddThemeFontSizeOverride("font_size", fontSize);
	}

	public static void ApplyLabel(Label label, int fontSize, Color color)
	{
		ApplyFont(label, fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.58f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
	}

	public static StyleBoxFlat CreatePanelStyle()
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = PanelBackground;
		style.BorderColor = PanelBorder;
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 8;
		style.CornerRadiusTopRight = 8;
		style.CornerRadiusBottomLeft = 8;
		style.CornerRadiusBottomRight = 8;
		style.ContentMarginLeft = 28;
		style.ContentMarginTop = 30;
		style.ContentMarginRight = 28;
		style.ContentMarginBottom = 30;
		style.ShadowColor = new Color(0.35f, 0.78f, 1f, 0.3f);
		style.ShadowSize = 24;
		return style;
	}

	public static StyleBoxFlat CreateButtonStyle(Color background, Color border)
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = background;
		style.BorderColor = border;
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 8;
		style.CornerRadiusTopRight = 8;
		style.CornerRadiusBottomLeft = 8;
		style.CornerRadiusBottomRight = 8;
		style.ContentMarginLeft = 18;
		style.ContentMarginTop = 6;
		style.ContentMarginRight = 18;
		style.ContentMarginBottom = 6;
		style.ShadowColor = new Color(0f, 0.16f, 0.26f, 0.12f);
		style.ShadowSize = 5;
		return style;
	}

	public static void ApplyButton(Button button, int fontSize = 18, bool selected = false)
	{
		EnsureInputDefaults();

		Color normal = selected ? new Color(0.82f, 0.96f, 1f, 0.86f) : ButtonNormal;
		Color hover = selected ? new Color(1f, 1f, 1f, 0.9f) : ButtonHover;
		Color pressed = selected ? new Color(0.72f, 0.92f, 1f, 0.92f) : ButtonPressed;

		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(normal, ButtonBorder));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(hover, ButtonHoverBorder));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(pressed, new Color(1f, 1f, 1f, 0.98f)));
		button.AddThemeStyleboxOverride("focus", CreateButtonStyle(ButtonFocus, new Color(0.02f, 0.34f, 0.52f, 1f)));
		button.AddThemeColorOverride("font_color", selected ? new Color(0.02f, 0.14f, 0.24f) : DarkText);
		button.AddThemeColorOverride("font_hover_color", new Color(0.02f, 0.16f, 0.28f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.02f, 0.14f, 0.24f));
		button.AddThemeColorOverride("font_focus_color", new Color(0f, 0.08f, 0.13f));
		button.FocusMode = Control.FocusModeEnum.All;
		ApplyFont(button, fontSize);
	}

	public static void EnsureInputDefaults()
	{
		if (inputDefaultsReady)
			return;

		inputDefaultsReady = true;

		EnsureKeyAction("ui_up", Key.Up);
		EnsureKeyAction("ui_down", Key.Down);
		EnsureKeyAction("ui_left", Key.Left);
		EnsureKeyAction("ui_right", Key.Right);
		EnsureKeyAction("ui_accept", Key.Enter);
		EnsureKeyAction("ui_accept", Key.Space);
		EnsureKeyAction(PlayerFish.UseItemAction, Key.P);
		EnsureJoyButtonAction("ui_accept", JoyButton.A);
		EnsureJoyButtonAction("ui_cancel", JoyButton.B);
		EnsureJoyButtonAction(PlayerFish.UseItemAction, JoyButton.Y);
		EnsureJoyButtonAction("ui_up", JoyButton.DpadUp);
		EnsureJoyButtonAction("ui_down", JoyButton.DpadDown);
		EnsureJoyButtonAction("ui_left", JoyButton.DpadLeft);
		EnsureJoyButtonAction("ui_right", JoyButton.DpadRight);
		EnsureJoyAxisAction("ui_up", JoyAxis.LeftY, -1f);
		EnsureJoyAxisAction("ui_down", JoyAxis.LeftY, 1f);
		EnsureJoyAxisAction("ui_left", JoyAxis.LeftX, -1f);
		EnsureJoyAxisAction("ui_right", JoyAxis.LeftX, 1f);
		EnsureJoyButtonAction("pause", JoyButton.Start);
	}

	public static void FocusFirstButton(Control root)
	{
		ConfigureButtonNavigation(root);

		Button button = FindFirstButton(root);

		if (button != null)
			button.CallDeferred(Control.MethodName.GrabFocus);
	}

	public static void ConfigureButtonNavigation(Control root)
	{
		List<Button> buttons = new List<Button>();
		CollectFocusableButtons(root, buttons);

		if (buttons.Count == 0)
			return;

		for (int i = 0; i < buttons.Count; i++)
		{
			Button current = buttons[i];
			Button previous = buttons[Mathf.PosMod(i - 1, buttons.Count)];
			Button next = buttons[Mathf.PosMod(i + 1, buttons.Count)];

			NodePath previousPath = current.GetPathTo(previous);
			NodePath nextPath = current.GetPathTo(next);

			current.FocusNeighborTop = previousPath;
			current.FocusNeighborLeft = previousPath;
			current.FocusNeighborBottom = nextPath;
			current.FocusNeighborRight = nextPath;
		}
	}

	private static void CollectFocusableButtons(Node node, List<Button> buttons)
	{
		if (node is Button button &&
			button.IsVisibleInTree() &&
			!button.Disabled &&
			!button.IsQueuedForDeletion())
		{
			buttons.Add(button);
		}

		foreach (Node child in node.GetChildren())
			CollectFocusableButtons(child, buttons);
	}

	private static Button FindFirstButton(Node node)
	{
		if (node is Button button &&
			button.IsVisibleInTree() &&
			!button.Disabled &&
			!button.IsQueuedForDeletion())
		{
			return button;
		}

		foreach (Node child in node.GetChildren())
		{
			Button found = FindFirstButton(child);

			if (found != null)
				return found;
		}

		return null;
	}

	private static void EnsureKeyAction(string action, Key key)
	{
		if (!InputMap.HasAction(action))
			InputMap.AddAction(action);

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey keyEvent &&
				(keyEvent.PhysicalKeycode == key || keyEvent.Keycode == key))
			{
				return;
			}
		}

		InputEventKey newEvent = new InputEventKey();
		newEvent.PhysicalKeycode = key;
		InputMap.ActionAddEvent(action, newEvent);
	}

	private static void EnsureJoyButtonAction(string action, JoyButton button)
	{
		if (!InputMap.HasAction(action))
			InputMap.AddAction(action);

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventJoypadButton joyEvent &&
				joyEvent.ButtonIndex == button)
			{
				return;
			}
		}

		InputEventJoypadButton newEvent = new InputEventJoypadButton();
		newEvent.ButtonIndex = button;
		InputMap.ActionAddEvent(action, newEvent);
	}

	private static void EnsureJoyAxisAction(string action, JoyAxis axis, float axisValue)
	{
		if (!InputMap.HasAction(action))
			InputMap.AddAction(action);

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventJoypadMotion motionEvent &&
				motionEvent.Axis == axis &&
				Mathf.Sign(motionEvent.AxisValue) == Mathf.Sign(axisValue))
			{
				return;
			}
		}

		InputEventJoypadMotion newEvent = new InputEventJoypadMotion();
		newEvent.Axis = axis;
		newEvent.AxisValue = axisValue;
		InputMap.ActionAddEvent(action, newEvent);
	}

	public static bool IsPausePressed(InputEvent inputEvent)
	{
		return inputEvent.IsActionPressed("pause") ||
			(inputEvent is InputEventJoypadButton joyEvent &&
				joyEvent.Pressed &&
				joyEvent.ButtonIndex == JoyButton.Start);
	}

	public static bool IsCancelPressed(InputEvent inputEvent)
	{
		return inputEvent.IsActionPressed("ui_cancel") ||
			(inputEvent is InputEventJoypadButton joyEvent &&
				joyEvent.Pressed &&
				joyEvent.ButtonIndex == JoyButton.B);
	}

	public static void RumbleConnectedJoypads(float weak = 0.25f, float strong = 0.75f, float duration = ShortRumbleDuration)
	{
		foreach (int device in Input.GetConnectedJoypads())
			Input.StartJoyVibration(device, weak, strong, duration);
	}

	public static StyleBoxFlat CreateInputStyle(bool focused)
	{
		StyleBoxFlat style = CreateButtonStyle(
			focused ? new Color(1f, 1f, 1f, 0.74f) : new Color(1f, 1f, 1f, 0.52f),
			focused ? new Color(0.65f, 0.9f, 1f, 1f) : ButtonBorder
		);
		style.ContentMarginLeft = 14;
		style.ContentMarginRight = 14;
		return style;
	}

	public static StyleBoxFlat CreateRowStyle(Color background, float borderAlpha)
	{
		StyleBoxFlat style = CreateButtonStyle(background, new Color(1f, 1f, 1f, borderAlpha));
		style.ContentMarginLeft = 10;
		style.ContentMarginTop = 4;
		style.ContentMarginRight = 10;
		style.ContentMarginBottom = 4;
		style.ShadowSize = 0;
		return style;
	}

	public static ControllerHintBar CreateControllerHintBar(ControllerHintMode mode = ControllerHintMode.Menu)
	{
		ControllerHintBar bar = new ControllerHintBar();
		bar.Mode = mode;
		bar.MouseFilter = Control.MouseFilterEnum.Ignore;
		bar.CustomMinimumSize = new Vector2(0f, 42f);
		bar.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		bar.AddThemeStyleboxOverride("panel", CreateControllerHintStyle());

		HBoxContainer items = new HBoxContainer();
		items.Alignment = BoxContainer.AlignmentMode.Center;
		items.AddThemeConstantOverride("separation", 8);
		bar.Items = items;
		bar.AddChild(items);

		UpdateControllerHintBar(bar);
		return bar;
	}

	public static void PlaceControllerHintOverlay(Control hintBar, float bottomMargin = 22f)
	{
		if (hintBar == null)
			return;

		if (hintBar is ControllerHintBar controllerHintBar)
		{
			controllerHintBar.UseOverlayPlacement = true;
			controllerHintBar.OverlayBottomMargin = bottomMargin;
		}

		RefreshControllerHintOverlay(hintBar, bottomMargin);
	}

	public static void RefreshControllerHintOverlay(Control hintBar, float bottomMargin)
	{
		if (hintBar == null)
			return;

		Vector2 contentSize = hintBar.GetCombinedMinimumSize();
		Vector2 viewportSize = hintBar.IsInsideTree()
			? hintBar.GetViewportRect().Size
			: new Vector2(1280f, 720f);
		if (viewportSize.X <= 1f)
			viewportSize = new Vector2(1280f, 720f);

		float width = Mathf.Ceil(Mathf.Clamp(contentSize.X, 140f, Mathf.Max(140f, viewportSize.X - 32f)));
		float height = Mathf.Ceil(Mathf.Clamp(contentSize.Y, 34f, 58f));

		hintBar.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
		hintBar.OffsetLeft = -width * 0.5f;
		hintBar.OffsetRight = width * 0.5f;
		hintBar.OffsetTop = -bottomMargin - height;
		hintBar.OffsetBottom = -bottomMargin;
	}

	public static void UpdateControllerHintBar(ControllerHintBar bar)
	{
		if (bar == null || bar.Items == null)
			return;

		bool hasController = Input.GetConnectedJoypads().Count > 0;
		bar.Visible = hasController;

		if (!hasController)
		{
			bar.CurrentKey = "";
			return;
		}

		ControllerFamily family = GetActiveControllerFamily();
		string key = $"{bar.Mode}:{family}";

		if (bar.CurrentKey == key)
			return;

		bar.CurrentKey = key;
		ClearHintItems(bar.Items);

		foreach (ControllerHintItem item in GetControllerHintItems(bar.Mode, family))
			bar.Items.AddChild(CreateControllerHintChip(item.IconPath, item.Text));
	}

	private static void ClearHintItems(HBoxContainer items)
	{
		foreach (Node child in items.GetChildren())
		{
			items.RemoveChild(child);
			child.QueueFree();
		}
	}

	private static Control CreateControllerHintChip(string iconPath, string text)
	{
		PanelContainer chip = new PanelContainer();
		chip.MouseFilter = Control.MouseFilterEnum.Ignore;
		chip.AddThemeStyleboxOverride("panel", CreateControllerHintChipStyle());

		HBoxContainer row = new HBoxContainer();
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddThemeConstantOverride("separation", 5);
		chip.AddChild(row);

		TextureRect icon = new TextureRect();
		icon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
		icon.CustomMinimumSize = new Vector2(30f, 30f);
		icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		row.AddChild(icon);

		Label label = new Label();
		label.Text = text;
		label.ClipText = false;
		label.CustomMinimumSize = new Vector2(Mathf.Max(62f, text.Length * 8f), 0f);
		label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		ApplyLabel(label, 13, LightText);
		row.AddChild(label);

		return chip;
	}

	private static StyleBoxFlat CreateControllerHintStyle()
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(0.02f, 0.12f, 0.17f, 0.82f);
		style.BorderColor = new Color(0.7f, 0.96f, 1f, 0.58f);
		style.BorderWidthLeft = 1;
		style.BorderWidthTop = 1;
		style.BorderWidthRight = 1;
		style.BorderWidthBottom = 1;
		style.CornerRadiusTopLeft = 4;
		style.CornerRadiusTopRight = 4;
		style.CornerRadiusBottomLeft = 4;
		style.CornerRadiusBottomRight = 4;
		style.ContentMarginLeft = 10;
		style.ContentMarginTop = 6;
		style.ContentMarginRight = 10;
		style.ContentMarginBottom = 6;
		return style;
	}

	private static StyleBoxFlat CreateControllerHintChipStyle()
	{
		StyleBoxFlat style = new StyleBoxFlat();
		style.BgColor = new Color(1f, 1f, 1f, 0f);
		style.BorderColor = new Color(1f, 1f, 1f, 0f);
		style.BorderWidthLeft = 0;
		style.BorderWidthTop = 0;
		style.BorderWidthRight = 0;
		style.BorderWidthBottom = 0;
		style.ContentMarginLeft = 2;
		style.ContentMarginTop = 1;
		style.ContentMarginRight = 6;
		style.ContentMarginBottom = 1;
		return style;
	}

	private static ControllerHintItem[] GetControllerHintItems(ControllerHintMode mode, ControllerFamily family)
	{
		string dpad = family == ControllerFamily.PlayStation
			? "res://Assets/ControllerHints/ps_dpad.png"
			: "res://Assets/ControllerHints/xbox_dpad.png";
		string accept = family == ControllerFamily.PlayStation
			? "res://Assets/ControllerHints/ps_cross.png"
			: "res://Assets/ControllerHints/xbox_a.png";
		string retry = family == ControllerFamily.PlayStation
			? "res://Assets/ControllerHints/ps_square.png"
			: "res://Assets/ControllerHints/xbox_x.png";
		string cancel = family == ControllerFamily.PlayStation
			? "res://Assets/ControllerHints/ps_circle.png"
			: "res://Assets/ControllerHints/xbox_b.png";

		if (mode == ControllerHintMode.GameOver)
		{
			return new ControllerHintItem[]
			{
				new ControllerHintItem(dpad, "Navigieren"),
				new ControllerHintItem(accept, "Bestätigen"),
				new ControllerHintItem(retry, "Nochmal"),
				new ControllerHintItem(cancel, "Hauptmenü")
			};
		}

		if (mode == ControllerHintMode.BackOnly)
		{
			return new ControllerHintItem[]
			{
				new ControllerHintItem(dpad, "Navigieren"),
				new ControllerHintItem(accept, "Bestätigen"),
				new ControllerHintItem(cancel, "Zurück")
			};
		}

		return new ControllerHintItem[]
		{
			new ControllerHintItem(dpad, "Navigieren"),
			new ControllerHintItem(accept, "Bestätigen")
		};
	}

	public static bool TryClaimControllerNotice()
	{
		if (controllerNoticeShownThisRun)
			return false;

		controllerNoticeShownThisRun = true;
		return true;
	}

	public static void ResetControllerNotice()
	{
		controllerNoticeShownThisRun = false;
	}

	public static ControllerInfo GetControllerInfo(string joyName)
	{
		ControllerFamily family = GetControllerFamily(joyName);

		if (family == ControllerFamily.PlayStation &&
			IsPs4Controller(joyName))
		{
			return new ControllerInfo(
				"PS4 Controller",
				"res://Assets/Controllers/controller_ps4_pixel.png"
			);
		}

		if (family == ControllerFamily.PlayStation)
		{
			return new ControllerInfo(
				"PS5 Controller",
				"res://Assets/Controllers/controller_ps5_pixel.png"
			);
		}

		if (family == ControllerFamily.Xbox)
		{
			return new ControllerInfo(
				"Xbox Controller",
				"res://Assets/Controllers/controller_xbox_series_pixel.png"
			);
		}

		return new ControllerInfo(
			"Controller",
			"res://Assets/Controllers/controller_generic_pixel.png"
		);
	}

	public static ControllerFamily GetActiveControllerFamily()
	{
		ControllerFamily fallback = ControllerFamily.Generic;

		foreach (int device in Input.GetConnectedJoypads())
		{
			ControllerFamily family = GetControllerFamily(Input.GetJoyName(device));

			if (family != ControllerFamily.Generic)
				return family;
		}

		return fallback;
	}

	public static ControllerFamily GetControllerFamily(string joyName)
	{
		string normalized = (joyName ?? "").ToLowerInvariant();

		if (normalized.Contains("dualshock") ||
			normalized.Contains("dual shock") ||
			normalized.Contains("dualsense") ||
			normalized.Contains("dual sense") ||
			normalized.Contains("ps4") ||
			normalized.Contains("ps5") ||
			normalized.Contains("playstation"))
		{
			return ControllerFamily.PlayStation;
		}

		if (normalized.Contains("xbox") ||
			normalized.Contains("xinput") ||
			normalized.Contains("microsoft"))
		{
			return ControllerFamily.Xbox;
		}

		return ControllerFamily.Generic;
	}

	private static bool IsPs4Controller(string joyName)
	{
		string normalized = (joyName ?? "").ToLowerInvariant();
		return normalized.Contains("dualshock") ||
			normalized.Contains("dual shock") ||
			normalized.Contains("ps4");
	}

	private readonly struct ControllerHintItem
	{
		public readonly string IconPath;
		public readonly string Text;

		public ControllerHintItem(string iconPath, string text)
		{
			IconPath = iconPath;
			Text = text;
		}
	}

	public readonly struct ControllerInfo
	{
		public readonly string DisplayName;
		public readonly string TexturePath;

		public ControllerInfo(string displayName, string texturePath)
		{
			DisplayName = displayName;
			TexturePath = texturePath;
		}
	}
}

public partial class ControllerHintBar : PanelContainer
{
	public GameUi.ControllerHintMode Mode = GameUi.ControllerHintMode.Menu;
	public HBoxContainer Items;
	public string CurrentKey = "";
	public bool UseOverlayPlacement = false;
	public float OverlayBottomMargin = 22f;

	public override void _Process(double delta)
	{
		GameUi.UpdateControllerHintBar(this);

		if (UseOverlayPlacement)
			GameUi.RefreshControllerHintOverlay(this, OverlayBottomMargin);
	}
}
