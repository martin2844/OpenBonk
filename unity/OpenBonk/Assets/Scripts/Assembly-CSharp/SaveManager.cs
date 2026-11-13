using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
	private static bool USE_ENCRYPTION = true;

	private static bool TEST_SAVES;

	private static int settingsVersion = 1;

	private static ulong steamIdSave = 69;

	private const string configName = "config.json";

	private const string statsName = "stats.json";

	private const string progressionName = "progression.json";

	public const string controllersName = "controller_config.json";

	public ConfigSaveFile config;

	public StatsSaveFile stats;

	public ProgressionSaveFile progression;

	public static Action A_SavesLoaded;

	public static Action A_ProgressionSaved;

	public static Action A_StatsSaved;

	private static string cloudDirectory = "CloudDir";

	private static string localDirectory = "LocalDir";

	private static string defaultSavesPath = "Saves";

	private static string testingSavesPath = "Saves_For_Testing";

	private static string TEST_SAVES_VERSION = "(1.0.5)";

	private const string lastSteamIdKey = "saves_last_steamid";

	public static bool loaded;

	private bool usingNoSave;
	private readonly HashSet<string> recoveredBackupPaths = new HashSet<string>();
	private ProgressionSaveFile initializedProgression;

	public static SaveManager Instance { get; private set; }

	public void Init()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
	}

	private void Awake()
	{
		Init();
	}

	private void OnDestroy()
	{
		if (Instance == this) (initializedProgression ?? progression)?.OnDestroy();
	}

	private void OnApplicationQuit()
	{
		SaveConfig();
		SaveStats();
		SaveProgression();
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	private void OnApplicationPause(bool paused)
	{
		if (!paused || Instance != this) return;
		SaveConfig();
		SaveStats();
		SaveProgression();
	}
#endif

	public void SaveConfig()
	{
		if (config == null || !loaded || usingNoSave) return;
		SaveTemp(JsonConvert.SerializeObject(config, Formatting.Indented), GetConfigPath(), false);
	}

	public void SaveStats()
	{
		if (stats == null || !loaded || usingNoSave) return;
		if (SaveTemp(JsonConvert.SerializeObject(stats, Formatting.Indented), GetStatsPath(), true))
			A_StatsSaved?.Invoke();
	}

	public void SaveProgression()
	{
		if (progression == null || !loaded || usingNoSave) return;
		if (SaveTemp(JsonConvert.SerializeObject(progression, Formatting.Indented), GetProgressionPath(), true))
			A_ProgressionSaved?.Invoke();
	}

	private bool SaveTemp(string serializedJson, string filePath, bool encrypt)
	{
		try
		{
			if (encrypt)
			{
				serializedJson = SimpleEncryptor.Encrypt(serializedJson);
				if (string.IsNullOrEmpty(serializedJson)) throw new Exception("Encryption failed");
			}
			string temp = filePath + ".tmp";
			string backup = filePath + ".bak";
			using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(serializedJson);
				writer.Flush();
				stream.Flush(true);
			}
			if (File.Exists(filePath)) File.Replace(temp, filePath, recoveredBackupPaths.Contains(filePath) ? null : backup);
			else File.Move(temp, filePath);
			recoveredBackupPaths.Remove(filePath);
			return true;
		}
		catch (Exception e) { Debug.LogError($"Failed to save {filePath}: {e}"); return false; }
	}

	private static string GetDataPath()
	{
		return Path.Combine(Application.persistentDataPath, TEST_SAVES ? testingSavesPath + " " + TEST_SAVES_VERSION : defaultSavesPath);
	}

	private static string GetDataPathDefault()
	{
		return Path.Combine(Application.persistentDataPath, defaultSavesPath);
	}

	private static string GetCloudFolder()
	{
		return Path.Combine(GetDataPath(), cloudDirectory);
	}

	private static string GetLocalFolder()
	{
		return Path.Combine(GetDataPath(), localDirectory);
	}

	private string GetConfigPath()
	{
		return Path.Combine(GetLocalFolder(), configName);
	}

	private string GetStatsPath()
	{
		return Path.Combine(GetCloudFolder(), steamIdSave.ToString(), statsName);
	}

	private string GetProgressionPath()
	{
		return Path.Combine(GetCloudFolder(), steamIdSave.ToString(), progressionName);
	}

	public static string GetControllersPath()
	{
		return Path.Combine(GetCloudFolder(), steamIdSave.ToString(), controllersName);
	}

	public static string GetControllersDir()
	{
		return Path.Combine(GetCloudFolder(), steamIdSave.ToString());
	}

	public void Load(bool loadBackup)
	{
		if (Load(SteamManager.steamId, loadBackup, out string reason, out string path)) return;
		Debug.LogError(reason);
		if (AlwaysManager.Instance != null && AlwaysManager.Instance.alwaysUi != null && AlwaysManager.Instance.alwaysUi.configWarning != null)
			AlwaysManager.Instance.alwaysUi.configWarning.ShowWarning(reason, path);
	}

	public void LoadNoSave()
	{
		usingNoSave = true;
		loaded = false;
		config = new ConfigSaveFile();
		progression = new ProgressionSaveFile();
		stats = new StatsSaveFile();
		config.Init();
		stats.Init();
		progression.Init();
		initializedProgression?.OnDestroy();
		initializedProgression = progression;
		loaded = true;
		A_SavesLoaded?.Invoke();
	}

	public bool Load(ulong steamId, bool loadBackup, out string failReason, out string failPath)
	{
		Init();
		failReason = string.Empty;
		failPath = GetCloudFolder();
		if (steamIdSave == steamId && loaded)
		{
			Debug.LogError("Tried to load more than once?");
			return false;
		}
		try
		{
			if (steamId == 0 && PlayerPrefs.HasKey(lastSteamIdKey))
				steamId = ulong.Parse(PlayerPrefs.GetString(lastSteamIdKey));
			if (steamId != 0) PlayerPrefs.SetString(lastSteamIdKey, steamId.ToString());
			steamIdSave = steamId;
		}
		catch (Exception e)
		{
			failReason = "Failed to load last steam id: " + e.Message;
			return false;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
        // Never replace a profile already loaded by this process.
        if (!loaded)
        {
            try { AndroidProfileImport.Prepare(GetCloudFolder(), steamIdSave.ToString(), !loadBackup); }
            catch (Exception e)
            {
                failPath = GetCloudFolder();
                failReason = "Failed to recover profile before loading: " + e.Message;
                return false;
            }
        }
#endif

		string configPath = GetConfigPath();
		string statsPath = GetStatsPath();
		string progressionPath = GetProgressionPath();
		// When a particular backup does not exist, retain its primary file. A
		// missing backup must never silently replace an existing save with defaults.
		string configReadPath = loadBackup && File.Exists(configPath + ".bak") ? configPath + ".bak" : configPath;
		string statsReadPath = loadBackup && File.Exists(statsPath + ".bak") ? statsPath + ".bak" : statsPath;
		string progressionReadPath = loadBackup && File.Exists(progressionPath + ".bak") ? progressionPath + ".bak" : progressionPath;
		try
		{
			if (TEST_SAVES && !Directory.Exists(GetDataPath()) && Directory.Exists(GetDataPathDefault()))
				CopyDirectory(GetDataPathDefault(), GetDataPath(), true);
			Directory.CreateDirectory(GetCloudFolder());
			Directory.CreateDirectory(GetLocalFolder());
			Directory.CreateDirectory(GetControllersDir());
		}
		catch (Exception e)
		{
			failReason = "Failed to create directories: " + e.Message;
			return false;
		}

		try
		{
			if (File.Exists(configReadPath))
				config = JsonConvert.DeserializeObject<ConfigSaveFile>(File.ReadAllText(configReadPath)) ?? throw new InvalidDataException("Config file is empty or null");
			else { config = new ConfigSaveFile(); SaveConfig(); }
#if UNITY_ANDROID && !UNITY_EDITOR
			// Early Android builds wrote zero-filled settings before the original
			// constructors were recovered. A zero FOV leaves the camera blind.
			// Recognize the other zero-filled defaults before repairing the HUD.
			if (config?.cfGameSettings != null && config.cfGameSettings.show_hud == 0 &&
				config.cfGameSettings.minimap_size == 0f && config.cfGameSettings.camera_shake == 0 &&
				config.cfGameSettings.input_delay == 0f)
			{
				config.cfGameSettings.show_hud = 1;
				config.cfGameSettings.minimap_size = 1.35f;
			}
			if (config?.cfVideoSettings != null &&
				(float.IsNaN(config.cfVideoSettings.fov) || config.cfVideoSettings.fov <= 0f || config.cfVideoSettings.fov >= 180f))
				config.cfVideoSettings.fov = new Assets.Scripts.Settings___Saves.SaveFiles.CFVideoSettings().fov;
#endif
		}
		catch (Exception e)
		{
			failPath = configReadPath;
			failReason = "Failed to load config file: " + e.Message;
			return false;
		}
		try
		{
			if (File.Exists(statsReadPath))
			{
				string json = File.ReadAllText(statsReadPath);
				if (USE_ENCRYPTION || !Application.isEditor) json = SimpleEncryptor.Decrypt(json);
				stats = JsonConvert.DeserializeObject<StatsSaveFile>(json) ?? throw new InvalidDataException("Stats file is empty or null");
			}
			else { stats = new StatsSaveFile(); SaveStats(); }
		}
		catch (Exception e)
		{
			failPath = statsReadPath;
			failReason = "Failed to load stats file: " + e.Message;
			return false;
		}
		try
		{
			if (File.Exists(progressionReadPath))
			{
				string json = File.ReadAllText(progressionReadPath);
				if (USE_ENCRYPTION || !Application.isEditor) json = SimpleEncryptor.Decrypt(json);
				progression = JsonConvert.DeserializeObject<ProgressionSaveFile>(json) ?? throw new InvalidDataException("Progression file is empty or null");
			}
			else { progression = new ProgressionSaveFile(); SaveProgression(); }
		}
		catch (Exception e)
		{
			failPath = progressionReadPath;
			failReason = "Failed to load progression file: " + e.Message;
			return false;
		}
		try { config.Init(); }
		catch (Exception e) { failPath = configReadPath; failReason = "Failed to initialize config: " + e.Message; return false; }
		try { stats.Init(); }
		catch (Exception e) { failPath = statsReadPath; failReason = "Failed to initialize stats: " + e.Message; return false; }
		try { progression.Init(); }
		catch (Exception e) { progression.OnDestroy(); failPath = progressionReadPath; failReason = "Failed to initialize progression: " + e.Message; return false; }
		initializedProgression?.OnDestroy();
		initializedProgression = progression;
		recoveredBackupPaths.Clear();
		if (configReadPath != configPath) recoveredBackupPaths.Add(configPath);
		if (statsReadPath != statsPath) recoveredBackupPaths.Add(statsPath);
		if (progressionReadPath != progressionPath) recoveredBackupPaths.Add(progressionPath);
		loaded = true;
		usingNoSave = false;
		A_SavesLoaded?.Invoke();
		return true;
	}

	public void NewSaveConfig()
	{
		config = new ConfigSaveFile();
		config.Init();
	}

	public void ResetAll()
	{
		config = new ConfigSaveFile();
		config.Init();
		initializedProgression?.OnDestroy();
		progression = new ProgressionSaveFile();
		progression.Init();
		initializedProgression = progression;
		stats = new StatsSaveFile();
		stats.Init();
	}

	private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true)
	{
		var source = new DirectoryInfo(sourceDir);
		if (!source.Exists) throw new DirectoryNotFoundException(sourceDir);
		Directory.CreateDirectory(destinationDir);
		foreach (FileInfo file in source.GetFiles()) file.CopyTo(Path.Combine(destinationDir, file.Name));
		if (recursive)
			foreach (DirectoryInfo child in source.GetDirectories())
				CopyDirectory(child.FullName, Path.Combine(destinationDir, child.Name), true);
	}

	public void SaveControllers()
	{
	}

	public void LoadControllers()
	{
	}
}
