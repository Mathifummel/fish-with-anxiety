using Godot;
using System.Collections.Generic;

public static class GameAudio
{
	public const string MenuMusicPath = "res://Assets/Animal Crossing New Horizons  - Main Theme Song.wav";
	public const string SambaMusicPath = "res://Assets/Samba de Amigo - Samba de Janeiro.wav";
	public const string LevelUpWarioWarePath = "res://Assets/Speed Up and Level Up - WarioWare, Inc. Mega Microgames! (OST).wav";
	public const string LevelUpTwistedPath = "res://Assets/Speed Up, Level Up - WarioWare Twisted! (OST).wav";
	public const string LevelUpDiyPath = "res://Assets/D.I.Y. Shuffle ~ Speed Up! - WarioWare D.I.Y. Soundtrack.wav";
	public const string JellyfishPath = "res://Assets/HD - SpongeBob Jellyfish Sound Effect.wav";
	public const string StressWarningPath = "res://Assets/Sounds/stress_warning_short.wav";
	public const string UiButtonPath = "res://Assets/Minecraft Menu Button Sound Effect  Sounffex.wav";
	public const string CountdownPath = "res://Assets/Friday Night Funkin - 3, 2, 1, GO! - Sound Effect (HD).wav";

	private static readonly string[] levelUpPool =
	{
		LevelUpWarioWarePath,
		LevelUpTwistedPath
	};

	private static readonly List<string> bubblePaths = new List<string>();
	private static bool bubblePathsLoaded = false;
	private static AudioStreamPlayer menuMusicPlayer;

	public static AudioStreamPlayer CreatePlayer(
		Node owner,
		string playerName,
		string streamPath,
		float volumeDb,
		bool autoplay = false
	)
	{
		AudioStream stream = LoadStream(streamPath);
		if (owner == null || stream == null)
			return null;

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Name = playerName,
			Stream = stream,
			VolumeDb = volumeDb
		};

		owner.AddChild(player);

		if (autoplay)
			player.Play();

		return player;
	}

	public static AudioStreamPlayer CreateLoopPlayer(
		Node owner,
		string playerName,
		string streamPath,
		float volumeDb,
		float loopFrom = 0f,
		bool autoplay = true
	)
	{
		AudioStream stream = LoadStream(streamPath);
		if (owner == null || stream == null)
			return null;

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Name = playerName,
			Stream = stream,
			VolumeDb = volumeDb
		};

		owner.AddChild(player);
		player.Finished += () =>
		{
			if (!player.IsQueuedForDeletion())
				player.Play(loopFrom);
		};

		if (autoplay)
			player.Play(loopFrom);

		return player;
	}

	public static void PlayOneShot(
		Node context,
		string streamPath,
		float volumeDb = -6f,
		float pitchScale = 1f,
		float fromPosition = 0f
	)
	{
		AudioStream stream = LoadStream(streamPath);
		Node parent = GetPersistentAudioParent(context);

		if (stream == null || parent == null)
			return;

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitchScale
		};

		parent.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play(fromPosition);
	}

	public static void PlaySpatialOneShot(
		Node context,
		string streamPath,
		Vector2 globalPosition,
		float volumeDb = -10f,
		float pitchScale = 1f,
		float maxDistance = 980f
	)
	{
		AudioStream stream = LoadStream(streamPath);
		Node parent = GetSceneAudioParent(context);

		if (stream == null || parent == null)
			return;

		AudioStreamPlayer2D player = new AudioStreamPlayer2D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitchScale,
			GlobalPosition = globalPosition,
			MaxDistance = maxDistance,
			Attenuation = 0.9f
		};

		parent.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	public static void PlayRandomBubble(
		Node context,
		Vector2 globalPosition,
		float volumeDb = -14f,
		float pitchScale = 1f
	)
	{
		string path = GetRandomBubblePath();

		if (string.IsNullOrEmpty(path))
			return;

		PlaySpatialOneShot(context, path, globalPosition, volumeDb, pitchScale, 820f);
	}

	public static void PlayLevelUp(Node context, int level)
	{
		string path = level >= 5
			? LevelUpDiyPath
			: levelUpPool[(int)(GD.Randi() % levelUpPool.Length)];

		PlayOneShot(context, path, -3.5f, 1f);
	}

	public static void PlayItemPickup(Node context, ItemType type, Vector2 globalPosition)
	{
		switch (type)
		{
			case ItemType.Alcohol:
				PlayRandomBubble(context, globalPosition, -5f, 0.86f);
				break;
			case ItemType.ChorusFruit:
				PlayRandomBubble(context, globalPosition, -6f, 1.24f);
				break;
			case ItemType.Trash:
				PlaySpatialOneShot(context, StressWarningPath, globalPosition, -12f, 0.96f, 760f);
				break;
		}
	}

	public static void FadeLoopVolume(AudioStreamPlayer player, float targetDb, float dt, float speedDbPerSecond = 28f)
	{
		if (player == null)
			return;

		player.VolumeDb = Mathf.MoveToward(player.VolumeDb, targetDb, speedDbPerSecond * dt);
	}

	public static void StopPlayer(AudioStreamPlayer player)
	{
		if (player != null && player.Playing)
			player.Stop();
	}

	public static void EnsureMenuMusic(Node context)
	{
		SceneTree tree = context?.GetTree();
		Node root = tree?.Root;

		if (root == null)
			return;

		if (menuMusicPlayer != null && GodotObject.IsInstanceValid(menuMusicPlayer))
		{
			if (!menuMusicPlayer.Playing)
				menuMusicPlayer.Play();

			menuMusicPlayer.VolumeDb = -13f;
			return;
		}

		menuMusicPlayer = CreateLoopPlayer(root, "PersistentMenuMusic", MenuMusicPath, -13f);
	}

	public static void StopMenuMusic(Node context)
	{
		if (menuMusicPlayer == null || !GodotObject.IsInstanceValid(menuMusicPlayer))
			return;

		menuMusicPlayer.Stop();
	}

	private static AudioStream LoadStream(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
		{
			GD.PushWarning($"Audio stream missing: {path}");
			return null;
		}

		return ResourceLoader.Load<AudioStream>(path);
	}

	private static string GetRandomBubblePath()
	{
		EnsureBubblePaths();

		if (bubblePaths.Count == 0)
			return "";

		return bubblePaths[(int)(GD.Randi() % bubblePaths.Count)];
	}

	private static void EnsureBubblePaths()
	{
		if (bubblePathsLoaded)
			return;

		bubblePathsLoaded = true;

		for (int i = 1; i <= 60; i++)
		{
			string path = $"res://Assets/Sounds/Bubbles/bubble_{i:00}.wav";
			if (ResourceLoader.Exists(path))
				bubblePaths.Add(path);
		}
	}

	private static Node GetPersistentAudioParent(Node context)
	{
		SceneTree tree = context?.GetTree();
		return tree?.Root;
	}

	private static Node GetSceneAudioParent(Node context)
	{
		SceneTree tree = context?.GetTree();
		return tree?.CurrentScene ?? tree?.Root;
	}
}
