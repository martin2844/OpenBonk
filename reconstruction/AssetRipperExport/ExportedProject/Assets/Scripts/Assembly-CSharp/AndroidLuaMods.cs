#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// An explicitly enabled package can supply one supported stat multiplier.
// The runtime is reloaded when a player is initialized.
internal static class AndroidLuaMods
{
    [Serializable] private sealed class Manifest
    {
        public string id;
        public int api;
        public bool enabled;
        public string hook, refresh;
        public string[] hooks;
    }

    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_eval(byte[] source, out double value, byte[] error, UIntPtr errorSize);

    [DllImport("megabonk_lua", CallingConvention = CallingConvention.Cdecl)]
    private static extern int megabonk_lua_eval_values(byte[] source, int count, [Out] double[] values, byte[] error, UIntPtr errorSize);

    private static PlayerInventory lastInventory;
    private static readonly List<string> activeKeys = new List<string>();
    private sealed class RefreshMod { internal string name, source; internal Manifest manifest; internal EStat[] stats; }
    private static readonly List<RefreshMod> refreshMods = new List<RefreshMod>();
    private const long FileLimit = 65536;
    internal sealed class ModInfo { internal string id; internal bool enabled; }

    private static string PreferenceKey(string id) => "megabonk.lua.enabled." + Convert.ToBase64String(Encoding.UTF8.GetBytes(id));
    private static AndroidJavaObject ModPreferences()
    {
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            return activity.Call<AndroidJavaObject>("getSharedPreferences", "megabonk_mod_state", 0);
    }

    private static bool Enabled(Manifest manifest)
    {
        using (var prefs = ModPreferences())
        {
            if (prefs.Call<bool>("contains", manifest.id)) return prefs.Call<bool>("getBoolean", manifest.id, false);
            // Preserve choices made by the first experimental in-game manager.
            if (PlayerPrefs.HasKey(PreferenceKey(manifest.id)))
            {
                bool enabled = PlayerPrefs.GetInt(PreferenceKey(manifest.id)) != 0;
                SetEnabled(manifest.id, enabled);
                return enabled;
            }
            return manifest.enabled;
        }
    }

    internal static void SetEnabled(string id, bool enabled)
    {
        using (var prefs = ModPreferences())
        using (var editor = prefs.Call<AndroidJavaObject>("edit"))
        {
            editor.Call<AndroidJavaObject>("putBoolean", id, enabled).Dispose();
            if (!editor.Call<bool>("commit")) throw new IOException("Could not save mod choice");
        }
    }

    internal static List<ModInfo> ListMods()
    {
        var result = new List<ModInfo>();
        string root = Path.Combine(Application.persistentDataPath, "Mods");
        if (!Directory.Exists(root)) return result;
        try
        {
            string[] folders = Directory.GetDirectories(root);
            Array.Sort(folders, StringComparer.Ordinal);
            for (int i = 0; i < folders.Length; i++)
            {
                try
                {
                    string file = Path.Combine(folders[i], "manifest.json");
                    if (!File.Exists(file) || new FileInfo(file).Length > FileLimit) continue;
                    var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(file));
                    if (manifest == null || !ValidRefresh(manifest) || manifest.id != Path.GetFileName(folders[i]) || !AndroidModHooks.TryGetStats(manifest.api, manifest.hook, manifest.hooks, out _)) continue;
                    result.Add(new ModInfo { id = manifest.id, enabled = Enabled(manifest) });
                }
                catch (Exception e) { Debug.LogWarning("Lua mods: cannot read manifest: " + e.Message); }
            }
        }
        catch (Exception e) { Debug.LogWarning("Lua mods: cannot list packages: " + e.Message); }
        return result;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        MyPlayer.A_PlayerInventoryInitialized -= Apply;
        MyPlayer.A_PlayerInventoryInitialized += Apply;
    }

    private static void Apply(PlayerInventory inventory)
    {
        if (inventory?.statInventory == null || inventory.playerStats == null) return;
        PlayerXp.A_LevelUp -= RefreshAtLevel;
        bool sameInventory = ReferenceEquals(lastInventory, inventory);
        var previousRefresh = new Dictionary<string, RefreshMod>(StringComparer.Ordinal);
        var previousValues = new Dictionary<string, StatModifier>(StringComparer.Ordinal);
        if (sameInventory)
        {
            foreach (var mod in refreshMods) previousRefresh[mod.name] = mod;
            foreach (string key in activeKeys)
                if (inventory.statInventory.movingStats.TryGetValue(key, out var value)) previousValues[key] = value;
        }
        refreshMods.Clear();
        bool removed = sameInventory && activeKeys.Count != 0;
        if (removed)
            foreach (string key in activeKeys) inventory.statInventory.RemoveMovingStat(key);
        activeKeys.Clear();
        lastInventory = inventory;
        string root = Path.Combine(Application.persistentDataPath, "Mods");
        if (Directory.Exists(root))
        {
            try
            {
                string[] folders = Directory.GetDirectories(root);
                Array.Sort(folders, StringComparer.Ordinal);
                for (int i = 0; i < folders.Length; i++) Load(folders[i], inventory, previousRefresh, previousValues);
            }
            catch (Exception e) { Debug.LogWarning("Lua mods: cannot read mod directory: " + e.Message); }
        }
        // Process the normal stat queue immediately, including removed mods on
        // a stage transition that reuses the same player inventory.
        if (removed || activeKeys.Count != 0)
        {
            inventory.statInventory.Tick();
            inventory.playerStats.ForceUpdateStats();
        }
        if (refreshMods.Count != 0) PlayerXp.A_LevelUp += RefreshAtLevel;
    }

    private static bool ValidRefresh(Manifest manifest) => manifest.refresh == null || (manifest.api == 2 && manifest.refresh == "level_up");

    private static void RefreshAtLevel(int level)
    {
        if (MyPlayer.Instance == null || !ReferenceEquals(MyPlayer.Instance.inventory, lastInventory)) return;
        foreach (var mod in refreshMods)
        {
            try { ApplyValues(mod.name, mod.manifest, mod.stats, mod.source, lastInventory, level, false); }
            catch (Exception e) { Debug.LogWarning($"Lua mod refresh skipped: {mod.name}: {e.Message}"); }
        }
    }

    private static void Load(string folder, PlayerInventory inventory, Dictionary<string, RefreshMod> previousRefresh, Dictionary<string, StatModifier> previousValues)
    {
        string name = Path.GetFileName(folder);
        try
        {
            string manifestPath = Path.Combine(folder, "manifest.json");
            if (!File.Exists(manifestPath) || new FileInfo(manifestPath).Length > FileLimit) return;
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.id == null || !Enabled(manifest)) return;
            if (manifest.api == 3) return; // Registered separately at data-load time.
            if (!ValidRefresh(manifest) || !AndroidModHooks.TryGetStats(manifest.api, manifest.hook, manifest.hooks, out EStat[] stats) || manifest.id != name)
                throw new InvalidDataException("invalid id, API version, or hook");
            string scriptPath = Path.Combine(folder, "main.lua");
            if (!File.Exists(scriptPath) || new FileInfo(scriptPath).Length > FileLimit)
                throw new InvalidDataException("missing or oversized main.lua");
            string source = File.ReadAllText(scriptPath);
            if (source.IndexOf('\0') >= 0) throw new InvalidDataException("NUL in script");
            try { ApplyValues(name, manifest, stats, source, inventory, inventory.playerXp != null ? inventory.playerXp.level : 0, true); }
            catch (Exception e)
            {
                if (manifest.refresh != "level_up" || !previousRefresh.TryGetValue(name, out var previous) || previous.manifest.hooks.Length != manifest.hooks.Length) throw;
                for (int i = 0; i < manifest.hooks.Length; i++)
                    if (previous.manifest.hooks[i] != manifest.hooks[i] || !previousValues.ContainsKey("lua-mod/" + name + "/" + manifest.hooks[i])) throw;
                foreach (string hook in manifest.hooks)
                {
                    string key = "lua-mod/" + name + "/" + hook;
                    inventory.statInventory.ChangeMovingStat(key, previousValues[key]);
                    activeKeys.Add(key);
                }
                Debug.LogWarning($"Lua mod retained after stage refresh failure: {name}: {e.Message}");
            }
            if (manifest.refresh == "level_up") refreshMods.Add(new RefreshMod { name = name, source = source, manifest = manifest, stats = stats });
        }
        catch (Exception e) { Debug.LogWarning($"Lua mod skipped: {name}: {e.Message}"); }
    }

    private static void ApplyValues(string name, Manifest manifest, EStat[] stats, string source, PlayerInventory inventory, int level, bool initial)
    {
            if (manifest.refresh == "level_up") source = "local level = " + Math.Max(0, level).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + source;
            var error = new byte[256];
            var values = new double[stats.Length];
            int status;
            if (manifest.api == 1) status = megabonk_lua_eval(Encoding.UTF8.GetBytes(source + "\0"), out values[0], error, (UIntPtr)error.Length);
            else status = megabonk_lua_eval_values(Encoding.UTF8.GetBytes(source + "\0"), values.Length, values, error, (UIntPtr)error.Length);
            if (status != 0)
            {
                int end = Array.IndexOf(error, (byte)0);
                throw new InvalidDataException(Encoding.UTF8.GetString(error, 0, end < 0 ? error.Length : end));
            }
            for (int i = 0; i < values.Length; i++)
            {
                double value = values[i];
                string hook = manifest.api == 1 ? manifest.hook : manifest.hooks[i];
                bool additive = AndroidModHooks.IsAdditive(hook);
                if (double.IsNaN(value) || double.IsInfinity(value) || (additive ? value < -1000 || value > 1000 : value < .1 || value > 4))
                    throw new InvalidDataException(additive ? "stat addition must be between -1000 and 1000" : "stat multiplier must be between 0.1 and 4");
            }
            // Validate every value before enqueuing any part of this package.
            for (int i = 0; i < stats.Length; i++)
            {
                string hook = manifest.api == 1 ? manifest.hook : manifest.hooks[i];
                string key = "lua-mod/" + name + (manifest.api == 1 ? "" : "/" + hook);
                inventory.statInventory.ChangeMovingStat(key, new StatModifier {
                    stat = stats[i], modifyType = AndroidModHooks.IsAdditive(hook) ? EStatModifyType.Flat : EStatModifyType.Multiplication, modification = (float)values[i]
                });
                if (initial) activeKeys.Add(key);
                Debug.Log($"Lua mod loaded: {name} {hook}={values[i]}");
            }
    }
}
#endif
