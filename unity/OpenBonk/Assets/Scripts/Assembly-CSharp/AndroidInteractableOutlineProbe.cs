#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using UnityEngine;

// Controlled positioning, but normal detector selection and component lifecycle.
internal sealed class AndroidInteractableOutlineProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_interactable_outline_probe", false)) return;
        var go = new GameObject("Interactable outline probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidInteractableOutlineProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f; InteractableShrineMoai target = null;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || (target = FindObjectOfType<InteractableShrineMoai>()) == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || manager == null || target == null || player.playerInput == null || player.playerInput.detectInteractables == null) { Debug.LogError("Interactable outline probe: scene unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var body = player.playerMovement.GetRb(); var position = body.position; var velocity = body.velocity;
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic);
        bool oldInvincible = (bool)invincible.GetValue(player); invincible.SetValue(player, true);
        var detector = player.playerInput.detectInteractables;
        var selectedField = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
        body.position = target.transform.position + Vector3.back * 2f + Vector3.up; body.velocity = Vector3.zero; Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && selectedField.GetValue(detector) != (object)target) yield return null;
        yield return null;
        var outline = target.GetComponent<Outline>(); var renderer = target.GetComponent<Renderer>();
        bool selected = selectedField.GetValue(detector) == (object)target;
        bool live = outline != null && outline.enabled && renderer != null && renderer.sharedMaterials.Length >= 3;
        Debug.Log($"Interactable outline probe: target={target.name}, selected={selected}, outline={live}, mode={(outline != null ? outline.OutlineMode.ToString() : "none")}, width={(outline != null ? outline.OutlineWidth : 0)}, materials={(renderer != null ? renderer.sharedMaterials.Length : 0)}");
        body.position = target.transform.position + Vector3.back * 30f + Vector3.up; body.velocity = Vector3.zero; Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && selectedField.GetValue(detector) == (object)target) yield return null;
        yield return null; yield return null;
        bool removed = target.GetComponent<Outline>() == null;
        Debug.Log($"Interactable outline probe {(selected && live && removed ? "PASS" : "FAIL")}: selected={selected}, live={live}, removedAfterLeaving={removed}, selectedAfter={selectedField.GetValue(detector) == (object)target}");
        body.position = position; body.velocity = velocity; invincible.SetValue(player, oldInvincible); manager.enabledWaves = waves;
    }
}
#endif
