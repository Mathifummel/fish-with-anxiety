using Godot;
using System.Collections.Generic;

public static class GameAudio
{
	public const string MenuMusicPath = "res://Assets/Animal Crossing New Horizons  - Main Theme Song.wav";
	public const string GameplayMusicPath = "res://Assets/Aquarium Park Act 1 - Sonic Colors (DS).wav";
	public const string SambaMusicPath = "res://Assets/Sounds/samba_loud_start.wav";
	public const string ChorusFruitTeleportPath = "res://Assets/Sonic Checkpoint SFX.wav";
	public const string SpinDashPath = "res://Assets/Sonic Spin Dash - Sound Effect.wav";
	public const string LevelUpWarioWarePath = "res://Assets/Speed Up and Level Up - WarioWare, Inc. Mega Microgames! (OST).wav";
	public const string LevelUpTwistedPath = "res://Assets/Speed Up, Level Up - WarioWare Twisted! (OST).wav";
	public const string LevelUpDiyPath = "res://Assets/D.I.Y. Shuffle ~ Speed Up! - WarioWare D.I.Y. Soundtrack.wav";
	public const string JellyfishPath = "res://Assets/HD - SpongeBob Jellyfish Sound Effect.wav";
	public const string StressWarningPath = "res://Assets/Sounds/stress_warning_short.wav";
	public const string UiButtonPath = "res://Assets/Minecraft Menu Button Sound Effect  Sounffex.wav";
	public const string CountdownPath = "res://Assets/Friday Night Funkin - 3, 2, 1, GO! - Sound Effect (HD).wav";
	public const string MusicBusName = "Music";
	public const string SfxBusName = "SFX";

	private const string AudioSettingsPath = "user://audio_settings.cfg";
	private const string AudioSection = "audio";
	private const float DefaultMusicVolume = 0.74f;
	private const float DefaultSfxVolume = 0.86f;
	private const float SpinDashSecondHalfStart = 1.835f;

	private static readonly string[] levelUpPool =
	{
		LevelUpWarioWarePath,
		LevelUpTwistedPath
	};

	private static readonly List<string> bubblePaths = new List<string>();
	private static bool bubblePathsLoaded = false;
	private static bool audioSettingsLoaded = false;
	private static float musicVolume = DefaultMusicVolume;
	private static float sfxVolume = DefaultSfxVolume;
	private static AudioStreamPlayer menuMusicPlayer;
	private static AudioStreamPlayer gameplayMusicPlayer;

	public static float MusicVolume
	{
		get
		{
			EnsureAudioReady();
			return musicVolume;
		}
	}

	public static float SfxVolume
	{
		get
		{
			EnsureAudioReady();
			return sfxVolume;
		}
	}

	public static AudioStreamPlayer CreatePlayer(
		Node owner,
		string playerName,
		string streamPath,
		float volumeDb,
		bool autoplay = false,
		bool isMusic = false
	)
	{
		EnsureAudioReady();
		AudioStream stream = LoadStream(streamPath);
		if (owner == null || stream == null)
			return null;

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Name = playerName,
			Stream = stream,
			VolumeDb = volumeDb,
			Bus = isMusic ? MusicBusName : SfxBusName
		};

		AttachPlayer(owner, player, autoplay, 0f);

		return player;
	}

	public static AudioStreamPlayer CreateLoopPlayer(
		Node owner,
		string playerName,
		string streamPath,
		float volumeDb,
		float loopFrom = 0f,
		bool autoplay = true,
		bool isMusic = false
	)
	{
		EnsureAudioReady();
		AudioStream stream = LoadStream(streamPath);
		if (owner == null || stream == null)
			return null;

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Name = playerName,
			Stream = stream,
			VolumeDb = volumeDb,
			Bus = isMusic ? MusicBusName : SfxBusName
		};

		player.Finished += () =>
		{
			if (!player.IsQueuedForDeletion() && player.IsInsideTree())
				player.Play(loopFrom);
		};

		AttachPlayer(owner, player, autoplay, loopFrom);

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
		EnsureAudioReady();
		AudioStream stream = LoadStream(streamPath);
		Node parent = GetPersistentAudioParent(context);

		if (stream == null || parent == null)
			return;

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitchScale,
			Bus = SfxBusName
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
		EnsureAudioReady();
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
			Attenuation = 0.9f,
			Bus = SfxBusName
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

	public static void PlayCountdown(Node context)
	{
		PlayOneShot(context, CountdownPath, -5f, 1.32f);
	}

	public static void PlayBoost(Node context)
	{
		PlayOneShot(context, SpinDashPath, -7f, 1f, SpinDashSecondHalfStart);
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

	public static void UpdateStressWarningLoop(
		AudioStreamPlayer player,
		bool active,
		float pressure,
		float dt,
		float quietDb = -19f,
		float loudDb = -7.5f
	)
	{
		if (player == null || !player.IsInsideTree())
			return;

		pressure = Mathf.Clamp(pressure, 0f, 1f);

		if (!active)
		{
			if (!player.Playing)
				return;

			player.VolumeDb = Mathf.MoveToward(player.VolumeDb, -52f, 42f * dt);
			player.PitchScale = Mathf.MoveToward(player.PitchScale, 0.9f, 0.65f * dt);

			if (player.VolumeDb <= -48f)
				player.Stop();

			return;
		}

		if (!player.Playing)
		{
			player.VolumeDb = quietDb - 8f;
			player.PitchScale = 0.9f;
			player.Play();
		}

		float targetVolume = Mathf.Lerp(quietDb, loudDb, pressure);
		float targetPitch = Mathf.Lerp(0.9f, 1.08f, pressure);

		player.VolumeDb = Mathf.MoveToward(player.VolumeDb, targetVolume, 34f * dt);
		player.PitchScale = Mathf.MoveToward(player.PitchScale, targetPitch, 0.55f * dt);
	}

	public static void StopPlayer(AudioStreamPlayer player)
	{
		if (player != null && player.Playing)
			player.Stop();
	}

	public static void EnsureMenuMusic(Node context)
	{
		EnsureAudioReady();
		StopGameplayMusic(context);

		SceneTree tree = context?.GetTree();
		Node root = tree?.Root;

		if (root == null)
			return;

		if (menuMusicPlayer != null && GodotObject.IsInstanceValid(menuMusicPlayer))
		{
			if (!menuMusicPlayer.IsInsideTree())
				return;

			if (!menuMusicPlayer.Playing)
				menuMusicPlayer.Play();

			menuMusicPlayer.VolumeDb = -13f;
			return;
		}

		menuMusicPlayer = CreateLoopPlayer(root, "PersistentMenuMusic", MenuMusicPath, -13f, 0f, true, true);
	}

	public static void StopMenuMusic(Node context)
	{
		if (menuMusicPlayer == null || !GodotObject.IsInstanceValid(menuMusicPlayer))
			return;

		menuMusicPlayer.Stop();
	}

	public static void EnsureGameplayMusic(Node context)
	{
		EnsureAudioReady();
		StopMenuMusic(context);

		SceneTree tree = context?.GetTree();
		Node root = tree?.Root;

		if (root == null)
			return;

		if (gameplayMusicPlayer != null && GodotObject.IsInstanceValid(gameplayMusicPlayer))
		{
			if (!gameplayMusicPlayer.IsInsideTree())
				return;

			if (!gameplayMusicPlayer.Playing)
				gameplayMusicPlayer.Play();

			gameplayMusicPlayer.VolumeDb = -15f;
			return;
		}

		gameplayMusicPlayer = CreateLoopPlayer(root, "PersistentGameplayMusic", GameplayMusicPath, -15f, 0f, true, true);
	}

	public static void StopGameplayMusic(Node context)
	{
		if (gameplayMusicPlayer == null || !GodotObject.IsInstanceValid(gameplayMusicPlayer))
			return;

		gameplayMusicPlayer.Stop();
	}

	public static void SetMusicVolume(float value)
	{
		EnsureAudioReady();
		musicVolume = Mathf.Clamp(value, 0f, 1f);
		ApplyBusVolumes();
		SaveAudioSettings();
	}

	public static void SetSfxVolume(float value)
	{
		EnsureAudioReady();
		sfxVolume = Mathf.Clamp(value, 0f, 1f);
		ApplyBusVolumes();
		SaveAudioSettings();
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

	private static void EnsureAudioReady()
	{
		EnsureAudioBuses();
		LoadAudioSettings();
		ApplyBusVolumes();
	}

	private static void EnsureAudioBuses()
	{
		EnsureBus(MusicBusName);
		EnsureBus(SfxBusName);
	}

	private static void EnsureBus(string busName)
	{
		if (AudioServer.GetBusIndex(busName) >= 0)
			return;

		AudioServer.AddBus();
		int busIndex = AudioServer.GetBusCount() - 1;
		AudioServer.SetBusName(busIndex, busName);
	}

	private static void LoadAudioSettings()
	{
		if (audioSettingsLoaded)
			return;

		audioSettingsLoaded = true;
		ConfigFile config = new ConfigFile();

		if (config.Load(AudioSettingsPath) != Error.Ok)
			return;

		musicVolume = Mathf.Clamp((float)config.GetValue(AudioSection, "music", DefaultMusicVolume).AsDouble(), 0f, 1f);
		sfxVolume = Mathf.Clamp((float)config.GetValue(AudioSection, "sfx", DefaultSfxVolume).AsDouble(), 0f, 1f);
	}

	private static void SaveAudioSettings()
	{
		ConfigFile config = new ConfigFile();
		config.SetValue(AudioSection, "music", musicVolume);
		config.SetValue(AudioSection, "sfx", sfxVolume);
		config.Save(AudioSettingsPath);
	}

	private static void ApplyBusVolumes()
	{
		SetBusVolume(MusicBusName, musicVolume);
		SetBusVolume(SfxBusName, sfxVolume);
	}

	private static void SetBusVolume(string busName, float linearVolume)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex < 0)
			return;

		float db = linearVolume <= 0.001f ? -80f : Mathf.LinearToDb(linearVolume);
		AudioServer.SetBusVolumeDb(busIndex, db);
		AudioServer.SetBusMute(busIndex, linearVolume <= 0.001f);
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

	private static void AttachPlayer(Node owner, AudioStreamPlayer player, bool autoplay, float startPosition)
	{
		if (owner == null || player == null)
			return;

		if (autoplay)
		{
			player.TreeEntered += () =>
			{
				if (!player.IsQueuedForDeletion() && !player.Playing)
					player.Play(startPosition);
			};
		}

		bool deferAdd = !owner.IsInsideTree() || owner == owner.GetTree()?.Root;
		if (deferAdd)
			owner.CallDeferred(Node.MethodName.AddChild, player);
		else
			owner.AddChild(player);

		if (autoplay && player.IsInsideTree() && !player.Playing)
			player.Play(startPosition);
	}

	private static Node GetSceneAudioParent(Node context)
	{
		SceneTree tree = context?.GetTree();
		return tree?.CurrentScene ?? tree?.Root;
	}
}
