#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

// Disposable run: the real weapon must defeat the altar boss before the portal is used.
internal sealed class AndroidBossWeaponPortalProbe : MonoBehaviour
{
    static bool installed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_boss_weapon_portal_probe", false)) return;
        installed = true;
        var root = new GameObject("boss weapon portal probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidBossWeaponPortalProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (SceneManager.GetActiveScene().name != "GeneratedMap" || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || EnemyManager.Instance.summonerController == null || MapController.currentMap == null)) yield return null;
        var player = MyPlayer.Instance; var altar = FindFirstObjectByType<InteractableBossSpawner>();
        if (player == null || altar == null || EnemyManager.Instance == null) { Debug.LogError("Boss weapon portal probe FAIL: initial state"); yield break; }
        var inventory = player.inventory; var manager = EnemyManager.Instance;
        manager.enabledWaves = false;
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, MyTime.time + 10000f);
        inventory.statInventory.ChangeMovingStat("boss probe damage", new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = 200f });
        int index = MapController.index;
        MapController.runConfig.mapTierIndex = 1;
        altar.transform.position = player.transform.position + player.transform.forward * 3f;
        Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(2f);
        if (!altar.Interact()) { Debug.LogError("Boss weapon portal probe FAIL: altar interaction"); yield break; }
        var bosses = (List<Enemy>)typeof(InteractableBossSpawner).GetField("bossEnemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(altar);
        if (bosses == null || bosses.Count != 1) { Debug.LogError($"Boss weapon portal probe FAIL: boss count={bosses?.Count ?? -1}"); yield break; }
        Enemy boss = bosses[0]; int hitCount = 0; float damage = 0f;
        Enemy.A_Damage += Record;
        Debug.Log($"Boss weapon portal probe START: boss={boss.enemyData.enemyName} hp={boss.hp:F1} damageMultiplier={inventory.playerStats.GetStat(EStat.DamageMultiplier):F1}");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && altar != null && !altar.portal.activeInHierarchy) yield return null;
        Enemy.A_Damage -= Record;
        if (altar == null || !altar.portal.activeInHierarchy || hitCount == 0) { Debug.LogError($"Boss weapon portal probe FAIL: portal={(altar != null && altar.portal.activeInHierarchy)} hits={hitCount} damage={damage:F1}"); yield break; }
        Debug.Log($"Boss weapon portal probe DEFEATED: firestaffHits={hitCount} damage={damage:F1} portalActive=true");
        var portal = altar.portal.GetComponent<InteractablePortal>();
        if (portal == null || !portal.Interact()) { Debug.LogError("Boss weapon portal probe FAIL: portal interaction"); yield break; }
        deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MapController.index == index || MyPlayer.Instance == null || MyPlayer.Instance == player || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        bool passed = MapController.index == index + 1 && MyPlayer.Instance != null && MyPlayer.Instance != player && MyPlayer.Instance.hasStarted && ReferenceEquals(inventory, MyPlayer.Instance.inventory);
        Debug.Log($"Boss weapon portal probe {(passed ? "PASS" : "FAIL")}: stage={MapController.index}, previousStage={index}, sameInventory={(MyPlayer.Instance != null && ReferenceEquals(inventory, MyPlayer.Instance.inventory))}, scene={SceneManager.GetActiveScene().name}");
        void Record(Enemy enemy, DamageContainer dc) { if (enemy == boss && dc.damageSource == "FireStaff") { hitCount++; damage += dc.damage; Debug.Log($"Boss weapon portal probe HIT: source={dc.damageSource} damage={dc.damage:F1}"); } }
    }
}
#endif
