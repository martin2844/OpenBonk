#if UNITY_ANDROID && !UNITY_EDITOR && (!OPENBONK_BETA_NO_PROBES || OPENBONK_DISTRICT_QA)
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class AndroidDistrictRunProbe : MonoBehaviour
{
    private bool moaiPassed, balancePassed;
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
            while (Time.realtimeSinceStartup < until && !StageReady(index, previous))
            {
                // Boss XP can open another upgrade while the portal's scaled
                // teleport delay is running on the previous stage.
                if (index > 0) AcceptPendingReward();
                yield return null;
            }
            if (!StageReady(index, previous)) { Fail("stage not ready", index); yield break; }
            var player = MyPlayer.Instance;
            var map = (OpenBonkExternalMapData)MapController.currentMap;
            var stage = MapController.currentStage;
            var manager = EnemyManager.Instance;
            if (index == 0)
            {
                MapController.runConfig.mapTierIndex = 2;
                inventory = player.inventory;
                yield return new WaitForEndOfFrame();
                LogStreetscapeRendering();
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
                if (map.moaiVisuals.ContainsKey(stage))
                {
                    yield return CheckMoaiVisualAndReward(player);
                    if (!moaiPassed) yield break;
                }
                if (map.balanceVisuals.ContainsKey(stage))
                {
                    yield return CheckBalanceVisualAndReward(player);
                    if (!balancePassed) yield break;
                }
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
                AcceptPendingReward();
                foreach (var enemy in manager.enemies.Values)
                    if (enemy != null && enemy.IsStageBoss() && enemy.enemyData == stage.stageTimeline.boss && enemy.CanTakeDamage())
                    { boss = enemy; break; }
                yield return null;
            }
            if (boss == null) { Fail("no fightable local boss", index); yield break; }
            string bossName = boss.enemyData.GetName();
            bool volley = false;
            until = Time.realtimeSinceStartup + 12f;
            while (!volley && Time.realtimeSinceStartup < until)
            {
                AcceptPendingReward();
                volley = GameObject.Find("NimboBossCharge") != null || GameObject.Find("NimboBossBolt") != null;
                yield return null;
            }
            if (!volley) { Fail("boss did not wind up a volley", index); yield break; }
            Debug.Log($"District probe volley: boss={bossName} projectile=true");
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
            while (!altar.portal.activeInHierarchy && Time.realtimeSinceStartup < until)
            {
                AcceptPendingReward();
                yield return null;
            }
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
                while (!saved && Time.realtimeSinceStartup < until)
                {
                    AcceptPendingReward();
                    yield return null;
                }
                SaveManager.A_ProgressionSaved -= MarkSaved;
                var progress = SaveManager.Instance.progression.menuMeta.GetMapProgress(map.eMap);
                if (!saved || !progress.IsTierCompleted(2) || !UiManager.Instance.deathScreen.victoryScreen.activeInHierarchy)
                { Fail("missing saved victory or results screen", index); yield break; }
                Debug.Log("District probe PASS: three local encounters, bosses, rewards, two transitions and saved victory");
            }
            previous = player;
        }
    }

    private IEnumerator CheckMoaiVisualAndReward(MyPlayer player)
    {
        var windows = UiManager.Instance?.encounterWindows;
        if (windows == null) { Fail("missing reward windows for Moai", 0); yield break; }
        float until = Time.realtimeSinceStartup + 8f;
        while (windows.HasEncounter() && Time.realtimeSinceStartup < until)
        {
            AcceptPendingReward();
            yield return null;
        }
        if (windows.HasEncounter()) { Fail("earlier reward blocked Moai check", 0); yield break; }

        InteractableShrineMoai shrine = null;
        float nearest = float.PositiveInfinity;
        foreach (var candidate in FindObjectsOfType<InteractableShrineMoai>())
        {
            float distance = (candidate.transform.position - player.transform.position).sqrMagnitude;
            if (candidate.done || distance >= nearest) continue;
            shrine = candidate; nearest = distance;
        }
        if (shrine == null) { Fail("declared Moai visual has no shrine", 0); yield break; }
        var visual = shrine.transform.Find("ExternalMoaiVisual");
        var head = shrine.GetComponent<MeshRenderer>();
        var renderer = visual != null ? visual.GetComponent<MeshRenderer>() : null;
        // Native shrine +Z follows the sampled ground normal, including ramps.
        // Imported +Y must follow that axis, rather than world-up on every seed.
        float alignment = visual == null ? -1f : Vector3.Dot(visual.up, shrine.transform.forward);
        float groundAngle = Vector3.Angle(shrine.transform.forward, Vector3.up);
        if (renderer == null || !renderer.enabled || !visual.gameObject.activeInHierarchy ||
            head == null || head.enabled || alignment < .999f || groundAngle > 44.1f)
        { Fail($"Moai override: visual={visual != null} renderer={renderer != null && renderer.enabled} nativeHead={head == null || head.enabled} groundAlignment={alignment:F4} groundAngle={groundAngle:F2}", 0); yield break; }
        var icon = shrine.minimapIcon; var alert = shrine.alertIcon;
        if (icon == null || alert == null || shrine.GetComponent<Collider>() == null)
        { Fail("Moai lost native icons or collision", 0); yield break; }

        var bounds = renderer.bounds;
        var outward = visual.forward; outward.y = 0; outward.Normalize();
        if (outward.sqrMagnitude < .5f) outward = Vector3.forward;
        var approach = bounds.center + outward * Mathf.Max(5f, bounds.size.y * .75f);
        approach.y = bounds.min.y + .5f;
        player.playerMovement.TeleportPlayerBackToBounds(approach);
        player.playerInput.SetSpawnDirection(-outward, -8f);
        Physics.SyncTransforms();
        until = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < until)
        {
            AcceptPendingReward();
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        var camera = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : Camera.main;
        if (camera == null || !GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), bounds))
        { Fail("Moai statue outside capture camera", 0); yield break; }
        string capture = System.IO.Path.Combine(Application.persistentDataPath, "mewtwo-statue.png");
        ScreenCapture.CaptureScreenshot("mewtwo-statue.png");
        Debug.Log($"District probe Moai visual: groundAligned=true alignment={alignment:F4} groundAngle={groundAngle:F2} nativeHeadVisible=false visible={renderer.isVisible} bounds={bounds} capture={capture}");
        yield return new WaitForEndOfFrame();
        yield return null;

        until = Time.realtimeSinceStartup + 8f;
        while (windows.HasEncounter() && Time.realtimeSinceStartup < until)
        {
            AcceptPendingReward();
            yield return null;
        }
        if (windows.HasEncounter()) { Fail("reward blocked Moai interaction", 0); yield break; }

        bool first = shrine.Interact();
        bool second = shrine.Interact();
        if (!first || second || !shrine.done)
        { Fail("Moai interaction must succeed exactly once", 0); yield break; }
        var level = windows.levelupScreen as LevelupScreen;
        var encounter = level == null ? null : typeof(LevelupScreen).GetField("encounterType", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(level);
        if (level == null || level.window == null || !level.window.activeInHierarchy ||
            encounter is not EEncounter kind || kind != EEncounter.Moai)
        { Fail("Moai did not open its native reward prompt", 0); yield break; }
        until = Time.realtimeSinceStartup + 8f;
        do
        {
            AcceptPendingReward();
            yield return null;
        } while (windows.HasEncounter() && Time.realtimeSinceStartup < until);
        if (windows.HasEncounter() || icon != null || alert != null || shrine != null)
        { Fail("Moai reward or single-use cleanup did not finish", 0); yield break; }
        moaiPassed = true;
        Debug.Log("District probe Moai PASS: external statue visible/ground-aligned, original head hidden, native icons/collider retained, firstInteraction=true secondInteraction=false, native reward accepted");
    }

    private IEnumerator CheckBalanceVisualAndReward(MyPlayer player)
    {
        var windows = UiManager.Instance?.encounterWindows;
        if (windows == null) { Fail("missing reward windows for the Bald Head", 0); yield break; }
        float until = Time.realtimeSinceStartup + 8f;
        while (windows.HasEncounter() && Time.realtimeSinceStartup < until)
        {
            AcceptPendingReward();
            yield return null;
        }
        if (windows.HasEncounter()) { Fail("earlier reward blocked the Bald Head check", 0); yield break; }

        InteractableShrineBalance shrine = null;
        float nearest = float.PositiveInfinity;
        foreach (var candidate in FindObjectsOfType<InteractableShrineBalance>())
        {
            float distance = (candidate.transform.position - player.transform.position).sqrMagnitude;
            if (candidate.done || distance >= nearest) continue;
            shrine = candidate; nearest = distance;
        }
        if (shrine == null) { Fail("declared Bald Head visual has no shrine", 0); yield break; }
        var visual = shrine.transform.Find("ExternalBalanceVisual");
        var head = shrine.GetComponent<MeshRenderer>();
        var renderer = visual != null ? visual.GetComponent<MeshRenderer>() : null;
        // Native shrine +Z follows the sampled ground normal, including ramps.
        // Imported +Y must follow that axis, rather than world-up on every seed.
        float alignment = visual == null ? -1f : Vector3.Dot(visual.up, shrine.transform.forward);
        float groundAngle = Vector3.Angle(shrine.transform.forward, Vector3.up);
        if (renderer == null || !renderer.enabled || !visual.gameObject.activeInHierarchy ||
            head == null || head.enabled || alignment < .999f || groundAngle > 44.1f)
        { Fail($"Bald Head override: visual={visual != null} renderer={renderer != null && renderer.enabled} nativeHead={head == null || head.enabled} groundAlignment={alignment:F4} groundAngle={groundAngle:F2}", 0); yield break; }
        var icon = shrine.minimapIcon; var alert = shrine.alertIcon;
        if (icon == null || alert == null || shrine.GetComponent<Collider>() == null)
        { Fail("Bald Head lost native icons or collision", 0); yield break; }

        var bounds = renderer.bounds;
        var outward = visual.forward; outward.y = 0; outward.Normalize();
        if (outward.sqrMagnitude < .5f) outward = Vector3.forward;
        var approach = bounds.center + outward * Mathf.Max(5f, bounds.size.y * .75f);
        approach.y = bounds.min.y + .5f;
        player.playerMovement.TeleportPlayerBackToBounds(approach);
        player.playerInput.SetSpawnDirection(-outward, -8f);
        Physics.SyncTransforms();
        until = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < until)
        {
            AcceptPendingReward();
            yield return null;
        }
        yield return new WaitForEndOfFrame();
        var camera = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : Camera.main;
        if (camera == null || !GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), bounds))
        { Fail("Moai statue outside capture camera", 0); yield break; }
        string capture = System.IO.Path.Combine(Application.persistentDataPath, "mewtwo-bald-head.png");
        ScreenCapture.CaptureScreenshot("mewtwo-bald-head.png");
        Debug.Log($"District probe Bald Head visual: groundAligned=true alignment={alignment:F4} groundAngle={groundAngle:F2} nativeHeadVisible=false visible={renderer.isVisible} bounds={bounds} capture={capture}");
        yield return new WaitForEndOfFrame();
        yield return null;

        until = Time.realtimeSinceStartup + 8f;
        while (windows.HasEncounter() && Time.realtimeSinceStartup < until)
        {
            AcceptPendingReward();
            yield return null;
        }
        if (windows.HasEncounter()) { Fail("reward blocked Bald Head interaction", 0); yield break; }

        bool first = shrine.Interact();
        bool second = shrine.Interact();
        if (!first || second || !shrine.done)
        { Fail("Bald Head interaction must succeed exactly once", 0); yield break; }
        // Balance uses the generic encounter window rather than the level-up one.
        var generic = windows.genericEncounterWindow as EncounterUi;
        if (generic == null || !generic.gameObject.activeInHierarchy)
        { Fail("Bald Head did not open its native reward prompt", 0); yield break; }
        yield return new WaitForEndOfFrame();
        generic.ChooseOffer(0);
        until = Time.realtimeSinceStartup + 8f;
        do
        {
            AcceptPendingReward();
            yield return null;
        } while (windows.HasEncounter() && Time.realtimeSinceStartup < until);
        if (windows.HasEncounter() || icon != null || alert != null || shrine != null)
        { Fail("Bald Head reward or single-use cleanup did not finish", 0); yield break; }
        balancePassed = true;
        Debug.Log("District probe Bald Head PASS: external statue visible/ground-aligned, original head hidden, native icons/collider retained, firstInteraction=true secondInteraction=false, native reward accepted");
    }

    private static bool StageReady(int index, MyPlayer previous) =>
        MapController.index == index && MapController.currentMap is OpenBonkExternalMapData map &&
        map.GetInternalName() == "distrito-nimbo:distrito-nimbo" && MyPlayer.Instance != null &&
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

    private static void LogStreetscapeRendering()
    {
        GameObject streets = null;
        // Include inactive scene objects so a hidden parent remains diagnosable.
        foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            if (candidate.name == "Nimbo streetscape" && candidate.scene.IsValid() && candidate.scene.isLoaded)
            { streets = candidate; break; }
        var camera = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : Camera.main;
        if (streets == null || camera == null)
        {
            Debug.LogError($"District render probe: streetscape={(streets != null)}, camera={(camera != null)}");
            return;
        }
        var root = streets.transform;
        var parent = root.parent;
        var renderers = streets.GetComponentsInChildren<MeshRenderer>(true);
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        int active = 0, enabled = 0, inMask = 0, inFrustum = 0, visible = 0;
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.activeInHierarchy) active++;
            if (renderer.enabled && !renderer.forceRenderingOff) enabled++;
            if ((camera.cullingMask & (1 << renderer.gameObject.layer)) != 0) inMask++;
            if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) inFrustum++;
            if (renderer.isVisible) visible++;
        }
        Debug.Log($"District render probe root: scene={streets.scene.name}, active={streets.activeSelf}/{streets.activeInHierarchy}, position={root.position}, scale={root.lossyScale}, parent={(parent != null ? parent.name : "none")}, parentActive={(parent == null || parent.gameObject.activeInHierarchy)}, parentScale={(parent != null ? parent.lossyScale : Vector3.one)}, renderers={renderers.Length}, activeRenderers={active}, enabledRenderers={enabled}, inMask={inMask}, inFrustum={inFrustum}, visibleAnyCamera={visible}");
        Debug.Log($"District render probe camera: name={camera.name}, scene={camera.gameObject.scene.name}, active={camera.gameObject.activeInHierarchy}, enabled={camera.enabled}, mask={camera.cullingMask}, position={camera.transform.position}, rotation={camera.transform.eulerAngles}, clip={camera.nearClipPlane}/{camera.farClipPlane}, occlusion={camera.useOcclusionCulling}, path={camera.actualRenderingPath}");
        System.Array.Sort(renderers, (left, right) =>
            left.bounds.SqrDistance(camera.transform.position).CompareTo(right.bounds.SqrDistance(camera.transform.position)));
        for (int i = 0; i < Mathf.Min(3, renderers.Length); i++)
        {
            var renderer = renderers[i];
            var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            var material = renderer.sharedMaterial;
            var shader = material != null ? material.shader : null;
            Debug.Log($"District render probe nearest {i}: name={renderer.name}, scene={renderer.gameObject.scene.name}, active={renderer.gameObject.activeSelf}/{renderer.gameObject.activeInHierarchy}, enabled={renderer.enabled}, forceOff={renderer.forceRenderingOff}, visibleAnyCamera={renderer.isVisible}, layer={renderer.gameObject.layer}, maskAllows={((camera.cullingMask & (1 << renderer.gameObject.layer)) != 0)}, inFrustum={GeometryUtility.TestPlanesAABB(planes, renderer.bounds)}, layerCullDistance={camera.layerCullDistances[renderer.gameObject.layer]}, position={renderer.transform.position}, scale={renderer.transform.lossyScale}, bounds={renderer.bounds}, mesh={(mesh != null ? mesh.name : "none")}, vertices={(mesh != null ? mesh.vertexCount : 0)}, meshBounds={(mesh != null ? mesh.bounds.ToString() : "none")}, staticBatch={renderer.isPartOfStaticBatch}, cast={renderer.shadowCastingMode}, material={(material != null ? material.name : "none")}, shader={(shader != null ? shader.name : "none")}, supported={(shader != null && shader.isSupported)}, passes={(material != null ? material.passCount : 0)}, instancing={(material != null && material.enableInstancing)}");
        }
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
