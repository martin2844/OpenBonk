#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;
using UnityEngine.Networking;

// Best-effort run submission for the public OpenBonk leaderboard. The server
// is intentionally unauthenticated; this only reports the local name and the
// kill count at the end of a run.
internal sealed class OpenBonkLeaderboard : MonoBehaviour
{
    private const string BaseUrl = "https://openbonk-leaderboard.vercel.app";
    private const string NameKey = "openbonk.player_name";
    private const int MaxNameLength = 24;

    private bool runActive;
    private bool submitted;
    private float runStartedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var root = new GameObject("OpenBonk leaderboard");
        DontDestroyOnLoad(root);
        root.AddComponent<OpenBonkLeaderboard>();
    }

    private void Awake()
    {
        PlayerName();
        GameManager.A_RunStarted += OnRunStarted;
        GameManager.A_GameOver += OnGameOver;
    }

    private void OnDestroy()
    {
        GameManager.A_RunStarted -= OnRunStarted;
        GameManager.A_GameOver -= OnGameOver;
    }

    private void OnRunStarted()
    {
        runActive = true;
        submitted = false;
        runStartedAt = Time.realtimeSinceStartup;
    }

    private void OnGameOver()
    {
        if (!runActive || submitted) return;
        runActive = false;
        submitted = true;
        int score = RunStats.GetStat(EMyStat.kills);
        if (score <= 0) return;
        int duration = Mathf.Max(1, Mathf.RoundToInt(Time.realtimeSinceStartup - runStartedAt));
        string map = MapId();
        string character = CharacterId();
        bool mods = ModsEnabled();
        string name = PlayerName();
        StartCoroutine(Post("kills", name, score, duration, map, character, mods));
        StartCoroutine(Post("kills_weekly", name, score, duration, map, character, mods));
    }

    // Vanilla content is sent by enum name; mod content by its stable
    // "package:id", since mod enum values are hashes.
    private static string CharacterId()
    {
        var id = CharacterMenu.selectedCharacter;
        try
        {
            if (DataManager.Instance != null && DataManager.Instance.TryGetCharacterData(id, out var data) &&
                data is OpenBonkExternalCharacterData external && !string.IsNullOrEmpty(external.externalId))
                return external.externalId;
        }
        catch (Exception e)
        {
            Debug.LogWarning("OpenBonk leaderboard character: " + e.GetType().Name);
        }
        return Enum.IsDefined(typeof(ECharacter), id) ? id.ToString() : "";
    }

    private static string MapId()
    {
        var map = MapController.currentMap;
        if (map == null) return "";
        if (map is OpenBonkExternalMapData external && !string.IsNullOrEmpty(external.externalId)) return external.externalId;
        return map.eMap.ToString();
    }

    private static bool ModsEnabled()
    {
        try
        {
            string root = Path.Combine(Application.persistentDataPath, "Mods");
            if (Directory.Exists(root))
            {
                using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                using var prefs = activity.Call<AndroidJavaObject>("getSharedPreferences", "megabonk_mod_state", 0);
                foreach (string folder in Directory.GetDirectories(root))
                    if (prefs.Call<bool>("getBoolean", Path.GetFileName(folder), false)) return true;
            }
            return AndroidLuaMods.ListMods().Exists(mod => mod.enabled);
        }
        catch (Exception e)
        {
            Debug.LogWarning("OpenBonk leaderboard mods: " + e.GetType().Name);
            return false;
        }
    }

    private IEnumerator Post(string board, string name, int score, int duration, string map, string character, bool mods)
    {
        var payload = new ScoreRequest
        {
            board = board,
            name = name,
            score = score,
            duration = duration,
            version = Application.version,
            map = map,
            character = character,
            mods = mods
        };
        var request = new UnityWebRequest(BaseUrl + "/api/v1/scores", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("OpenBonk leaderboard " + board + ": " + request.error + " " + (request.downloadHandler != null ? request.downloadHandler.text : ""));
        else
            Debug.Log("OpenBonk leaderboard submitted " + board + ": " + score + " as " + name);
        request.Dispose();
    }

    private static string PlayerName()
    {
        try
        {
            using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity.Call<AndroidJavaObject>("getIntent");
            string fromLauncher = intent.Call<string>("getStringExtra", "openbonk_player_name");
            if (!string.IsNullOrWhiteSpace(fromLauncher))
                PlayerPrefs.SetString(NameKey, fromLauncher.Trim());
        }
        catch (Exception e)
        {
            Debug.LogWarning("OpenBonk leaderboard name: " + e.GetType().Name);
        }
        string stored = PlayerPrefs.GetString(NameKey, "Player").Trim();
        if (string.IsNullOrEmpty(stored)) stored = "Player";
        return stored.Length > MaxNameLength ? stored.Substring(0, MaxNameLength) : stored;
    }

    [Serializable]
    private sealed class ScoreRequest
    {
        public string board;
        public string name;
        public int score;
        public int duration;
        public string version;
        public string map;
        public string character;
        public bool mods;
    }
}
#endif
