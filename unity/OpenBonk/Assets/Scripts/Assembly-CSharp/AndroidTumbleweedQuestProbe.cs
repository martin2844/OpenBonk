#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

// Disposable run: forced quest odds, position, one tumbleweed and invulnerability.
internal sealed class AndroidTumbleweedQuestProbe : MonoBehaviour
{
    InteractableTumbleWeed watchedWeed;
    bool weedInteracted;
    bool shotgun;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool katana = intent.Call<bool>("getBooleanExtra", "megabonk_tumbleweed_quest_probe", false);
        bool shotgun = intent.Call<bool>("getBooleanExtra", "megabonk_tumbleweed_shotgun_probe", false);
        if (!katana && !shotgun) return;
        var go = new GameObject("tumbleweed quest probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidTumbleweedQuestProbe>().shotgun = shotgun;
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        int expectedStage = shotgun ? 1 : 0;
        string achievementName = shotgun ? "a_shotgun" : "a_katana";
        while (Time.realtimeSinceStartup < deadline && (MapController.currentMap == null || MapController.currentMap.eMap != EMap.Desert || MapController.index != expectedStage || TransitionUI.Instance == null || TransitionUI.Instance.isTransitioning || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EnemyManager.Instance == null || EffectManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var detector = FindObjectOfType<DetectInteractables>();
        if (player == null || detector == null || MapController.currentMap == null || MapController.currentMap.eMap != EMap.Desert || MapController.index != expectedStage || MyAchievements.IsAchievementDone(achievementName)) { Debug.LogError("Tumbleweed quest probe INCONCLUSIVE: initial state"); yield break; }
        var achievement = DataManager.Instance.GetAchievement(achievementName);
        if (achievement == null) { Debug.LogError("Tumbleweed quest probe INCONCLUSIVE: achievement missing"); yield break; }
        int oldTarget = achievement.targetValue;
        achievement.targetValue = 100;
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        player.inventory.playerXp.level = 9999;
        var body = player.playerMovement.GetRb();
        var before = new System.Collections.Generic.HashSet<InteractableTumbleWeed>(FindObjectsOfType<InteractableTumbleWeed>());
        EffectManager.Instance.SpawnTumbleWeeds(1);
        InteractableTumbleWeed weed = null;
        foreach (var candidate in FindObjectsOfType<InteractableTumbleWeed>()) if (!before.Contains(candidate)) { weed = candidate; break; }
        if (weed == null || weed.rb == null) { achievement.targetValue = oldTarget; Debug.LogError("Tumbleweed quest probe INCONCLUSIVE: spawned object missing"); yield break; }
        // Let the real spawn movement settle, then move the player to the
        // spawned object rather than overriding its queued MovePosition.
        yield return new WaitForSecondsRealtime(1f);
        weed.rb.velocity = Vector3.zero; weed.rb.constraints = RigidbodyConstraints.FreezeAll;
        body.position = weed.rb.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
        var selected = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && selected.GetValue(detector) != weed) yield return null;
        if (selected.GetValue(detector) != weed) {
            var collider = weed.GetComponent<Collider>();
            var overlaps = Physics.OverlapSphere(detector.transform.position, 5f, detector.whatIsInteractable);
            Debug.LogError($"Tumbleweed quest probe INCONCLUSIVE: detector did not select weed; active={weed.gameObject.activeInHierarchy}, collider={(collider != null && collider.enabled)}, trigger={(collider != null && collider.isTrigger)}, layer={weed.gameObject.layer}, mask={detector.whatIsInteractable.value}, tag={weed.gameObject.tag}, weedPosition={weed.transform.position}, playerPosition={body.position}, detectorPosition={detector.transform.position}, selected={selected.GetValue(detector)}, overlaps={overlaps.Length}");
            achievement.targetValue = oldTarget; yield break;
        }
        watchedWeed = weed;
        DetectInteractables.A_Interacted += OnInteraction;
        Debug.Log("Tumbleweed quest probe READY_WEED: use interact");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && (shotgun ? EffectManager.Instance.spawnedShotgunObject : EffectManager.Instance.spawnedKatanaObject) == null) yield return null;
        var quest = shotgun ? EffectManager.Instance.spawnedShotgunObject : EffectManager.Instance.spawnedKatanaObject;
        achievement.targetValue = oldTarget;
        if (quest == null) { Debug.LogError("Tumbleweed quest probe FAIL: weapon quest did not spawn"); yield break; }
        var target = quest.GetComponent<InteractableUnlock>();
        if (target == null) { Debug.LogError("Tumbleweed quest probe INCONCLUSIVE: unlock component missing"); yield break; }
        body.position = quest.transform.position + Vector3.up * 1.5f; body.velocity = Vector3.zero; Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && selected.GetValue(detector) != target) yield return null;
        if (selected.GetValue(detector) != target) { Debug.LogError("Tumbleweed quest probe INCONCLUSIVE: detector did not select weapon"); yield break; }
        Debug.Log($"Tumbleweed quest probe {(shotgun ? "READY_SHOTGUN" : "READY_KATANA")}: use interact");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && !MyAchievements.IsAchievementDone(achievementName)) yield return null;
        bool unlocked = MyAchievements.IsAchievementDone(achievementName);
        int level = player.inventory.weaponInventory.GetWeaponLevel(shotgun ? EWeapon.Shotgun : EWeapon.Katana);
        Debug.Log($"Tumbleweed quest probe {(weedInteracted && unlocked && level == 1 ? "PASS" : "FAIL")}: stage={expectedStage}, weapon={(shotgun ? "Shotgun" : "Katana")}, weedInteracted={weedInteracted}, questSpawned=true, achievement={unlocked}, weaponLevel={level}");
    }
    void OnInteraction(BaseInteractable interactable, bool success) { if (interactable == watchedWeed && success) weedInteracted = true; }
    void OnDestroy() { DetectInteractables.A_Interacted -= OnInteraction; }
}
#endif
