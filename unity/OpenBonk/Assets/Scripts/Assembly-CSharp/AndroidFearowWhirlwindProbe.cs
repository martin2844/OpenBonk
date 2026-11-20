#if UNITY_ANDROID && !UNITY_EDITOR && (!OPENBONK_BETA_NO_PROBES || OPENBONK_DISTRICT_QA)
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

/// <summary>Opt-in real Fearow AI/cadence check. Never advances the attack clock.</summary>
internal sealed class AndroidFearowWhirlwindProbe : MonoBehaviour
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<WeaponBase> disabledWeapons = new List<WeaponBase>();
    private MyPlayer player;
    private EnemyManager manager;
    private Enemy fearow;
    private uint fearowId;
    private bool oldInvincible, oldWaves, modified, probePaused;
    private Vector3 oldPosition;
    private FieldInfo invincible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "openbonk_fearow_whirlwind_probe", false)) return;
        var root = new GameObject("Fearow whirlwind probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidFearowWhirlwindProbe>();
    }

    private IEnumerator Start()
    {
        var run = Run();
        while (true)
        {
            bool more = false;
            System.Exception error = null;
            try { more = run.MoveNext(); }
            catch (System.Exception failure) { error = failure; }
            if (error != null) { Fail(error.ToString()); yield break; }
            if (!more) yield break;
            yield return run.Current;
        }
    }

    private IEnumerator Run()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && !Ready()) yield return null;
        if (!Ready()) { Fail("Nimbo stage 1 unavailable"); yield break; }
        player = MyPlayer.Instance; manager = EnemyManager.Instance;
        // Mini-boss events use the timeline's rotating miniboss roster;
        // event.enemies is the swarm roster and is ignored for these events.
        var roster = MapController.currentStage.stageTimeline.minibosses;
        EnemyData data = roster != null && roster.Count > 0 ? DataManager.Instance.GetEnemyData(roster[0]) : null;
        if (data is not OpenBonkMapEnemyData custom || !custom.whirlwindAttack ||
            data.GetName() != "Fearow" || data.minStayAtDistance < 3f || data.maxStayAtDistance > 38f)
        { Fail("first miniboss does not declare ranged Fearow whirlwind"); yield break; }
        var generator = FindFirstObjectByType<ProceduralTileGeneration>();
        Vector3 center = Vector3.zero;
        bool found = false;
        if (generator?.flatTiles != null)
            foreach (var tile in generator.flatTiles)
            {
                if (tile == null) continue;
                Vector3 point = tile.transform.position;
                if (!Physics.Raycast(point + Vector3.up * 3, Vector3.down, out var floor, 8,
                        GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore)) continue;
                Vector3 ahead = floor.point + Vector3.forward * 12f;
                if (!Physics.Raycast(ahead + Vector3.up * 3, Vector3.down, out var other, 8,
                        GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore) ||
                    Mathf.Abs(floor.point.y - other.point.y) > .3f) continue;
                center = floor.point; found = true; break;
            }
        if (!found) { Fail("flat 12m approach unavailable"); yield break; }
        oldPosition = player.playerMovement.GetRbFeetPosition();
        invincible = typeof(MyPlayer).GetField("isInvincible", Private);
        oldInvincible = (bool)invincible.GetValue(player); oldWaves = manager.enabledWaves;
        modified = true;
        invincible.SetValue(player, true); manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values)
            if (weapon.enabled) { weapon.Disable(); disabledWeapons.Add(weapon); }
        player.playerMovement.TeleportPlayerBackToBounds(center + Vector3.up * 1.5f);
        player.playerInput.SetSpawnDirection(Vector3.forward, 3f);
        Physics.SyncTransforms();
        fearow = manager.SpawnEnemy(data, center + Vector3.forward * 12f + Vector3.up * 2.5f,
            0, true, EEnemyFlag.SummonerMiniboss, false);
        if (fearow == null) { Fail("real miniboss spawn rejected"); yield break; }
        uint spawnedId = fearowId = fearow.id;
        float startedAt = MyTime.time;
        var attackField = typeof(Actors.Enemies.EnemyMovementRb).GetField("customWhirlwind", Private);
        bool sawWarning = false;
        OpenBonkHostileWhirlwind storm = null;
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && storm == null && fearow != null && fearow.id == spawnedId)
        {
            var attack = attackField.GetValue(fearow.enemyMovement) as OpenBonkEnemyWhirlwind;
            if (!sawWarning && attack?.IsWindingUp == true)
            {
                sawWarning = true;
                Debug.Log($"Fearow whirlwind probe warning: elapsed={MyTime.time - startedAt:F3}, distance={Vector3.Distance(fearow.transform.position, player.transform.position):F2}, noClockAcceleration=true");
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot("fearow-whirlwind-warning.png");
            }
            if (attack != null) storm = attack.Active;
            yield return null;
        }
        if (!sawWarning || storm == null) { Fail("ordinary Fearow AI did not telegraph and cast within 45s"); yield break; }
        float spawnedAt = MyTime.time;
        if (spawnedAt - startedAt < 12f || storm.Owner != fearow ||
            storm.GetComponentsInChildren<ProjectileBase>(true).Length != 0 ||
            storm.GetComponentsInChildren<Collider>(true).Length != 0)
        { Fail("cadence, hostile ownership or isolated visual mismatch"); yield break; }
        var source = OpenBonkHostileWhirlwind.NativeProjectile();
        var nativeRenderers = source.GetComponentsInChildren<ParticleSystemRenderer>(true);
        var renderers = storm.GetComponentsInChildren<ParticleSystemRenderer>(true);
        bool sameVisual = renderers.Length == nativeRenderers.Length;
        for (int i = 0; sameVisual && i < renderers.Length; i++)
            sameVisual = renderers[i].mesh == nativeRenderers[i].mesh && renderers[i].sharedMaterial == nativeRenderers[i].sharedMaterial;
        if (!sameVisual) { Fail("whirlwind differs from native player tornado visual"); yield break; }
        // Leave the locked attack path before taking the capture: otherwise
        // the hero can hide the tornado directly between Fearow and camera.
        player.playerMovement.TeleportPlayerBackToBounds(center + Vector3.right * 4f + Vector3.up * 1.5f);
        player.playerInput.SetSpawnDirection(storm.transform.position - player.transform.position, 3f);
        yield return new WaitForSecondsRealtime(.6f);
        if (storm == null) { Fail("whirlwind ended before visible travel"); yield break; }
        yield return new WaitForEndOfFrame();
        var camera = PlayerCamera.Instance != null ? PlayerCamera.Instance.camera : Camera.main;
        int liveParticles = 0, visibleMeshParticles = 0;
        var scratch = new ParticleSystem.Particle[256];
        if (camera != null)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var renderer in renderers)
            {
                var system = renderer.GetComponent<ParticleSystem>();
                bool active = renderer.enabled && renderer.gameObject.activeInHierarchy &&
                    renderer.bounds.size.sqrMagnitude > .01f && renderer.transform.lossyScale.sqrMagnitude > .01f;
                bool inView = active && renderer.isVisible && GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
                int count = system.GetParticles(scratch), alive = 0;
                for (int i = 0; i < count; i++)
                    if (scratch[i].remainingLifetime > 0 && scratch[i].GetCurrentSize3D(system).sqrMagnitude > .01f &&
                        scratch[i].GetCurrentColor(system).a > 8) alive++;
                if (active) liveParticles += alive;
                if (inView && renderer.mesh != null) visibleMeshParticles += alive;
                Debug.Log($"Fearow particle evidence: name={system.name}, active={active}, visible={inView}, live={alive}, time={system.time:F3}, scale={renderer.transform.lossyScale}, bounds={renderer.bounds}, instancing={renderer.enableGPUInstancing}, shader={renderer.sharedMaterial.shader.name}");
            }
        }
        if (liveParticles == 0 || visibleMeshParticles == 0)
        { Fail("no active visible native tornado mesh particles"); yield break; }
        ScreenCapture.CaptureScreenshot("fearow-whirlwind.png");
        Vector3 before = storm.transform.position; float age = storm.Age;
        MyTime.Pause(); probePaused = true;
        yield return new WaitForSecondsRealtime(.5f);
        bool pausePass = storm != null && storm.transform.position == before && storm.Age == age;
        MyTime.Unpause(); probePaused = false;
        if (!pausePass) { Fail("pause did not freeze whirlwind travel and lifetime"); yield break; }
        Debug.Log($"Fearow whirlwind probe native visual: elapsed={spawnedAt - startedAt:F3}, visible=true matchingParticleSystems={renderers.Length}, liveParticles={liveParticles}, visibleMeshParticles={visibleMeshParticles}, playerProjectileScripts=0 colliders=0 pause=true");
        deadline = Time.realtimeSinceStartup + 7f;
        while (storm != null && Time.realtimeSinceStartup < deadline) yield return null;
        if (storm != null) { Fail("whirlwind outlived bounded lifetime"); yield break; }
        Debug.Log("Fearow whirlwind probe PASS: real ranged Fearow casts at ordinary cadence, visible native tornado visual, telegraph, isolated hostile behavior, pause freeze and bounded cleanup");
        Cleanup(); Destroy(gameObject);
    }

    private static bool Ready() => MapController.index == 0 &&
        MapController.currentMap is OpenBonkExternalMapData map && map.GetInternalName() == "distrito-nimbo:distrito-nimbo" &&
        MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && !MyPlayer.Instance.isTeleporting &&
        MyPlayer.Instance.inventory?.weaponInventory != null && GameManager.Instance != null &&
        GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && !MyTime.paused &&
        EnemyManager.Instance?.summonerController != null;

    private void Fail(string reason)
    {
        Debug.LogError("Fearow whirlwind probe FAIL: " + reason);
        Cleanup(); Destroy(gameObject);
    }

    private void Cleanup()
    {
        if (probePaused) { MyTime.Unpause(); probePaused = false; }
        if (!modified) return;
        modified = false;
        if (fearow != null && fearow.id == fearowId && fearow.gameObject.activeInHierarchy) fearow.ReleaseToPool();
        fearow = null;
        if (player != null)
        {
            invincible.SetValue(player, oldInvincible);
            player.playerMovement.TeleportPlayerBackToBounds(oldPosition + Vector3.up * 1.5f);
        }
        if (manager != null) manager.enabledWaves = oldWaves;
        foreach (var weapon in disabledWeapons) if (weapon != null) weapon.Enable();
        disabledWeapons.Clear();
    }

    private void OnDestroy() => Cleanup();
}
#endif
