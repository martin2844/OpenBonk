#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Explicitly requested diagnostic for disposable emulator runs only.
internal sealed class AndroidChallengeShrineProbe : MonoBehaviour
{
    private static bool waitForPlayerInput;
    private static bool walkToReward;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_challenge_shrine_probe", false)) return;
        waitForPlayerInput = intent.Call<bool>("getBooleanExtra", "megabonk_challenge_manual_input", false);
        walkToReward = intent.Call<bool>("getBooleanExtra", "megabonk_challenge_walk_reward", false);
        var root = new GameObject("Challenge shrine probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidChallengeShrineProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || EffectManager.Instance == null || MapController.currentMap == null)) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("Challenge shrine probe: game not ready"); yield break; }
        var manager = EnemyManager.Instance; bool oldWaves = manager.enabledWaves; manager.enabledWaves = false;
        var weapons = new List<WeaponBase>();
        foreach (var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); weapons.Add(weapon); }
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
        bool oldInvincible = (bool)invincible.GetValue(MyPlayer.Instance); invincible.SetValue(MyPlayer.Instance, true);
        var shrine = FindObjectsByType<InteractableShrineChallenge>(FindObjectsSortMode.None).FirstOrDefault(s => s.CanInteract());
        bool spawnedPrefab = false;
        if (shrine == null && MapController.currentMap.shrines != null)
        {
            var prefab = MapController.currentMap.shrines.FirstOrDefault(p => p != null && p.GetComponent<InteractableShrineChallenge>() != null);
            if (prefab != null) { shrine = Instantiate(prefab, MyPlayer.Instance.transform.position + Vector3.forward * 3f, Quaternion.identity).GetComponent<InteractableShrineChallenge>(); spawnedPrefab = true; }
        }
        if (shrine == null) { Debug.LogError("Challenge shrine probe: original shrine unavailable"); Cleanup(); yield break; }
        var input = FindFirstObjectByType<PlayerInput>();
        var detector = input != null ? input.detectInteractables : null;
        var playerBody = MyPlayer.Instance.GetComponent<Rigidbody>();
        if (detector == null || playerBody == null) { Debug.LogError("Challenge shrine probe: player detector or body unavailable"); Cleanup(); yield break; }
        Vector3 originalPlayerPosition = playerBody.position, originalPlayerVelocity = playerBody.velocity;
        playerBody.position = shrine.transform.position + Vector3.back * 2f; playerBody.velocity = Vector3.zero;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var selected = (BaseInteractable)typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(detector);
        int completed = 0, died = 0, released = 0; var tracked = new HashSet<Enemy>();
        Action onCompleted = () => completed++;
        Action<Enemy, Assets.Scripts.Actors.DamageContainer> onDeath = (enemy, dc) => { if (tracked.Contains(enemy)) died++; };
        Action<Enemy> onRelease = enemy => { if (tracked.Contains(enemy)) released++; };
        InteractableShrineChallenge.A_Completed += onCompleted; Enemy.A_EnemyDied += onDeath; Enemy.A_EnemyReleasedFromPool += onRelease;
        int chestCount() => FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => EffectManager.Instance.openChestNormal != null && t.name == EffectManager.Instance.openChestNormal.name + "(Clone)");
        int before = chestCount();
        if (selected == shrine)
        {
            if (waitForPlayerInput)
            {
                Debug.Log("Challenge shrine probe: READY for external interaction input");
                float inputDeadline = Time.realtimeSinceStartup + 45f;
                while (Time.realtimeSinceStartup < inputDeadline && shrine.CanInteract())
                {
                    for (int button = 0; button < 20; button++)
                        if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + button)))
                            Debug.Log($"Challenge shrine probe: joystick button {button} down, selected={typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(detector) == (object)shrine}, distance={Vector3.Distance(playerBody.position, shrine.transform.position):F2}");
                    yield return null;
                }
            }
            else
            {
                typeof(PlayerInput).GetField("interacting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(input, true);
                yield return new WaitForFixedUpdate();
                yield return null;
            }
        }
        playerBody.position = originalPlayerPosition; playerBody.velocity = originalPlayerVelocity; Physics.SyncTransforms();
        var enemies = (HashSet<Enemy>)typeof(InteractableShrineChallenge).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shrine);
        if (enemies != null) tracked.UnionWith(enemies);
        bool detected = selected == shrine;
        bool started = !shrine.CanInteract();
        bool allChallenge = tracked.Count > 0 && tracked.All(e => e != null && e.IsChallenge());
        bool effectActive = shrine.fx != null && shrine.fx.activeSelf;
        Debug.Log($"Challenge shrine probe: prefabFallback={spawnedPrefab}, externalInput={waitForPlayerInput}, detected={detected}, started={started}, enemies={tracked.Count}, allChallenge={allChallenge}, fx={effectActive}");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && tracked.Any(e => e != null && !e.CanTakeDamage())) yield return null;
        foreach (Enemy enemy in tracked) if (enemy != null && enemy.CanTakeDamage()) enemy.Kill("ChallengeShrineProbe");
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && completed == 0) yield return null;
        int after = chestCount();
        bool passed = detected && started && allChallenge && effectActive && died == tracked.Count &&
            released == tracked.Count && completed == 1 && after == before + 1;
        string result = $"Challenge shrine probe {(passed ? "PASS" : "FAIL")}: enemies={tracked.Count}, died={died}, released={released}, completed={completed}, chestsBefore={before}, chestsAfter={after}";
        if (passed) Debug.Log(result); else Debug.LogError(result);
        if (passed)
        {
            var reward = FindObjectsByType<OpenChest>(FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.activeInHierarchy && c.name == EffectManager.Instance.openChestNormal.name + "(Clone)");
            var player = MyPlayer.Instance;
            var body = player != null ? player.GetComponent<Rigidbody>() : null;
            if (reward == null || body == null) Debug.LogError("Challenge shrine reward probe FAIL: reward or player body missing");
            else
            {
                var settings = SaveManager.Instance.config.cfGameSettings;
                int oldSkip = settings.skip_chest_animation; settings.skip_chest_animation = 1;
                Vector3 oldPosition = body.position, oldVelocity = body.velocity;
                int openEvents = 0, itemEvents = 0; EItem awarded = default;
                Action opened = () => openEvents++;
                Action<EItem> itemAdded = item => { itemEvents++; awarded = item; };
                OpenChest.A_Open += opened; ItemInventory.A_ItemAdded += itemAdded;
                Vector3 destination = reward.transform.position;
                if (walkToReward)
                {
                    Vector3 origin = destination + Vector3.back * 4f + Vector3.up * 20f;
                    var capsule = body.GetComponent<CapsuleCollider>();
                    if (!Physics.Raycast(origin, Vector3.down, out var ground, 40f, GameManager.Instance.whatIsGround) || capsule == null)
                    {
                        Debug.LogError("Challenge shrine reward probe INCONCLUSIVE: no approach ground or capsule");
                        OpenChest.A_Open -= opened; ItemInventory.A_ItemAdded -= itemAdded;
                        settings.skip_chest_animation = oldSkip;
                        InteractableShrineChallenge.A_Completed -= onCompleted; Enemy.A_EnemyDied -= onDeath; Enemy.A_EnemyReleasedFromPool -= onRelease;
                        Cleanup(); yield break;
                    }
                    body.position = ground.point + Vector3.up * (capsule.height * .5f + .1f);
                }
                else body.position = destination;
                body.velocity = Vector3.zero; Physics.SyncTransforms();
                var rewardCollider = reward.GetComponent<Collider>();
                var playerColliders = body.GetComponentsInChildren<Collider>();
                Debug.Log($"Challenge reward diagnostic: rewardPosition={reward.transform.position} playerPosition={body.position} gameTime={MyTime.time:F2} scale={Time.timeScale:F2} rewardCollider={(rewardCollider != null ? rewardCollider.bounds.ToString() : "missing")} trigger={(rewardCollider != null && rewardCollider.isTrigger)} enabled={(rewardCollider != null && rewardCollider.enabled)}");
                foreach (var collider in playerColliders)
                    Debug.Log($"Challenge reward diagnostic: playerCollider={collider.name} layer={collider.gameObject.layer} enabled={collider.enabled} trigger={collider.isTrigger} bounds={collider.bounds} overlaps={(rewardCollider != null && collider.bounds.Intersects(rewardCollider.bounds))}");
                float end = Time.realtimeSinceStartup + 20f, nextReport = Time.realtimeSinceStartup + 2f;
                float startDistance = Vector3.Distance(body.position, destination), nearestDistance = startDistance;
                var horizontal = typeof(PlayerInput).GetField("moveHorizontal", BindingFlags.Instance | BindingFlags.NonPublic);
                var vertical = typeof(PlayerInput).GetField("moveVertical", BindingFlags.Instance | BindingFlags.NonPublic);
                while (Time.realtimeSinceStartup < end && openEvents == 0)
                {
                    if (walkToReward)
                    {
                        Vector3 world = destination - body.position; world.y = 0f;
                        nearestDistance = Mathf.Min(nearestDistance, world.magnitude);
                        Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, input.cameraRotation.y, 0f)) * world.normalized;
                        horizontal.SetValue(input, local.x); vertical.SetValue(input, local.z);
                    }
                    if (Time.realtimeSinceStartup >= nextReport)
                    {
                        Debug.Log($"Challenge reward diagnostic: playerPosition={body.position} rewardPosition={reward.transform.position} gameTime={MyTime.time:F2} scale={Time.timeScale:F2} rewardActive={reward.gameObject.activeInHierarchy}");
                        nextReport += 2f;
                    }
                    yield return new WaitForEndOfFrame();
                }
                if (walkToReward) { horizontal.SetValue(input, 0f); vertical.SetValue(input, 0f); }
                bool collected = openEvents == 1 && itemEvents == 1 && !reward.gameObject.activeSelf && (!walkToReward || startDistance > 3f && nearestDistance < 2f);
                Debug.Log($"Challenge shrine reward probe {(collected ? "PASS" : "FAIL")}: walked={walkToReward} startDistance={startDistance:F2} nearestDistance={nearestDistance:F2} opens={openEvents} itemEvents={itemEvents} awarded={awarded} active={reward.gameObject.activeSelf}");
                OpenChest.A_Open -= opened; ItemInventory.A_ItemAdded -= itemAdded;
                settings.skip_chest_animation = oldSkip; body.position = oldPosition; body.velocity = oldVelocity; Physics.SyncTransforms();
            }
        }
        InteractableShrineChallenge.A_Completed -= onCompleted; Enemy.A_EnemyDied -= onDeath; Enemy.A_EnemyReleasedFromPool -= onRelease;
        Cleanup();
        void Cleanup() { if (MyPlayer.Instance != null) invincible.SetValue(MyPlayer.Instance, oldInvincible); if (manager != null) manager.enabledWaves = oldWaves; foreach (var weapon in weapons) if (weapon != null) weapon.Enable(); }
    }
}
#endif
