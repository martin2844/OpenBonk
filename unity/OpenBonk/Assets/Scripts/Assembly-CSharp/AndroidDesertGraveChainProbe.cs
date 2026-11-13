#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Disposable, opt-in quest-chain observation with forced setup and damage.
internal sealed class AndroidDesertGraveChainProbe : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    bool reload;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool reload = intent.Call<bool>("getBooleanExtra", "megabonk_desert_grave_reload_probe", false);
        if (!reload && !intent.Call<bool>("getBooleanExtra", "megabonk_desert_grave_chain_probe", false)) return;
        var go = new GameObject("Desert grave chain probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidDesertGraveChainProbe>().reload = reload;
    }
    IEnumerator Start() {
        if (reload) {
            float reloadDeadline = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < reloadDeadline && (!SaveManager.loaded || DataManager.Instance == null || SaveManager.Instance == null || SaveManager.Instance.progression == null)) yield return null;
            if (!SaveManager.loaded || DataManager.Instance == null || SaveManager.Instance == null || SaveManager.Instance.progression == null) Debug.LogError("Desert grave reload INCONCLUSIVE: saves unavailable");
            else Debug.Log($"Desert grave reload: achievement={MyAchievements.IsAchievementDone("a_graveyard")}");
            yield break;
        }
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MapController.currentMap == null || MapController.currentMap.eMap != EMap.Desert || MapController.runConfig == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || EffectManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || MapController.currentMap == null || MapController.currentMap.eMap != EMap.Desert || EffectManager.Instance == null || MyAchievements.IsAchievementDone("a_graveyard")) { Debug.LogError("Desert grave chain INCONCLUSIVE: initial state"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", Private).SetValue(player, float.MaxValue);
        CharacterMenu.selectedCharacter = ECharacter.Calcium;
        MapController.runConfig.mapTierIndex = 1;
        typeof(EffectManager).GetMethod("OnMapGenerationComplete", Private).Invoke(EffectManager.Instance, null);
        var graves = FindObjectsByType<InteractableDesertGrave>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var grave = graves.FirstOrDefault(g => g.gameObject.activeSelf);
        if (graves.Length != 4 || grave == null || graves.Count(g => g.gameObject.activeSelf) != 1) { Debug.LogError($"Desert grave chain FAIL: graves={graves.Length} active={graves.Count(g => g.gameObject.activeSelf)}"); yield break; }
        for (int i = 0; i < 4; i++) {
            var next = grave.nextShrine;
            if (next == null || !grave.Interact()) { Debug.LogError($"Desert grave chain FAIL: interaction {i}"); yield break; }
            var field = typeof(InteractableDesertGrave).GetField("myEnemy", Private);
            deadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < deadline && field.GetValue(grave) == null) yield return null;
            var enemy = field.GetValue(grave) as Enemy;
            if (enemy == null) { Debug.LogError($"Desert grave chain FAIL: boss spawn {i}"); yield break; }
            yield return Kill(enemy);
            deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline && !next.gameObject.activeSelf) yield return null;
            if (!next.gameObject.activeSelf) { Debug.LogError($"Desert grave chain FAIL: next reveal {i}"); yield break; }
            Debug.Log($"Desert grave chain: completed={i+1}, next={next.name}");
            if (i < 3) { grave = next.GetComponent<InteractableDesertGrave>(); if (grave == null) { Debug.LogError("Desert grave chain FAIL: next grave missing"); yield break; } }
            else {
                var statue = next.GetComponent<InteractableSkeletonKingStatue>();
                if (statue == null || !statue.Interact()) { Debug.LogError("Desert grave chain FAIL: statue interaction"); yield break; }
                var kingField = typeof(InteractableSkeletonKingStatue).GetField("myEnemy", Private);
                deadline = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < deadline && kingField.GetValue(statue) == null) yield return null;
                var king = kingField.GetValue(statue) as Enemy;
                if (king == null) { Debug.LogError("Desert grave chain FAIL: king spawn"); yield break; }
                yield return Kill(king);
                deadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < deadline && !MyAchievements.IsAchievementDone("a_graveyard")) yield return null;
                Debug.Log($"Desert grave chain {(MyAchievements.IsAchievementDone("a_graveyard") ? "PASS" : "FAIL")}: four graves and king, achievement={MyAchievements.IsAchievementDone("a_graveyard")}");
            }
        }
    }
    static IEnumerator Kill(Enemy enemy) {
        float deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy != null && enemy.CanTakeDamage()) {
            enemy.SetDiagnosticHealth(1f);
            enemy.DamageFromPlayerWeapon(new DamageContainer(1000f, "probe") { enemy = enemy, damage = 1000f, procCoefficient = 0f });
        }
    }
}
#endif
