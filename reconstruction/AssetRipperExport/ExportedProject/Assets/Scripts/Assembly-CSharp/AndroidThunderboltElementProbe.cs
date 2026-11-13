#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in check of the actual external weapon and item pre-attack dispatcher.
internal sealed class AndroidThunderboltElementProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_thunderbolt_element_probe", false)) return;
        var go = new GameObject("Thunderbolt element probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidThunderboltElementProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk-lightning-test", "training-cell");
        if (player == null || player.inventory == null || manager == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData)) { Debug.LogError("Thunderbolt element probe INCONCLUSIVE: player or item unavailable"); yield break; }
        WeaponBase weapon = null;
        foreach (var candidate in player.inventory.weaponInventory.weapons.Values) { if (candidate.weaponData is OpenBonkLuaWeaponData) weapon = candidate; candidate.Disable(); }
        if (weapon == null) { Debug.LogError("Thunderbolt element probe INCONCLUSIVE: external weapon unavailable"); yield break; }
        manager.enabledWaves = false;
        var enemy = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Thunderbolt element probe INCONCLUSIVE: enemy unavailable"); yield break; }
        bool lightning = weapon.weaponData.element == EElement.Lightning;
        float baseline = NonCritDamage(weapon, enemy, out var beforeElement);
        player.inventory.itemInventory.AddItem(id);
        float boosted = NonCritDamage(weapon, enemy, out var boostedElement);
        weapon.weaponData.element = EElement.Neutral;
        float neutral = NonCritDamage(weapon, enemy, out var neutralElement);
        weapon.weaponData.element = EElement.Lightning;
        player.inventory.itemInventory.RemoveItem(id);
        float removed = NonCritDamage(weapon, enemy, out var removedElement);
        bool pass = lightning && beforeElement == EElement.Lightning && boostedElement == EElement.Lightning && neutralElement == EElement.Neutral && removedElement == EElement.Lightning && baseline > 0 && Math.Abs(boosted / baseline - 2f) < .001f && Math.Abs(neutral / baseline - 1f) < .001f && Math.Abs(removed / baseline - 1f) < .001f;
        Debug.Log($"Thunderbolt element probe {(pass ? "PASS" : "FAIL")}: configured={lightning} before={beforeElement} boostedElement={boostedElement} neutralElement={neutralElement} baseline={baseline:F3} boosted={boosted:F3} neutral={neutral:F3} removed={removed:F3}");
    }
    static float NonCritDamage(WeaponBase weapon, Assets.Scripts.Actors.Enemies.Enemy enemy, out EElement element)
    {
        DamageContainer hit = null;
        for (int i = 0; i < 50; i++) { hit = WeaponUtility.GetDamageContainer(weapon, null, enemy, Vector3.right, 1f); if (!hit.crit) break; }
        element = hit.element; return hit.crit ? -1f : hit.damage;
    }
}
#endif
