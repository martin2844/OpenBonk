#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Menu.Shop;
using Newtonsoft.Json;
using UnityEngine;

// API 3 supports a character/weapon pair, weapon, item, or experimental Forest-based map per package.
// Only launcher-installed, explicitly enabled packages are considered.
internal static class AndroidContentMods
{
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class Manifest { public int api; public string kind, id; public FileSpec[] files; public CharacterSpec character; public WeaponSpec weapon; public PassiveSpec passive; public ItemSpec item; public MapSpec map; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class FileSpec { public string path, sha256; public long size; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class CharacterSpec { public string id, name, description, bundle, model, walk, idle, run, jump, surf, surfboard, portrait; public float scale; public HatSpec hat; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class HatSpec { public string bone; public float offsetX, offsetY, offsetZ, scale; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class WeaponSpec { public string id, name, description, effect, icon, lightningColor, soundClip; public float damage, cooldown, range, chainRange, chainProc; public int bounces; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class PassiveSpec { public string name, description; public PerLevelSpec[] perLevel; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class PerLevelSpec { public string stat; public float amount; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class ItemSpec { public string id, name, description, icon, rarity; public bool inPool; public int maxAmount, maxAmountPerRun; public ItemBonusSpec[] bonuses; public string[] hooks; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class ItemBonusSpec { public string stat, mode; public float amount; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class MapSpec { public string id, name, description, icon, groundTexture, wallTexture, bundle, @base; public string[] hooks; public MapObjectSpec[] objects; public int tileGridSize; public float volatility, slopeStrength, flatMapBias, stageDuration, noiseScale, heightMultiplier, mapScale; }
    [Serializable, UnityEngine.Scripting.Preserve] private sealed class MapObjectSpec { public string prefab; public int amount; public int[] stages; public float scaleMin, scaleMax, checkRadius, maxSlopeAngle, upOffset; }
    private static readonly List<AssetBundle> Bundles = new List<AssetBundle>();
    private static readonly List<OpenBonkProceduralMapBuild> Maps = new List<OpenBonkProceduralMapBuild>();
    private static readonly List<OpenBonkMeshMapBuild> MeshMaps = new List<OpenBonkMeshMapBuild>();
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    private sealed class DismissDialog : AndroidJavaProxy
    {
        internal DismissDialog() : base("android.content.DialogInterface$OnClickListener") { }
        public void onClick(AndroidJavaObject dialog, int which) { dialog.Call("dismiss"); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install() { DataManager.A_DataLoaded -= Load; DataManager.A_DataLoaded += Load; }
    private static void Load()
    {
        DataManager.A_DataLoaded -= Load;
        string root = Path.Combine(Application.persistentDataPath, "Mods");
        if (!Directory.Exists(root)) return;
        string[] folders = Directory.GetDirectories(root); Array.Sort(folders, StringComparer.Ordinal);
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var prefs = activity.Call<AndroidJavaObject>("getSharedPreferences", "megabonk_mod_state", 0);
        var failed = new List<string>();
        for (int i = 0; i < folders.Length; i++)
        {
            string id = Path.GetFileName(folders[i]);
            if (!prefs.Call<bool>("getBoolean", id, false)) continue;
            try { LoadPackage(folders[i], id); }
            catch (Exception e) { Debug.LogWarning($"Content mod skipped: {id}: {e.Message}"); failed.Add(id); }
        }
        if (failed.Count != 0) ShowLoadFailure(activity, failed);
    }
    private static void ShowLoadFailure(AndroidJavaObject activity, List<string> failed)
    {
        string names = string.Join(", ", failed);
        try
        {
            using var locale = new AndroidJavaClass("java.util.Locale");
            using var currentLocale = locale.CallStatic<AndroidJavaObject>("getDefault");
            bool spanish = currentLocale.Call<string>("getLanguage") == "es";
            string title = spanish ? "No se pudieron cargar algunos mods" : "Some mods could not be loaded";
            string message = spanish
                ? $"Mods: {names}\n\nVolvé al launcher para desactivarlos o restaurar una versión anterior en Gestionar mods."
                : $"Mods: {names}\n\nReturn to the launcher to disable them or restore a previous version in Manage mods.";
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                    using var builder = new AndroidJavaObject("android.app.AlertDialog$Builder", currentActivity);
                    builder.Call<AndroidJavaObject>("setTitle", title).Call<AndroidJavaObject>("setMessage", message).Call<AndroidJavaObject>("setPositiveButton", "OK", new DismissDialog()).Call<AndroidJavaObject>("show");
                }
                catch (Exception e) { Debug.LogWarning($"Could not show mod loading error: {e.Message}"); }
            }));
        }
        catch (Exception e) { Debug.LogWarning($"Could not schedule mod loading error: {e.Message}"); }
    }
    private static void LoadPackage(string folder, string id)
    {
        string manifestPath = Path.Combine(folder, "manifest.json");
        if (!File.Exists(manifestPath) || new FileInfo(manifestPath).Length > 65536) return;
        // JsonUtility creates empty instances for omitted nested classes; API 3
        // uses absence to distinguish item-only and character packages.
        var manifest = JsonConvert.DeserializeObject<Manifest>(Utf8.GetString(File.ReadAllBytes(manifestPath)));
        // Other API versions are handled by AndroidLuaMods.
        if (manifest == null || manifest.api != 3 || manifest.kind != "content") return;
        if (manifest.id != id || manifest.files == null || manifest.files.Length < 2 || manifest.files.Length > 64) throw new InvalidDataException("invalid manifest");
        if (manifest.map != null)
        {
            if (manifest.item != null || manifest.character != null || manifest.weapon != null || manifest.passive != null) throw new InvalidDataException("mixed content definitions are not supported");
            LoadMap(folder, id, manifest); return;
        }
        if (manifest.item != null)
        {
            if (manifest.character != null || manifest.weapon != null || manifest.passive != null) throw new InvalidDataException("mixed content definitions are not supported");
            LoadItem(folder, id, manifest); return;
        }
        if (manifest.character == null && manifest.weapon != null && manifest.passive == null)
        {
            LoadWeapon(folder, id, manifest); return;
        }
        if (manifest.character == null || manifest.weapon == null || manifest.files.Length < 3) throw new InvalidDataException("invalid character manifest");
        var c = manifest.character; var w = manifest.weapon;
        if ((!string.IsNullOrEmpty(w.icon) && !Text(w.icon, 120)) || (!string.IsNullOrEmpty(w.soundClip) && (!Text(w.soundClip, 120) || w.effect != "lightning"))) throw new InvalidDataException("invalid weapon appearance");
        OpenBonkContentIds.Character(id, c.id); OpenBonkContentIds.Weapon(id, w.id);
        if (!Text(c.name, 80) || !Text(c.description, 500) || !Text(w.name, 80) || !Text(w.description, 500) || !Text(c.model, 120) || !Text(c.walk, 120) || (w.effect != "lightning" && w.effect != "fireball" && w.effect != "arrow") ||
            !Finite(c.scale, .01f, 10f) || !Finite(w.damage, 0f, 10000f) || !Finite(w.cooldown, .05f, 100f) || !Finite(w.range, .1f, 100f) || !Finite(w.chainRange, 0f, 100f) || !Finite(w.chainProc, 0f, 1f) || w.bounces < 0 || w.bounces > 16)
            throw new InvalidDataException("invalid content definition");
        if ((!string.IsNullOrEmpty(c.idle) && !Text(c.idle, 120)) || (!string.IsNullOrEmpty(c.run) && !Text(c.run, 120)) || (!string.IsNullOrEmpty(c.jump) && !Text(c.jump, 120)) || (!string.IsNullOrEmpty(c.surf) && !Text(c.surf, 120)) || (!string.IsNullOrEmpty(c.surfboard) && !Text(c.surfboard, 120))) throw new InvalidDataException("invalid movement definition");
        if (c.hat != null && (!Text(c.hat.bone, 120) || !Finite(c.hat.offsetX, -20f, 20f) || !Finite(c.hat.offsetY, -20f, 20f) || !Finite(c.hat.offsetZ, -20f, 20f) || !Finite(c.hat.scale, .01f, 10f))) throw new InvalidDataException("invalid hat anchor");
        EStat[] passiveStats = null; float[] passiveValues = null;
        if (manifest.passive != null)
        {
            var p = manifest.passive;
            if (!Text(p.name, 80) || !Text(p.description, 500) || p.perLevel == null || p.perLevel.Length == 0 || p.perLevel.Length > 16) throw new InvalidDataException("invalid passive definition");
            passiveStats = new EStat[p.perLevel.Length]; passiveValues = new float[p.perLevel.Length];
            var seen = new HashSet<EStat>();
            for (int i = 0; i < p.perLevel.Length; i++)
            {
                var bonus = p.perLevel[i];
                if (bonus == null || !Enum.TryParse(bonus.stat, false, out EStat stat) || !Enum.IsDefined(typeof(EStat), stat) || !seen.Add(stat) || !Finite(bonus.amount, -1f, 1f)) throw new InvalidDataException("invalid per-level bonus");
                passiveStats[i] = stat; passiveValues[i] = bonus.amount;
            }
        }
        var files = new Dictionary<string, FileSpec>(StringComparer.Ordinal);
        foreach (var f in manifest.files)
        {
            if (f == null || !SafePath(f.path) || f.size <= 0 || f.size > 64L * 1024 * 1024 || f.sha256 == null || f.sha256.Length != 64 || files.ContainsKey(f.path)) throw new InvalidDataException("invalid file declaration");
            files.Add(f.path, f);
        }
        string scriptPath = CheckedFile(folder, files, "main.lua", 65536);
        string bundlePath = CheckedFile(folder, files, c.bundle, 64L * 1024 * 1024);
        string portraitPath = CheckedFile(folder, files, c.portrait, 4L * 1024 * 1024);
        string source = Utf8.GetString(File.ReadAllBytes(scriptPath));
        if (source.IndexOf('\0') >= 0) throw new InvalidDataException("invalid Lua source");
        AssetBundle bundle = null; Texture2D portrait = null, weaponIcon = null;
        try
        {
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null) throw new InvalidDataException("cannot load model bundle");
            var model = bundle.LoadAsset<GameObject>(c.model);
            if (model == null || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0) throw new InvalidDataException("model missing");
            if (c.hat != null)
            {
                bool found = false;
                foreach (var part in model.GetComponentsInChildren<Transform>(true)) if (part.name == c.hat.bone) { found = true; break; }
                if (!found) throw new InvalidDataException("hat bone missing");
            }
            AnimationClip idle = null, walk = null, run = null, jump = null, surf = null;
            foreach (var clip in bundle.LoadAllAssets<AnimationClip>()) { if (clip.name == c.idle) idle = clip; if (clip.name == c.walk) walk = clip; if (clip.name == c.run) run = clip; if (clip.name == c.jump) jump = clip; if (clip.name == c.surf) surf = clip; }
            if (walk == null || (!string.IsNullOrEmpty(c.idle) && idle == null) || (!string.IsNullOrEmpty(c.run) && run == null) || (!string.IsNullOrEmpty(c.jump) && jump == null) || (!string.IsNullOrEmpty(c.surf) && surf == null)) throw new InvalidDataException("movement clip missing");
            if (!string.IsNullOrEmpty(c.surfboard)) { bool found = false; foreach (var part in model.GetComponentsInChildren<Transform>(true)) if (part.name == c.surfboard) { found = true; break; } if (!found) throw new InvalidDataException("surfboard missing"); }
            portrait = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!portrait.LoadImage(File.ReadAllBytes(portraitPath)) || portrait.width > 2048 || portrait.height > 2048) throw new InvalidDataException("invalid portrait");
            var data = DataManager.Instance;
            var baseCharacter = data.GetCharacterData(ECharacter.Fox);
            var baseWeapon = data.GetWeapon(w.effect == "fireball" ? EWeapon.FireStaff : w.effect == "arrow" ? EWeapon.Bow : EWeapon.LightningStaff);
            if (!string.IsNullOrEmpty(w.icon))
            {
                string iconPath = CheckedFile(folder, files, w.icon, 4L * 1024 * 1024);
                weaponIcon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!weaponIcon.LoadImage(File.ReadAllBytes(iconPath)) || weaponIcon.width > 2048 || weaponIcon.height > 2048) throw new InvalidDataException("invalid weapon icon");
                weaponIcon.filterMode = FilterMode.Point;
            }
            var weapon = CreateWeapon(id, w, source, weaponIcon != null ? weaponIcon : baseWeapon.icon);
            if (!string.IsNullOrEmpty(w.soundClip))
            {
                weapon.lightningSound = bundle.LoadAsset<AudioClip>(w.soundClip);
                if (weapon.lightningSound == null) throw new InvalidDataException("weapon sound missing");
            }
            var character = ScriptableObject.CreateInstance<OpenBonkExternalCharacterData>();
            var skin = UnityEngine.Object.Instantiate(data.GetSkin(ECharacter.Fox, 0));
            skin.unlockRequirement = null; skin.skinType = Assets.Scripts._Data.ESkinType.Default; skin.icon = portrait;
            character.isEnabled = true; character.externalId = id + ":" + c.id; character.displayName = c.name; character.description = c.description;
            character.sortingPriority = -100; character.icon = portrait; character.weapon = weapon; character.prefab = model; character.idleAnimation = idle; character.walkAnimation = walk; character.runAnimation = run; character.jumpAnimation = jump; character.surfAnimation = surf; character.surfboard = c.surfboard; character.modelScale = c.scale;
            if (c.hat != null) { character.hasHatOverride = true; character.hatBone = c.hat.bone; character.hatOffset = new Vector3(c.hat.offsetX, c.hat.offsetY, c.hat.offsetZ); character.hatScale = c.hat.scale; }
            if (manifest.passive != null)
            {
                var passive = ScriptableObject.CreateInstance<OpenBonkLevelPassiveData>();
                passive.displayName = manifest.passive.name; passive.description = manifest.passive.description; passive.icon = portrait;
                passive.ePassive = (EPassive)OpenBonkContentIds.Weapon(id, w.id); passive.stats = passiveStats; passive.perLevel = passiveValues;
                character.passive = passive;
            }
            character.colliderHeight = baseCharacter.colliderHeight; character.colliderWidth = baseCharacter.colliderWidth;
            character.statModifiers = baseCharacter.statModifiers == null ? new List<StatModifier>() : new List<StatModifier>(baseCharacter.statModifiers);
            character.StatCategoryRatios = baseCharacter.StatCategoryRatios == null ? new List<StatCategoryRatio>() : new List<StatCategoryRatio>(baseCharacter.StatCategoryRatios);
            data.RegisterExternalCharacter(id, c.id, w.id, character, weapon, skin);
            Bundles.Add(bundle); bundle = null; portrait = null; weaponIcon = null;
            Debug.Log($"Content mod loaded: {id} character={c.id} weapon={w.id}");
        }
        finally { if (bundle != null) bundle.Unload(true); if (portrait != null) UnityEngine.Object.Destroy(portrait); if (weaponIcon != null) UnityEngine.Object.Destroy(weaponIcon); }
    }
    private static void LoadMap(string folder, string id, Manifest manifest)
    {
        var spec = manifest.map;
        OpenBonkContentIds.Map(id, spec.id);
        if (spec.@base != "Forest" && spec.@base != "Desert") throw new InvalidDataException("unsupported map template");
        bool spawnHook = false, healthHook = false, speedHook = false, damageHook = false;
        bool enemyHealthHook = false, enemySpeedHook = false, enemyDamageHook = false;
        if (spec.hooks != null) foreach (string hook in spec.hooks)
        {
            if (hook == "stage_spawn_multiplier" && !spawnHook) spawnHook = true;
            else if (hook == "stage_enemy_health_multiplier" && !healthHook) healthHook = true;
            else if (hook == "stage_enemy_speed_multiplier" && !speedHook) speedHook = true;
            else if (hook == "stage_enemy_damage_multiplier" && !damageHook) damageHook = true;
            else if (hook == "enemy_health_multiplier" && !enemyHealthHook) enemyHealthHook = true;
            else if (hook == "enemy_speed_multiplier" && !enemySpeedHook) enemySpeedHook = true;
            else if (hook == "enemy_damage_multiplier" && !enemyDamageHook) enemyDamageHook = true;
            else throw new InvalidDataException("unsupported map hooks");
        }
        if ((spec.objects == null && !string.IsNullOrEmpty(spec.bundle)) || (spec.objects != null && (spec.objects.Length == 0 || spec.objects.Length > 8 || !Text(spec.bundle, 120)))) throw new InvalidDataException("invalid map objects");
        var files = new Dictionary<string, FileSpec>(StringComparer.Ordinal);
        foreach (var f in manifest.files)
        {
            if (f == null || !SafePath(f.path) || f.size <= 0 || f.size > 64L * 1024 * 1024 || f.sha256 == null || f.sha256.Length != 64 || files.ContainsKey(f.path)) throw new InvalidDataException("invalid file declaration");
            files.Add(f.path, f);
        }
        string source = Utf8.GetString(File.ReadAllBytes(CheckedFile(folder, files, "main.lua", 65536)));
        if (source.IndexOf('\0') >= 0) throw new InvalidDataException("invalid Lua source");
        string iconPath = CheckedFile(folder, files, spec.icon, 4L * 1024 * 1024);
        string groundPath = string.IsNullOrEmpty(spec.groundTexture) ? null : CheckedFile(folder, files, spec.groundTexture, 4L * 1024 * 1024);
        string wallPath = string.IsNullOrEmpty(spec.wallTexture) ? null : CheckedFile(folder, files, spec.wallTexture, 4L * 1024 * 1024);
        string bundlePath = spec.objects == null ? null : CheckedFile(folder, files, spec.bundle, 64L * 1024 * 1024);
        Texture2D icon = null, ground = null, wall = null; AssetBundle bundle = null; OpenBonkProceduralMapBuild build = null; OpenBonkMeshMapBuild meshBuild = null;
        try
        {
            icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!icon.LoadImage(File.ReadAllBytes(iconPath)) || icon.width > 2048 || icon.height > 2048) throw new InvalidDataException("invalid map icon");
            if (groundPath != null)
            {
                ground = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!ground.LoadImage(File.ReadAllBytes(groundPath)) || ground.width > 2048 || ground.height > 2048) throw new InvalidDataException("invalid ground texture");
                ground.wrapMode = TextureWrapMode.Repeat; ground.filterMode = FilterMode.Point;
            }
            if (wallPath != null)
            {
                wall = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!wall.LoadImage(File.ReadAllBytes(wallPath)) || wall.width > 2048 || wall.height > 2048) throw new InvalidDataException("invalid wall texture");
                wall.wrapMode = TextureWrapMode.Repeat; wall.filterMode = FilterMode.Point;
            }
            OpenBonkExternalMapData map;
            if (spec.@base == "Desert")
            {
                var recipe = new OpenBonkMeshMapRecipe { name = spec.name, description = spec.description, noiseScale = spec.noiseScale, heightMultiplier = spec.heightMultiplier, mapScale = spec.mapScale, stageDuration = spec.stageDuration };
                meshBuild = OpenBonkMeshMapBuild.Create(DataManager.Instance.GetMap(EMap.Desert), recipe, icon, ground, wall); map = meshBuild.Map;
            }
            else
            {
                var recipe = new OpenBonkProceduralMapRecipe { name = spec.name, description = spec.description, tileGridSize = spec.tileGridSize, volatility = spec.volatility, slopeStrength = spec.slopeStrength, flatMapBias = spec.flatMapBias, stageDuration = spec.stageDuration };
                build = OpenBonkProceduralMapBuild.Create(DataManager.Instance.GetMap(EMap.Forest), recipe, icon, ground, wall); map = build.Map;
            }
            map.luaSource = source; map.luaStageSpawnMultiplier = spawnHook; map.luaStageEnemyHealthMultiplier = healthHook; map.luaStageEnemySpeedMultiplier = speedHook; map.luaStageEnemyDamageMultiplier = damageHook;
            map.luaEnemyHealthMultiplier = enemyHealthHook; map.luaEnemySpeedMultiplier = enemySpeedHook; map.luaEnemyDamageMultiplier = enemyDamageHook;
            if (spec.objects != null)
            {
                bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null) throw new InvalidDataException("cannot load map object bundle");
                var placements = new OpenBonkMapObjects.Placement[spec.objects.Length];
                for (int i = 0; i < placements.Length; i++)
                {
                    var obj = spec.objects[i];
                    if (obj == null || !Text(obj.prefab, 120)) throw new InvalidDataException("invalid map object");
                    placements[i] = new OpenBonkMapObjects.Placement { prefab = bundle.LoadAsset<GameObject>(obj.prefab), amount = obj.amount, stages = obj.stages, scaleMin = obj.scaleMin, scaleMax = obj.scaleMax, checkRadius = obj.checkRadius, maxSlopeAngle = obj.maxSlopeAngle, upOffset = obj.upOffset };
                }
                OpenBonkMapObjects.AddToStages(map, placements);
            }
            DataManager.Instance.RegisterExternalMap(id, spec.id, map);
            if (build != null) { Maps.Add(build); build = null; }
            if (meshBuild != null) { MeshMaps.Add(meshBuild); meshBuild = null; }
            if (bundle != null) { Bundles.Add(bundle); bundle = null; }
            icon = null; ground = null; wall = null;
            Debug.Log($"Content mod loaded: {id} map={spec.id}");
        }
        finally { if (build != null) build.Dispose(); if (meshBuild != null) meshBuild.Dispose(); if (bundle != null) bundle.Unload(true); if (icon != null) UnityEngine.Object.Destroy(icon); if (ground != null) UnityEngine.Object.Destroy(ground); if (wall != null) UnityEngine.Object.Destroy(wall); }
    }
    private static void LoadWeapon(string folder, string id, Manifest manifest)
    {
        var w = manifest.weapon;
        if (!string.IsNullOrEmpty(w.soundClip)) throw new InvalidDataException("standalone weapon sound bundle unsupported");
        OpenBonkContentIds.Weapon(id, w.id);
        if (!Text(w.name, 80) || !Text(w.description, 500) || (w.effect != "lightning" && w.effect != "fireball" && w.effect != "arrow") ||
            !Finite(w.damage, 0f, 10000f) || !Finite(w.cooldown, .05f, 100f) || !Finite(w.range, .1f, 100f) ||
            !Finite(w.chainRange, 0f, 100f) || !Finite(w.chainProc, 0f, 1f) || w.bounces < 0 || w.bounces > 16)
            throw new InvalidDataException("invalid weapon definition");
        var files = new Dictionary<string, FileSpec>(StringComparer.Ordinal);
        foreach (var f in manifest.files)
        {
            if (f == null || !SafePath(f.path) || f.size <= 0 || f.size > 64L * 1024 * 1024 || f.sha256 == null || f.sha256.Length != 64 || files.ContainsKey(f.path)) throw new InvalidDataException("invalid file declaration");
            files.Add(f.path, f);
        }
        string source = Utf8.GetString(File.ReadAllBytes(CheckedFile(folder, files, "main.lua", 65536)));
        if (source.IndexOf('\0') >= 0) throw new InvalidDataException("invalid Lua source");
        string iconPath = CheckedFile(folder, files, w.icon, 4L * 1024 * 1024);
        Texture2D icon = null; OpenBonkLuaWeaponData weapon = null;
        try
        {
            icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!icon.LoadImage(File.ReadAllBytes(iconPath)) || icon.width > 2048 || icon.height > 2048) throw new InvalidDataException("invalid weapon icon");
            weapon = CreateWeapon(id, w, source, icon);
            DataManager.Instance.RegisterExternalWeapon(id, w.id, weapon);
            icon = null; weapon = null;
            Debug.Log($"Content mod loaded: {id} weapon={w.id}");
        }
        finally { if (weapon != null) { if (weapon.upgradeData != null) UnityEngine.Object.Destroy(weapon.upgradeData); UnityEngine.Object.Destroy(weapon); } if (icon != null) UnityEngine.Object.Destroy(icon); }
    }
    private static OpenBonkLuaWeaponData CreateWeapon(string id, WeaponSpec w, string source, Texture icon)
    {
        bool projectile = w.effect == "fireball" || w.effect == "arrow";
        var baseWeapon = DataManager.Instance.GetWeapon(w.effect == "fireball" ? EWeapon.FireStaff : w.effect == "arrow" ? EWeapon.Bow : EWeapon.LightningStaff);
        var weapon = ScriptableObject.CreateInstance<OpenBonkLuaWeaponData>();
        weapon.isEnabled = true; weapon.externalId = id + ":" + w.id; weapon.displayName = w.name; weapon.description = w.description;
        weapon.effect = w.effect; weapon.icon = icon; weapon.upgradeData = OpenBonkLuaWeaponData.CreateUpgrades(baseWeapon.upgradeData, projectile); weapon.damage = w.damage; weapon.projectiles = 1;
        weapon.projectileBounces = w.bounces; weapon.attackDuration = 1f; weapon.projectileSpeed = 1f; weapon.endCooldown = w.cooldown;
        weapon.element = EElement.Lightning; weapon.procCoefficient = 1f; weapon.spawnProjectileRange = w.range; weapon.chainRange = w.chainRange; weapon.chainProcCoefficient = w.chainProc; weapon.luaSource = source;
        if (!string.IsNullOrEmpty(w.lightningColor))
        {
            if (projectile || w.lightningColor.Length != 7 || w.lightningColor[0] != '#' || !ColorUtility.TryParseHtmlString(w.lightningColor, out Color color)) throw new InvalidDataException("invalid lightning color");
            weapon.hasLightningColor = true; weapon.lightningColor = color;
        }
        if (projectile)
        {
            if (baseWeapon.attack == null) throw new InvalidDataException("projectile template unavailable");
            weapon.attack = baseWeapon.attack; weapon.element = baseWeapon.element; weapon.knockback = baseWeapon.knockback;
            weapon.attackDuration = baseWeapon.attackDuration; weapon.maxDuration = baseWeapon.maxDuration; weapon.maxSizeMultiplier = baseWeapon.maxSizeMultiplier;
            weapon.effectDuration = baseWeapon.effectDuration; weapon.projectileSpeed = baseWeapon.projectileSpeed;
            weapon.burstTime = baseWeapon.burstTime; weapon.minBurstInterval = baseWeapon.minBurstInterval;
            weapon.canBounce = baseWeapon.canBounce; weapon.amplificationMode = baseWeapon.amplificationMode;
            weapon.procCoefficient = baseWeapon.procCoefficient; weapon.useVision = baseWeapon.useVision;
            weapon.canMultiHit = baseWeapon.canMultiHit; weapon.onlySpawnWhenCloseEnemies = baseWeapon.onlySpawnWhenCloseEnemies;
            weapon.spawnOffset = baseWeapon.spawnOffset;
        }
        return weapon;
    }
    private static void LoadItem(string folder, string id, Manifest manifest)
    {
        var item = manifest.item;
        OpenBonkContentIds.Item(id, item.id);
        if (!Text(item.name, 80) || !Text(item.description, 500) || !Enum.TryParse(item.rarity, false, out EItemRarity rarity) ||
            !Enum.IsDefined(typeof(EItemRarity), rarity) || rarity.ToString() != item.rarity || item.maxAmount < 0 || item.maxAmountPerRun < 0 ||
            item.maxAmount > 100000 || item.maxAmountPerRun > 100000 || (item.bonuses != null && item.bonuses.Length > 16))
            throw new InvalidDataException("invalid item definition");
        bool stackHook = false, attackHook = false, hitHook = false, levelHook = false, killHook = false, damageHook = false, beforeDamageHook = false, tickHook = false;
        if (item.hooks != null)
        {
            if (item.hooks.Length > 8) throw new InvalidDataException("too many item hooks");
            foreach (string hook in item.hooks)
            {
                if (hook == "stack_changed" && !stackHook) stackHook = true;
                else if (hook == "pre_attack" && !attackHook) attackHook = true;
                else if (hook == "on_hit" && !hitHook) hitHook = true;
                else if (hook == "level_up" && !levelHook) levelHook = true;
                else if (hook == "enemy_killed" && !killHook) killHook = true;
                else if (hook == "player_damaged" && !damageHook) damageHook = true;
                else if (hook == "before_player_damage" && !beforeDamageHook) beforeDamageHook = true;
                else if (hook == "tick" && !tickHook) tickHook = true;
                else throw new InvalidDataException("invalid or duplicate item hook");
            }
        }
        var declaredBonuses = item.bonuses ?? Array.Empty<ItemBonusSpec>();
        if (declaredBonuses.Length == 0 && !attackHook && !hitHook && !beforeDamageHook && !tickHook && !killHook) throw new InvalidDataException("item needs bonuses or an active hook");
        var bonuses = new OpenBonkItemBonus[declaredBonuses.Length]; var seen = new HashSet<EStat>();
        for (int i = 0; i < bonuses.Length; i++)
        {
            var bonus = declaredBonuses[i];
            if (bonus == null || !Enum.TryParse(bonus.stat, false, out EStat stat) || !Enum.IsDefined(typeof(EStat), stat) || stat.ToString() != bonus.stat || !seen.Add(stat) ||
                !Enum.TryParse(bonus.mode, false, out EStatModifyType mode) || (mode != EStatModifyType.Flat && mode != EStatModifyType.Addition) || mode.ToString() != bonus.mode || !Finite(bonus.amount, -10f, 10f))
                throw new InvalidDataException("invalid item bonus");
            bonuses[i] = new OpenBonkItemBonus { stat = stat, modifyType = mode, perStack = bonus.amount };
        }
        var files = new Dictionary<string, FileSpec>(StringComparer.Ordinal);
        foreach (var f in manifest.files)
        {
            if (f == null || !SafePath(f.path) || f.size <= 0 || f.size > 64L * 1024 * 1024 || f.sha256 == null || f.sha256.Length != 64 || files.ContainsKey(f.path)) throw new InvalidDataException("invalid file declaration");
            files.Add(f.path, f);
        }
        string script = Utf8.GetString(File.ReadAllBytes(CheckedFile(folder, files, "main.lua", 65536)));
        if (script.IndexOf('\0') >= 0) throw new InvalidDataException("invalid Lua source");
        string iconPath = CheckedFile(folder, files, item.icon, 4L * 1024 * 1024);
        Texture2D icon = null; OpenBonkItemData data = null;
        try
        {
            icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!icon.LoadImage(File.ReadAllBytes(iconPath)) || icon.width > 2048 || icon.height > 2048) throw new InvalidDataException("invalid item icon");
            data = ScriptableObject.CreateInstance<OpenBonkItemData>();
            data.isEnabled = true; data.externalId = id + ":" + item.id; data.displayName = item.name; data.description = item.description;
            data.icon = icon; data.rarity = rarity; data.inItemPool = item.inPool; data.maxAmount = item.maxAmount; data.maxAmountPerRun = item.maxAmountPerRun; data.bonuses = bonuses;
            data.luaSource = script; data.luaStackChanged = stackHook; data.luaPreAttack = attackHook; data.luaOnHit = hitHook; data.luaLevelUp = levelHook; data.luaEnemyKilled = killHook; data.luaPlayerDamaged = damageHook; data.luaBeforePlayerDamage = beforeDamageHook; data.luaTick = tickHook;
            DataManager.Instance.RegisterExternalItem(id, item.id, data);
            Debug.Log($"Content mod loaded: {id} item={item.id}");
            icon = null; data = null;
        }
        finally { if (data != null) UnityEngine.Object.Destroy(data); if (icon != null) UnityEngine.Object.Destroy(icon); }
    }
    private static string CheckedFile(string root, Dictionary<string, FileSpec> files, string path, long limit)
    {
        if (path == null || !files.TryGetValue(path, out var spec) || spec.size > limit) throw new InvalidDataException("missing declared file");
        string file = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(file) || new FileInfo(file).Length != spec.size) throw new InvalidDataException("content size mismatch");
        using var stream = File.OpenRead(file); using var sha = SHA256.Create();
        string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        if (!string.Equals(hash, spec.sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("content hash mismatch");
        return file;
    }
    private static bool SafePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > 240 || path.IndexOf('\\') >= 0) return false;
        string[] parts = path.Split('/'); if (parts.Length > 8) return false;
        foreach (string part in parts) { if (part.Length == 0 || part.Length > 64 || part == "." || part == "..") return false; foreach (char ch in part) if (!(ch >= 'a' && ch <= 'z') && !(ch >= 'A' && ch <= 'Z') && !(ch >= '0' && ch <= '9') && ch != '.' && ch != '_' && ch != '-') return false; }
        return true;
    }
    private static bool Finite(float f, float min, float max) => !float.IsNaN(f) && !float.IsInfinity(f) && f >= min && f <= max;
    private static bool Text(string text, int max) => !string.IsNullOrWhiteSpace(text) && text.Length <= max && text.IndexOf('\0') < 0;
}
#endif
