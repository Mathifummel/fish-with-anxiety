using Godot;

public static class GameUi
{
	private static Font pixelFont;

	public static readonly Color PanelBackground = new Color(1f, 1f, 1f, 0.24f);
	public static readonly Color PanelBorder = new Color(1f, 1f, 1f, 0.62f);
	public static readonly Color ButtonNormal = new Color(1f, 1f, 1f, 0.62f);
	public static readonly Color ButtonHover = new Color(1f, 1f, 1f, 0.82f);
	public static readonly Color ButtonPressed = new Color(0.84f, 0.96f, 1f, 0.86f);
	public static readonly Color ButtonFocus = new Color(1f, 1f, 1f, 0.86f);
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
		Color normal = selected ? new Color(0.82f, 0.96f, 1f, 0.86f) : ButtonNormal;
		Color hover = selected ? new Color(1f, 1f, 1f, 0.9f) : ButtonHover;
		Color pressed = selected ? new Color(0.72f, 0.92f, 1f, 0.92f) : ButtonPressed;

		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(normal, ButtonBorder));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(hover, ButtonHoverBorder));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(pressed, new Color(1f, 1f, 1f, 0.98f)));
		button.AddThemeStyleboxOverride("focus", CreateButtonStyle(ButtonFocus, new Color(0.65f, 0.9f, 1f, 1f)));
		button.AddThemeColorOverride("font_color", selected ? new Color(0.02f, 0.14f, 0.24f) : DarkText);
		button.AddThemeColorOverride("font_hover_color", new Color(0.02f, 0.16f, 0.28f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.02f, 0.14f, 0.24f));
		ApplyFont(button, fontSize);
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
}
