#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in diagnostic for disposable Android emulator runs.
internal sealed class AndroidGravestoneProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_gravestone_probe", false)) return;
        var go = new GameObject("Gravestone probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidGravestoneProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableGravestone grave = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.isCrypt &&
                MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.playerInput != null && EnemyManager.Instance != null)
            {
                grave = FindObjectsByType<InteractableGravestone>(FindObjectsSortMode.None).FirstOrDefault(g => g.CanInteract());
                if (grave != null) break;
            }
            yield return null;
        }
        if (grave == null) { Debug.LogError("Gravestone probe FAIL: generated gravestone unavailable"); yield break; }
        var player = MyPlayer.Instance; var detector = player.playerInput.detectInteractables;
        var body = player.playerMovement != null ? player.playerMovement.GetRb() : null;
        if (detector == null || body == null) { Debug.LogError("Gravestone probe FAIL: player detector or body unavailable"); yield break; }
        int countBefore = player.inventory.statInventory.permanentChanges.Values.Sum(x => x.Count);
        Enemy spawned = null; Action<Enemy> onSpawn = enemy => spawned = enemy; Enemy.A_EnemySpawned += onSpawn;
        var oldPosition = body.position; var oldVelocity = body.velocity;
        body.position = grave.transform.position + Vector3.back * 2f; body.velocity = Vector3.zero; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        var selected = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(detector) as BaseInteractable;
        if (selected == grave)
        {
            typeof(PlayerInput).GetField("interacting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player.playerInput, true);
            yield return new WaitForFixedUpdate(); yield return null;
        }
        int countAfter = player.inventory.statInventory.permanentChanges.Values.Sum(x => x.Count);
        bool success = selected == grave && !grave.gameObject.activeSelf && countAfter == countBefore + 2 && spawned != null;
        Debug.Log($"Gravestone probe {(success ? "PASS" : "FAIL")}: generated=true selected={selected == grave} inactive={!grave.gameObject.activeSelf} addedStats={countAfter - countBefore} spawned={(spawned != null ? spawned.enemyData.enemyName.ToString() : "none")}");
        Enemy.A_EnemySpawned -= onSpawn;
        body.position = oldPosition; body.velocity = oldVelocity; Physics.SyncTransforms();
    }
}
#endif
