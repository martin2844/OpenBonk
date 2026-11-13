#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Intent-gated diagnostics for a disposable Android run.
internal sealed class AndroidBananaQuestProbe : MonoBehaviour
{
    bool readOnly;
    int bananaHits;
    float bananaDamage;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool quest = intent.Call<bool>("getBooleanExtra", "megabonk_banana_quest_probe", false);
        bool read = intent.Call<bool>("getBooleanExtra", "megabonk_banana_save_probe", false);
        if (!quest && !read) return;
        var go = new GameObject("banana quest probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidBananaQuestProbe>().readOnly = read;
    }
    IEnumerator Start()
    {
        if (readOnly) {
            float loadDeadline = Time.realtimeSinceStartup + 120f;
            while (Time.realtimeSinceStartup < loadDeadline && (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null)) yield return null;
            bool loaded = SaveManager.loaded && SaveManager.Instance != null && SaveManager.Instance.progression != null;
            bool achievement = loaded && MyAchievements.IsAchievementDone("a_bananarang");
            Debug.Log($"Banana save probe {(achievement ? "PASS" : "FAIL")}: loaded={loaded} achievement={achievement}");
            yield break;
        }
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableBananarang banana = null;
        while (Time.realtimeSinceStartup < deadline) {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted && EnemyManager.Instance != null)
                banana = FindObjectOfType<InteractableBananarang>();
            if (banana != null) break;
            yield return null;
        }
        var player = MyPlayer.Instance;
        var detector = FindObjectOfType<DetectInteractables>();
        if (banana == null || player == null || detector == null || MyAchievements.IsAchievementDone("a_bananarang")) {
            Debug.LogError($"Banana quest probe INCONCLUSIVE: banana={banana}, detector={detector}");
            yield break;
        }
        var inventory = player.inventory.weaponInventory;
        bool initiallyAbsent = inventory.GetWeaponLevel(EWeapon.Bananarang) == 0;
        Vector3 position = banana.transform.position;
        // Record actual map surfaces before moving the player or invoking the quest.
        foreach (var offset in new[] { Vector3.zero, Vector3.right * 3f, Vector3.left * 3f, Vector3.forward * 3f, Vector3.back * 3f })
        {
            var hits = Physics.RaycastAll(position + offset + Vector3.up * 100f, Vector3.down, 200f, GameManager.Instance.whatIsGroundAndObjects, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            int count = 0;
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(banana.transform)) continue;
                Debug.Log($"Banana terrain probe: offset={offset} surfaceY={hit.point.y:F2} deltaY={position.y-hit.point.y:F2} normalY={hit.normal.y:F2} object={hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                if (++count == 3) break;
            }
            if (count == 0) Debug.Log($"Banana terrain probe: offset={offset} no ground/object surface within ray");
        }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in inventory.weapons.Values) weapon.Disable();
        var body = player.playerMovement.GetRb();
        body.position = position + Vector3.up * 2f;
        body.velocity = Vector3.zero;
        Physics.SyncTransforms();
        var field = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
        // Let the real proximity detector select the quest object before the
        // existing direct-selection fallback used by the quest diagnostic.
        float detectDeadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < detectDeadline && field.GetValue(detector) != banana) yield return null;
        bool detected = field.GetValue(detector) == banana;
        Debug.Log($"Banana proximity probe {(detected ? "PASS" : "INCONCLUSIVE")}: teleportedNear=true actualDetectorSelected={detected}");
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool inputProbe = intent.Call<bool>("getBooleanExtra", "megabonk_banana_input_probe", false);
        var fx = banana.fx;
        bool duplicateRejected = false;
        if (inputProbe)
        {
            if (!detected) { Debug.LogError("Banana input probe INCONCLUSIVE: detector did not select banana"); yield break; }
            Debug.Log("Banana input probe READY: waiting for interact button");
            deadline = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < deadline && !MyAchievements.IsAchievementDone("a_bananarang")) yield return null;
        }
        else
        {
            field.SetValue(detector, banana);
            detector.TryInteract();
            duplicateRejected = !banana.Interact();
        }
        bool unlocked = MyAchievements.IsAchievementDone("a_bananarang");
        int level = inventory.GetWeaponLevel(EWeapon.Bananarang);
        bool effect = fx != null && fx.activeSelf && fx.transform.parent == null;
        bool ok = initiallyAbsent && unlocked && level == 1 && effect && (inputProbe || duplicateRejected);
        Debug.Log($"Banana quest probe {(ok ? "PASS" : "FAIL")}: position={position} initiallyAbsent={initiallyAbsent} achievement={unlocked} level={level} effect={effect} duplicateRejected={duplicateRejected}");
        if (ok && intent.Call<bool>("getBooleanExtra", "megabonk_banana_combat_probe", false))
        {
            typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
            typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
            Enemy.A_Damage += OnEnemyDamage;
            EnemyManager.Instance.enabledWaves = true;
            deadline = Time.realtimeSinceStartup + 70f;
            while (Time.realtimeSinceStartup < deadline && bananaHits == 0) yield return null;
            Debug.Log($"Banana quest combat probe {(bananaHits > 0 && bananaDamage > 0 ? "PASS" : "INCONCLUSIVE")}: questGranted=true ordinaryWaves=true hits={bananaHits} damage={bananaDamage:F2}");
            Enemy.A_Damage -= OnEnemyDamage;
        }
    }
    void OnEnemyDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy == null || damage == null || damage.damageSource != EWeapon.Bananarang.ToString()) return;
        bananaHits++; bananaDamage += damage.damage;
        Debug.Log($"Banana quest combat probe HIT: enemy={enemy.GetInstanceID()} damage={damage.damage:F2}");
    }
    void OnDestroy() { Enemy.A_Damage -= OnEnemyDamage; }
}
#endif
