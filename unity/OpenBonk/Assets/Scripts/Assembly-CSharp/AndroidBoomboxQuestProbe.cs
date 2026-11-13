#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Opt-in diagnostic for a disposable Android save and run.
internal sealed class AndroidBoomboxQuestProbe : MonoBehaviour
{
    bool verifyCompleted;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool quest = intent.Call<bool>("getBooleanExtra", "megabonk_boombox_quest_probe", false);
        bool complete = intent.Call<bool>("getBooleanExtra", "megabonk_boombox_completed_probe", false);
        if (!quest && !complete) return;
        var root = new GameObject("boombox quest probe"); DontDestroyOnLoad(root);
        root.AddComponent<AndroidBoomboxQuestProbe>().verifyCompleted = complete;
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EnemyManager.Instance == null || !SaveManager.loaded)) yield return null;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || !SaveManager.loaded) {
            Debug.LogError("Boombox quest probe INCONCLUSIVE: run unavailable"); yield break;
        }
        var boxes = FindObjectsOfType<InteractableBoombox>();
        bool unlocked = MyAchievements.IsAchievementDone("a_boombox");
        if (verifyCompleted) {
            bool ok = unlocked && boxes.Length == 0;
            Debug.Log($"Boombox completed probe {(ok ? "PASS" : "FAIL")}: achievement={unlocked} generated={boxes.Length}");
            yield break;
        }
        int target = MyAchievements.GetAchievementTargetValue("a_boombox");
        var detector = FindObjectOfType<DetectInteractables>();
        if (unlocked || target <= 0 || boxes.Length != target || detector == null) {
            Debug.LogError($"Boombox quest probe INCONCLUSIVE: achievement={unlocked} target={target} generated={boxes.Length} detector={detector}"); yield break;
        }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in MyPlayer.Instance.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var field = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.NonPublic | BindingFlags.Instance);
        var body = MyPlayer.Instance.playerMovement.GetRb();
        bool allActivated = true, rejected = true, threshold = true;
        for (int i = 0; i < boxes.Length; i++) {
            var box = boxes[i];
            body.position = box.transform.position + Vector3.up * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
            field.SetValue(detector, box); detector.TryInteract();
            allActivated &= !box.CanInteract() && box.fx.activeSelf && box.pauseZone.enabled && box.music.isPlaying;
            field.SetValue(detector, box); detector.TryInteract();
            rejected &= !box.Interact();
            bool achieved = MyAchievements.IsAchievementDone("a_boombox");
            threshold &= achieved == (i == boxes.Length - 1);
        }
        bool pass = allActivated && rejected && threshold && MyAchievements.IsAchievementDone("a_boombox");
        Debug.Log($"Boombox quest probe {(pass ? "PASS" : "FAIL")}: generated={boxes.Length} target={target} activated={allActivated} duplicatesRejected={rejected} threshold={threshold} achievement={MyAchievements.IsAchievementDone("a_boombox")}");
    }
}
#endif
