using Godot;
using System.Collections.Generic;

public enum StartItemKind
{
	ExtraHeart,
	Alcohol,
	ChorusFruit
}

public sealed class SkinDefinition
{
	public readonly string Id;
	public readonly string DisplayName;
	public readonly string PackName;
	public readonly string Frame1Path;
	public readonly string Frame2Path;
	public readonly int Price;

	public SkinDefinition(string id, string displayName, string packName, string frame1Path, string frame2Path, int price)
	{
		Id = id;
		DisplayName = displayName;
		PackName = packName;
		Frame1Path = frame1Path;
		Frame2Path = frame2Path;
		Price = price;
	}
}

public sealed class StartItemDefinition
{
	public readonly StartItemKind Kind;
	public readonly string DisplayName;
	public readonly string Description;
	public readonly string IconPath;
	public readonly int Price;

	public StartItemDefinition(StartItemKind kind, string displayName, string description, string iconPath, int price)
	{
		Kind = kind;
		DisplayName = displayName;
		Description = description;
		IconPath = iconPath;
		Price = price;
	}
}

public static class ShopCatalog
{
	public const string DefaultSkinId = "default";
	public const int SkinPrice = 750;
	public const int StartItemPrice = 100;

	private static readonly SkinDefinition[] skins = BuildSkins();
	private static readonly StartItemDefinition[] startItems =
	{
		new StartItemDefinition(
			StartItemKind.ExtraHeart,
			"Extra Herz",
			"Startet die naechste Runde mit einem zweiten Leben.",
			"res://Assets/Herz.png",
			StartItemPrice
		),
		new StartItemDefinition(
			StartItemKind.Alcohol,
			"Alkohol",
			"Ein Start-Item fuer kurze Unverwundbarkeit.",
			"res://Assets/Alkohol.png",
			StartItemPrice
		),
		new StartItemDefinition(
			StartItemKind.ChorusFruit,
			"Chorusfrucht",
			"Ein Start-Item fuer einen Notfall-Teleport.",
			"res://Assets/Chorusfrucht.png",
			StartItemPrice
		)
	};

	public static IReadOnlyList<SkinDefinition> Skins => skins;
	public static IReadOnlyList<StartItemDefinition> StartItems => startItems;

	public static SkinDefinition GetSkin(string id)
	{
		foreach (SkinDefinition skin in skins)
		{
			if (skin.Id == id)
				return skin;
		}

		return skins[0];
	}

	public static bool IsKnownSkin(string id)
	{
		foreach (SkinDefinition skin in skins)
		{
			if (skin.Id == id)
				return true;
		}

		return false;
	}

	public static StartItemDefinition GetStartItem(StartItemKind kind)
	{
		foreach (StartItemDefinition item in startItems)
		{
			if (item.Kind == kind)
				return item;
		}

		return startItems[0];
	}

	private static SkinDefinition[] BuildSkins()
	{
		List<SkinDefinition> result = new List<SkinDefinition>
		{
			new SkinDefinition(
				DefaultSkinId,
				"Standard",
				"Standard",
				"res://Assets/Fisch_1.png",
				"res://Assets/Fisch_2.png",
				0
			)
		};

		AddPack(result, "skinpack1", "Skinpack 1", 8);
		AddPack(result, "skinpack2", "Skinpack 2", 8);
		return result.ToArray();
	}

	private static void AddPack(List<SkinDefinition> result, string prefix, string packName, int count)
	{
		for (int i = 1; i <= count; i++)
		{
			string number = i.ToString("00");
			string id = $"{prefix}_{number}";
			result.Add(
				new SkinDefinition(
					id,
					$"{packName} #{i}",
					packName,
					$"res://Assets/Generated/Skins/{id}_frame1.png",
					$"res://Assets/Generated/Skins/{id}_frame2.png",
					SkinPrice
				)
			);
		}
	}
}
