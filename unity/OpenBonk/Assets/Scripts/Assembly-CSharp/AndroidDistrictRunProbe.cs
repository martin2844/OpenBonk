#if UNITY_ANDROID && !UNITY_EDITOR && (!OPENBONK_BETA_NO_PROBES || OPENBONK_DISTRICT_QA)
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class AndroidDistrictRunProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" ||
            !intent.Call<bool>("getBooleanExtra", "openbonk_district_run_probe", false)) return;
        var root = new GameObject("District run probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidDistrictRunProbe>();
    }

    private IEnumerator Start()
    {
        MyPlayer previous = null;
        PlayerInventory inventory = null;
        for (int index = 0; index < 3; index++)
        {
            float until = Time.realtimeSinceStartup + 90;
            while (Time.realtimeSinceStartup < until && !StageReady(index, previous)) yield return null;
            if (!StageReady(index, previous)) { Fail("stage not ready", index); yield break; }
            var player = MyPlayer.Instance;
            var map = (OpenBonkExternalMapData)MapController.currentMap;
            var stage = MapController.currentStage;
            var manager = EnemyManager.Instance;
            if (index == 0)
            {
                MapController.runConfig.mapTierIndex = 2;
                inventory = player.inventory;
            }
            if (!ReferenceEquals(inventory, player.inventory)) { Fail("inventory lost", index); yield break; }
            typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(player, true);
            typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(player, MyTime.time + 500f);

            if (!map.arenaLandmarks.TryGetValue(stage, out var landmark) ||
                GameObject.Find(landmark.name + "(Clone)") == null ||
                !map.bossRewards.TryGetValue(stage, out var reward))
            { Fail("missing arena landmark or reward", index); yield break; }

            bool localWave = false;
            until = Time.realtimeSinceStartup + 26f;
            while (!localWave && Time.realtimeSinceStartup < until)
            {
                AcceptPendingReward();
                foreach (var enemy in manager.enemies.Values)
                {
                    if (enemy == null) continue;
                    if (enemy.enemyData is not OpenBonkMapEnemyData) { Fail("inherited spawn", index); yield break; }
                    localWave = true;
                }
                yield return null;
            }
            if (!localWave) { Fail("no local mobs", index); yield break; }
            Debug.Log($"District probe stage {index + 1}: wave=true landmark={landmark.name}");
            yield return new WaitForSecondsRealtime(2f);
            manager.enabledWaves = false;

            if (index == 0)
            {
                InteractablePot capsule = null;
                foreach (var pot in FindObjectsOfType<InteractablePot>())
                    if (!pot.isSilver && pot.transform.Find("CargoCapsule(Clone)") != null) { capsule = pot; break; }
                if (capsule == null || !capsule.Interact() || GameObject.Find("NimboCapsuleBurst(Clone)") == null)
                { Fail("capsule did not break with its effect", index); yield break; }
                Debug.Log("District probe capsule break=true");
            }

            var altar = FindFirstObjectByType<InteractableBossSpawner>();
            if (altar == null) { Fail("missing altar", index); yield break; }
            var approach = altar.transform.position + LandmarkOffset(altar, landmark);
            player.playerMovement.TeleportPlayerBackToBounds(approach);
            Physics.SyncTransforms();
            if (!altar.Interact()) { Fail("altar rejected interaction", index); yield break; }
            Enemy boss = null;
            until = Time.realtimeSinceStartup + 12f;
            while (boss == null && Time.realtimeSinceStartup < until)
            {
                foreach (var enemy in manager.enemies.Values)
                    if (enemy != null && enemy.IsStageBoss() && enemy.enemyData == stage.stageTimeline.boss && enemy.CanTakeDamage())
                    { boss = enemy; break; }
                yield return null;
            }
            if (boss == null) { Fail("no fightable local boss", index); yield break; }
            string bossName = boss.enemyData.GetName();
            if (index > 0)
            {
                string warning = index == 1 ? "Sweep warning" : "Pulse warning";
                until = Time.realtimeSinceStartup + 19f;
                while (GameObject.Find(warning) == null && Time.realtimeSinceStartup < until)
                {
                    AcceptPendingReward();
                    yield return null;
                }
                if (GameObject.Find(warning) == null) { Fail("no attack telegraph", index); yield break; }
                Debug.Log($"District probe warning: {warning}");
                yield return new WaitForSecondsRealtime(.7f);
            }
            boss.DamageExternal(new DamageContainer(0, "district-test") { damage = boss.maxHp + 1000, enemy = boss });
            until = Time.realtimeSinceStartup + 12f;
            while (!altar.portal.activeInHierarchy && Time.realtimeSinceStartup < until) yield return null;
            if (!altar.portal.activeInHierarchy || inventory.itemInventory.GetItem(reward) == null)
            { Fail("boss death did not grant reward and portal", index); yield break; }
            Debug.Log($"District probe boss {index + 1}: {bossName}, reward=true, portal=true");
            var portal = altar.portal.GetComponent<InteractablePortal>();
            bool saved = false;
            void MarkSaved() { saved = true; }
            if (index == 2) SaveManager.A_ProgressionSaved += MarkSaved;
            if (portal == null || !portal.Interact())
            {
                SaveManager.A_ProgressionSaved -= MarkSaved;
                Fail("portal rejected interaction", index);
                yield break;
            }
            if (index == 2)
            {
                until = Time.realtimeSinceStartup + 15f;
                while (!saved && Time.realtimeSinceStartup < until) yield return null;
                SaveManager.A_ProgressionSaved -= MarkSaved;
                var progress = SaveManager.Instance.progression.menuMeta.GetMapProgress(map.eMap);
                if (!saved || !progress.IsTierCompleted(2) || !UiManager.Instance.deathScreen.victoryScreen.activeInHierarchy)
                { Fail("missing saved victory or results screen", index); yield break; }
                Debug.Log("District probe PASS: three local encounters, bosses, rewards, two transitions and saved victory");
            }
            previous = player;
        }
    }

    private static bool StageReady(int index, MyPlayer previous) =>
        MapController.index == index && MapController.currentMap is OpenBonkExternalMapData map &&
        map.GetInternalName() == "openbonk.distrito-nimbo:distrito-nimbo" && MyPlayer.Instance != null &&
        MyPlayer.Instance != previous && MyPlayer.Instance.hasStarted && !MyPlayer.Instance.isTeleporting &&
        MyPlayer.Instance.inventory != null && GameManager.Instance != null && GameManager.Instance.IsPlaying() &&
        !GameManager.Instance.cutscene && EnemyManager.Instance?.summonerController != null;

    private static Vector3 LandmarkOffset(InteractableBossSpawner altar, GameObject landmark)
    {
        var building = GameObject.Find(landmark.name + "(Clone)").transform.position;
        var away = altar.transform.position - building;
        away.y = 0;
        return away.normalized * 9f + Vector3.up * 1.8f;
    }

    private static void AcceptPendingReward()
    {
        var windows = UiManager.Instance?.encounterWindows;
        if (windows == null || !windows.HasEncounter()) return;
        if (windows.chestWindow is ChestWindowUi chest && chest.gameObject.activeInHierarchy)
        {
            if (chest.b_open.gameObject.activeInHierarchy) chest.OpenButton();
            else if (chest.b_take.gameObject.activeInHierarchy) chest.TakeButton();
            return;
        }
        if (windows.levelupScreen is not LevelupScreen level || level.window == null ||
            !level.window.activeInHierarchy || level.upgradePicker == null) return;
        foreach (var button in level.upgradePicker.buttons)
            if (button != null && button.gameObject.activeInHierarchy && button.GetButton().interactable)
            {
                button.SelectUpgrade();
                return;
            }
    }

    private static void Fail(string reason, int index) => Debug.LogError($"District probe FAIL stage {index + 1}: {reason}");
}
#endif
