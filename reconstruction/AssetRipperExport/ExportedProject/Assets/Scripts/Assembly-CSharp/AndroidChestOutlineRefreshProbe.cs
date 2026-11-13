#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable emulator probe: real generated chest and detector, controlled money and position.
internal sealed class AndroidChestOutlineRefreshProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chest_outline_refresh_probe", false)) return;
        var go = new GameObject("Chest outline refresh probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidChestOutlineRefreshProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f; InteractableChest chest = null;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || (chest = FindObjectOfType<InteractableChest>()) == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || manager == null || chest == null || player.playerInput?.detectInteractables == null) { Debug.LogError("Chest outline refresh probe: scene unavailable"); yield break; }
        bool oldWaves = manager.enabledWaves; manager.enabledWaves = false;
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic); bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var gold = typeof(PlayerInventory).GetProperty("gold").GetSetMethod(true); float oldGold = player.inventory.gold; gold.Invoke(player.inventory, new object[] { 0f });
        var body = player.playerMovement.GetRb(); var oldPosition = body.position; var oldVelocity = body.velocity;
        var detector = player.playerInput.detectInteractables; var selected = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
        body.position = chest.transform.position + Vector3.back * 2f + Vector3.up; body.velocity = Vector3.zero; Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 8f; while (Time.realtimeSinceStartup < deadline && selected.GetValue(detector) != (object)chest) yield return null;
        yield return new WaitForFixedUpdate(); yield return null;
        var outline = chest.GetComponent<Outline>(); bool selectedChest = selected.GetValue(detector) == (object)chest;
        Color before = outline != null ? outline.OutlineColor : Color.clear;
        gold.Invoke(player.inventory, new object[] { 100000f });
        yield return new WaitForFixedUpdate(); yield return null; yield return null;
        Color after = outline != null ? outline.OutlineColor : Color.clear;
        bool pass = selectedChest && before == (Color)new Color32(189,34,34,255) && after == (Color)new Color32(255,215,69,255);
        Debug.Log($"Chest outline refresh probe {(pass ? "PASS" : "FAIL")}: selected={selectedChest}, before={before}, after={after}, currentSelection={selected.GetValue(detector) == (object)chest}");
        gold.Invoke(player.inventory, new object[] { oldGold }); body.position = oldPosition; body.velocity = oldVelocity; invincible.SetValue(player, oldInvincible); manager.enabledWaves = oldWaves;
    }
}
#endif
