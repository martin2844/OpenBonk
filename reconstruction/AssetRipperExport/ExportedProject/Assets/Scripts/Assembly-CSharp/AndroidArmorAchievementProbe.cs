#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

// Explicitly opted-in, isolated Android diagnostic; modifies the test package's save.
internal sealed class AndroidArmorAchievementProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_armor_achievement_probe", false)) return;
        var go = new GameObject("Armor achievement probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidArmorAchievementProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || SaveManager.Instance == null || !SaveManager.loaded)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || player.character != ECharacter.SirOofie) { Debug.LogError("Armor achievement probe: Sir Oofie or scene unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        player.inventory.statInventory.ChangeStat(new StatModifier { stat = EStat.Armor, modifyType = EStatModifyType.Flat, modification = 0.5f }, false, 30f, false);
        player.inventory.statInventory.Tick();
        MyStats.SetValueForce("damageReductionArmorAsKnight", 499f);
        var health = player.inventory.playerHealth;
        deadline = Time.realtimeSinceStartup + 10f; while (Time.realtimeSinceStartup < deadline && !health.CanTakeDamage()) yield return null;
        if (!health.CanTakeDamage()) { Debug.LogError("Armor achievement probe: player cannot take damage"); Restore(); yield break; }
        int events = 0, blocked = 0; float dealt = 0;
        void OnDamage(PlayerHealth h, DamageContainer dc, bool shield) { if (h != health || dc.damageSource != "armor_probe") return; events++; blocked = dc.damageBlockedByArmor; dealt = dc.damage; }
        PlayerHealth.A_TakeDamage += OnDamage;
        int hp = health.hp; float shieldBefore = health.shield;
        Debug.Log($"Armor achievement probe: before armor={player.inventory.playerStats.GetStat(EStat.Armor)}, hp={hp}, shield={shieldBefore}, knight={MyStats.GetStat("damageReductionArmorAsKnight")}, unlocked={SaveManager.Instance.progression.achievements.Contains("a_aegis")}");
        health.DamagePlayerExternal(10f, 0f, Vector3.forward, false, "armor_probe", DcFlags.BypassEvade | DcFlags.BypassAegis);
        yield return new WaitForSecondsRealtime(4f);
        PlayerHealth.A_TakeDamage -= OnDamage;
        Debug.Log($"Armor achievement probe: after events={events}, blocked={blocked}, damage={dealt}, hp={health.hp}, shield={health.shield}, knight={MyStats.GetStat("damageReductionArmorAsKnight")}, total={MyStats.GetStat("damageReductionArmor")}, unlocked={SaveManager.Instance.progression.achievements.Contains("a_aegis")}");
        SaveManager.Instance.SaveStats(); SaveManager.Instance.SaveProgression();
        Restore();
        void Restore() { if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
