#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.IO;
using System.Collections;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Newtonsoft.Json;
using UnityEngine;

// Test-only: register isolated synthetic content against the real game data.
internal static class AndroidExternalRegistrationProbe
{
    private static bool forceExternalRun;
    private static bool doubleProjectile;
    private static bool passiveProbe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "openbonk_registration_probe", false)) return;
        forceExternalRun = intent.Call<bool>("getBooleanExtra", "openbonk_force_external_run", false);
        doubleProjectile = intent.Call<bool>("getBooleanExtra", "openbonk_double_projectile_probe", false);
        passiveProbe = intent.Call<bool>("getBooleanExtra", "openbonk_level_passive_probe", false);
        DataManager.A_DataLoaded -= Run; DataManager.A_DataLoaded += Run;
    }
    private static void Run()
    {
        DataManager.A_DataLoaded -= Run;
        try
        {
            var data = DataManager.Instance;
            var originalCharacter = data.GetCharacterData(ECharacter.Fox);
            var originalWeapon = data.GetWeapon(EWeapon.FireStaff);
            var modelBundle = AssetBundle.LoadFromFile(Path.Combine(Application.persistentDataPath, "Probe/pikachu-model"));
            if (modelBundle == null) throw new IOException("bundle unavailable");
            var model = modelBundle.LoadAsset<GameObject>("pikachu-study");
            if (model == null || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0) throw new InvalidDataException("model missing");
            var weapon = ScriptableObject.CreateInstance<OpenBonkLuaWeaponData>();
            var lightning = data.GetWeapon(EWeapon.LightningStaff);
            weapon.isEnabled = true; weapon.externalId = "pikachu-test:thunderbolt"; weapon.displayName = "Thunderbolt (test)";
            weapon.description = "Strikes an enemy and chains lightning; every second strike is stronger.";
            weapon.icon = lightning.icon; weapon.upgradeData = OpenBonkLuaWeaponData.CreateUpgrades(lightning.upgradeData);
            weapon.damage = 14f; weapon.knockback = 0f; weapon.critChance = 0f; weapon.projectiles = doubleProjectile ? 2 : 1; weapon.projectileBounces = 2;
            weapon.attackDuration = 1f; weapon.projectileSpeed = 1f; weapon.endCooldown = 1.1f; weapon.procCoefficient = 1f;
            weapon.spawnProjectileRange = 24f; weapon.chainRange = 7f; weapon.chainProcCoefficient = .5f;
            weapon.luaSource = File.ReadAllText(Path.Combine(Application.persistentDataPath, "Probe/pikachu-thunderbolt.lua"));
            var character = ScriptableObject.CreateInstance<OpenBonkExternalCharacterData>();
            var skin = UnityEngine.Object.Instantiate(data.GetSkin(ECharacter.Fox, 0));
            AnimationClip walk = null;
            foreach (var clip in modelBundle.LoadAllAssets<AnimationClip>()) if (clip.name.Contains("OpenBonk walk")) walk = clip;
            if (walk == null) throw new InvalidDataException("walk animation missing");
            character.isEnabled = true; character.externalId = "pikachu-test:hero"; character.displayName = "Pikachu (test)"; character.description = "Experimental external character";
            character.sortingPriority = -100; character.icon = originalCharacter.icon;
            var portraitPath = Path.Combine(Application.persistentDataPath, "Probe/pikachu-portrait.png");
            if (File.Exists(portraitPath))
            {
                var portrait = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!portrait.LoadImage(File.ReadAllBytes(portraitPath))) throw new InvalidDataException("portrait is invalid");
                character.icon = portrait;
            }
            character.weapon = weapon; character.prefab = model; character.passive = null; character.walkAnimation = walk; character.modelScale = .24f;
            if (passiveProbe)
            {
                var passive = ScriptableObject.CreateInstance<OpenBonkLevelPassiveData>();
                passive.displayName = "Electric Energy"; passive.description = "+1% power-up chance and duration per level gained";
                passive.stats = new[] { Assets.Scripts.Menu.Shop.EStat.PowerupChance, Assets.Scripts.Menu.Shop.EStat.DurationMultiplier };
                passive.perLevel = new[] { .01f, .01f }; passive.ePassive = (Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.EPassive)OpenBonkContentIds.Weapon("pikachu-test", "thunderbolt");
                character.passive = passive;
            }
            character.colliderHeight = originalCharacter.colliderHeight; character.colliderWidth = originalCharacter.colliderWidth;
            character.statModifiers = originalCharacter.statModifiers == null ? new List<StatModifier>() : new List<StatModifier>(originalCharacter.statModifiers);
            character.StatCategoryRatios = originalCharacter.StatCategoryRatios == null ? new List<StatCategoryRatio>() : new List<StatCategoryRatio>(originalCharacter.StatCategoryRatios);
            data.RegisterExternalCharacter("pikachu-test", "pikachu", "thunderbolt", character, weapon, skin);
            var characterId = (ECharacter)OpenBonkContentIds.Character("pikachu-test", "pikachu");
            var weaponId = (EWeapon)OpenBonkContentIds.Weapon("pikachu-test", "thunderbolt");
            if (data.GetCharacterData(characterId) != character || data.GetWeapon(weaponId) != weapon || data.GetSkin(characterId, 0) != skin ||
                data.GetCharacterData(ECharacter.Fox) != originalCharacter || data.GetWeapon(EWeapon.FireStaff) != originalWeapon)
                throw new InvalidOperationException("registry lookup mismatch");
            var prefs = new Preferences { selectedCharacter = characterId }; prefs.characterSkins[characterId] = 2;
            var restored = JsonConvert.DeserializeObject<Preferences>(JsonConvert.SerializeObject(prefs));
            if (restored.selectedCharacter != characterId || restored.GetCharacterSkin(characterId) != 2) throw new InvalidOperationException("preference roundtrip failed");
            bool rejected = false;
            try { data.RegisterExternalCharacter("pikachu-test", "pikachu", "thunderbolt", character, weapon, skin); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected || data.GetCharacterData(ECharacter.Fox) != originalCharacter) throw new InvalidOperationException("collision was not rejected");
            Debug.Log($"OpenBonk content registration probe PASS character={(int)characterId} weapon={(int)weaponId} modelRenderers={model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");
            if (forceExternalRun)
            {
                MyPlayer.A_PrePlayerSpawn -= ForceExternal; MyPlayer.A_PrePlayerSpawn += ForceExternal;
                MyPlayer.A_PlayerInventoryInitialized -= ObservePlayer; MyPlayer.A_PlayerInventoryInitialized += ObservePlayer;
            }
            // The diagnostic intentionally retains the bundle for the test process.
        }
        catch (Exception e) { Debug.LogError("OpenBonk content registration probe FAIL: " + e); }
    }
    private static void ForceExternal() { CharacterMenu.selectedCharacter = (ECharacter)OpenBonkContentIds.Character("pikachu-test", "pikachu"); }
    internal static void ObservePlayer(PlayerInventory inventory)
    {
        MyPlayer.A_PlayerInventoryInitialized -= ObservePlayer;
        var go = new GameObject("External gameplay probe"); UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<AndroidExternalGameplayProbe>().inventory = inventory;
        if (passiveProbe) go.AddComponent<AndroidLevelPassiveProbe>().inventory = inventory;
    }
}

internal sealed class AndroidLevelPassiveProbe : MonoBehaviour
{
    internal PlayerInventory inventory;
    private IEnumerator Start()
    {
        var deadline = Time.realtimeSinceStartup + 40f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying()) { Debug.LogWarning("Level passive probe INCONCLUSIVE: no gameplay"); yield break; }
        inventory.playerStats.ForceUpdateStats();
        float baseChance = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.PowerupChance);
        float baseDuration = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.DurationMultiplier);
        Inventory__Items__Pickups.Xp_and_Levels.PlayerXp.A_LevelUp?.Invoke(2); inventory.playerStats.ForceUpdateStats();
        float chance2 = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.PowerupChance);
        float duration2 = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.DurationMultiplier);
        Inventory__Items__Pickups.Xp_and_Levels.PlayerXp.A_LevelUp?.Invoke(6); inventory.playerStats.ForceUpdateStats();
        float chance6 = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.PowerupChance);
        float duration6 = Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(Assets.Scripts.Menu.Shop.EStat.DurationMultiplier);
        bool ok = Mathf.Abs(chance2-baseChance-.02f) < .0001f && Mathf.Abs(duration2-baseDuration-.02f) < .0001f && Mathf.Abs(chance6-baseChance-.06f) < .0001f && Mathf.Abs(duration6-baseDuration-.06f) < .0001f;
        Debug.Log($"Level passive probe {(ok ? "PASS" : "FAIL")}: base={baseChance:F3}/{baseDuration:F3} level2={chance2:F3}/{duration2:F3} level6={chance6:F3}/{duration6:F3}");
        Inventory__Items__Pickups.Xp_and_Levels.PlayerXp.A_LevelUp?.Invoke(0); inventory.playerStats.ForceUpdateStats();
    }
}

// Exercises the real button hover/selection path without forcing the spawn ID.
internal sealed class AndroidExternalSelectionProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "openbonk_selection_probe", false)) return;
        var go = new GameObject("External selection probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalSelectionProbe>();
    }
    private IEnumerator Start()
    {
        var id = (ECharacter)OpenBonkContentIds.Character("pikachu-test", "pikachu");
        var deadline = Time.realtimeSinceStartup + 120f;
        MyButtonCharacter button = null;
        while (Time.realtimeSinceStartup < deadline && button == null)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<MyButtonCharacter>())
                if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.characterData != null && candidate.characterData.eCharacter == id) { button = candidate; break; }
            if (button == null) yield return null;
        }
        if (button == null) { Debug.LogError("External selection probe INCONCLUSIVE: button not found"); Destroy(gameObject); yield break; }
        yield return new WaitForSecondsRealtime(4f);
        Assets.Scripts.Managers.ButtonManager.ForceHoverButton(button);
        yield return null;
        bool ok = button.canUseCharacter && CharacterMenu.selectedCharacter == id && Assets.Scripts.Managers.ButtonManager.GetCurrentButton() == button;
        Debug.Log($"External selection probe {(ok ? "PASS" : "FAIL")}: usable={button.canUseCharacter} selected={(int)CharacterMenu.selectedCharacter} expected={(int)id} portrait={(button.characterData.icon != null)}");
        if (ok) { MyPlayer.A_PlayerInventoryInitialized -= AndroidExternalRegistrationProbe.ObservePlayer; MyPlayer.A_PlayerInventoryInitialized += AndroidExternalRegistrationProbe.ObservePlayer; }
        Destroy(gameObject);
    }
}
#endif
