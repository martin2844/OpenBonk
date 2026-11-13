#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in setup; weapon targeting, enemy combat and shrine completion run normally.
internal sealed class AndroidChallengeCombatProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_challenge_combat_probe", false)) return;
        var go = new GameObject("Challenge combat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidChallengeCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || EffectManager.Instance == null || MapController.currentMap == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || manager == null || EffectManager.Instance == null || MapController.currentMap == null) { Debug.LogError("Challenge combat probe: scene unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var until = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        bool oldInvincible = (bool)invincible.GetValue(player); float oldUntil = (float)until.GetValue(player);
        invincible.SetValue(player, true); until.SetValue(player, MyTime.time + 10000f);
        var shrine = FindObjectsByType<InteractableShrineChallenge>(FindObjectsSortMode.None).FirstOrDefault(s => s.CanInteract());
        bool fallback = false;
        if (shrine == null && MapController.currentMap.shrines != null)
        {
            var prefab = MapController.currentMap.shrines.FirstOrDefault(p => p != null && p.GetComponent<InteractableShrineChallenge>() != null);
            if (prefab != null) { shrine = Instantiate(prefab, player.transform.position + Vector3.forward * 3f, Quaternion.identity).GetComponent<InteractableShrineChallenge>(); fallback = true; }
        }
        var body = player.GetComponent<Rigidbody>();
        if (shrine == null || body == null) { Debug.LogError("Challenge combat probe: shrine or player body missing"); Restore(); yield break; }
        Vector3 oldPosition = body.position, oldVelocity = body.velocity;
        body.position = shrine.transform.position + Vector3.back * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        bool externalInput;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            externalInput = intent.Call<bool>("getBooleanExtra", "megabonk_challenge_combat_external", false);
        bool started;
        if (externalInput)
        {
            var oldConstraints = body.constraints;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.position = shrine.transform.position + Vector3.back * 2f;
            body.velocity = Vector3.zero;
            Physics.SyncTransforms();
            var detector = player.playerInput != null ? player.playerInput.detectInteractables : null;
            var current = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
            yield return new WaitForFixedUpdate();
            bool selected = detector != null && current.GetValue(detector) == (object)shrine;
            Debug.Log($"Challenge combat probe: READY external selected={selected}, prefabFallback={fallback}");
            deadline = Time.realtimeSinceStartup + 45f;
            while (selected && shrine.CanInteract() && Time.realtimeSinceStartup < deadline)
            {
                for (int button = 0; button < 20; button++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + button)))
                        Debug.Log($"Challenge combat probe: button={button}, selectedNow={current.GetValue(detector) == (object)shrine}, distance={Vector3.Distance(body.position, shrine.transform.position):F2}");
                yield return null;
            }
            started = selected && !shrine.CanInteract();
            body.constraints = oldConstraints;
        }
        else started = shrine.Interact();
		if (!started) { Debug.LogError($"Challenge combat probe: interaction failed externalInput={externalInput}"); Restore(); yield break; }
		var spawned = (HashSet<Enemy>)typeof(InteractableShrineChallenge).GetField("enemies", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shrine);
		var tracked = spawned != null ? new HashSet<Enemy>(spawned) : new HashSet<Enemy>();
        int initial = tracked.Count, hits = 0, deaths = 0, releases = 0, completions = 0;
        int Chests() => FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => EffectManager.Instance.openChestNormal != null && t.name == EffectManager.Instance.openChestNormal.name + "(Clone)");
        int beforeChests = Chests();
        Enemy.A_Damage += OnDamage; Enemy.A_EnemyDied += OnDeath; Enemy.A_EnemyReleasedFromPool += OnRelease; InteractableShrineChallenge.A_Completed += OnComplete;
        Debug.Log($"Challenge combat probe: started={started}, externalInput={externalInput}, prefabFallback={fallback}, tracked={initial}, activeWeapons={player.inventory.weaponInventory.weapons.Values.Count(w => w.enabled)}");
        deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline && completions == 0) yield return new WaitForSecondsRealtime(.25f);
        Debug.Log($"Challenge combat probe: finished hits={hits}, deaths={deaths}, releases={releases}, completions={completions}, chestsBefore={beforeChests}, chestsAfter={Chests()}");
        if (completions == 1 && deaths == initial && initial > 0)
        {
            var chest = FindObjectsByType<OpenChest>(FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.activeInHierarchy && c.name == EffectManager.Instance.openChestNormal.name + "(Clone)");
            var capsule = body.GetComponent<CapsuleCollider>();
            if (chest == null || capsule == null) Debug.LogError("Challenge combat reward INCONCLUSIVE: missing chest or capsule");
            else
            {
                Vector3 target = chest.transform.position;
                Vector3 origin = target + Vector3.back * 4f + Vector3.up * 20f;
                if (!Physics.Raycast(origin, Vector3.down, out var ground, 40f, GameManager.Instance.whatIsGround)) Debug.LogError("Challenge combat reward INCONCLUSIVE: no approach ground");
                else
                {
                    var settings = SaveManager.Instance.config.cfGameSettings;
                    int oldSkip = settings.skip_chest_animation; settings.skip_chest_animation = 1;
                    int opens = 0, items = 0; EItem awarded = default;
                    Action onOpen = () => opens++;
                    Action<EItem> onItem = item => { items++; awarded = item; };
                    OpenChest.A_Open += onOpen; ItemInventory.A_ItemAdded += onItem;
                    body.position = ground.point + Vector3.up * (capsule.height * .5f + .1f); body.velocity = Vector3.zero; Physics.SyncTransforms();
                    var input = player.playerInput;
                    var horizontal = typeof(PlayerInput).GetField("moveHorizontal", BindingFlags.Instance | BindingFlags.NonPublic);
                    var vertical = typeof(PlayerInput).GetField("moveVertical", BindingFlags.Instance | BindingFlags.NonPublic);
                    float startDistance = Vector3.Distance(body.position, target), nearest = startDistance;
                    deadline = Time.realtimeSinceStartup + 20f;
                    while (Time.realtimeSinceStartup < deadline && opens == 0 && input != null)
                    {
                        Vector3 direction = target - body.position; direction.y = 0f; nearest = Mathf.Min(nearest, direction.magnitude);
                        Vector3 local = Quaternion.Inverse(Quaternion.Euler(0f, input.cameraRotation.y, 0f)) * direction.normalized;
                        horizontal.SetValue(input, local.x); vertical.SetValue(input, local.z);
                        yield return new WaitForEndOfFrame();
                    }
                    if (input != null) { horizontal.SetValue(input, 0f); vertical.SetValue(input, 0f); }
                    bool pass = startDistance > 3f && nearest < 2f && opens == 1 && items == 1 && !chest.gameObject.activeSelf;
                    Debug.Log($"Challenge combat reward {(pass ? "PASS" : "INCONCLUSIVE")}: startDistance={startDistance:F2}, nearest={nearest:F2}, hits={hits}, deaths={deaths}, opens={opens}, items={items}, awarded={awarded}");
                    OpenChest.A_Open -= onOpen; ItemInventory.A_ItemAdded -= onItem; settings.skip_chest_animation = oldSkip;
                }
            }
        }
        Enemy.A_Damage -= OnDamage; Enemy.A_EnemyDied -= OnDeath; Enemy.A_EnemyReleasedFromPool -= OnRelease; InteractableShrineChallenge.A_Completed -= OnComplete;
        body.position = oldPosition; body.velocity = oldVelocity; Physics.SyncTransforms(); Restore();
        void OnDamage(Enemy enemy, DamageContainer dc) { if (!tracked.Contains(enemy)) return; hits++; Debug.Log($"Challenge combat probe: hit source={dc.damageSource}, damage={dc.damage:F2}, hp={enemy.hp:F2}"); }
        void OnDeath(Enemy enemy, DamageContainer dc) { if (tracked.Contains(enemy)) deaths++; }
        void OnRelease(Enemy enemy) { if (tracked.Contains(enemy)) releases++; }
        void OnComplete() { completions++; }
        void Restore() { if (player != null) { invincible.SetValue(player, oldInvincible); until.SetValue(player, oldUntil); } if (manager != null) manager.enabledWaves = waves; }
    }
}
#endif
