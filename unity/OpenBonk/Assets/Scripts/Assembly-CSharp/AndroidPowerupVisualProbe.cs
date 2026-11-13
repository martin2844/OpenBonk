#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in only: place several real pooled pickups for a visual capture.
internal sealed class AndroidPowerupVisualProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_powerup_visual_probe", false)) return;
        var root = new GameObject("Powerup visual probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidPowerupVisualProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
               (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MyPlayer.Instance == null ||
                PickupManager.Instance == null || PoolManager.Instance == null || PlayerCamera.Instance == null ||
                PlayerCamera.Instance.camera == null || GameManager.Instance.cutscene)) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("Powerup visual probe: game or camera unavailable"); yield break; }
        var camera = PlayerCamera.Instance.camera;
        var manager = PickupManager.Instance;
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f || right.sqrMagnitude < .01f) { Debug.LogError("Powerup visual probe: invalid camera orientation"); yield break; }
        var created = new List<Pickup>();
        var types = new[] { EPickup.Health, EPickup.Nuke, EPickup.Time, EPickup.Shield,
                            EPickup.Rage, EPickup.Haste, EPickup.Stonks, EPickup.Magnet };
        for (int i = 0; i < types.Length; i++)
        {
            Vector3 at = MyPlayer.Instance.transform.position + forward * 7f + Vector3.up * 3f;
            Pickup pickup = manager.SpawnPickup(types[i], at, 1, false, 120f);
            if (pickup == null) { Debug.LogError($"Powerup visual probe: could not spawn {types[i]}"); continue; }
            created.Add(pickup);
            pickup.enabled = false;
            if (pickup.collider != null) pickup.collider.enabled = false;
            var generic = pickup.GetComponent<GenericPowerupPrefab>();
            var renderer = generic != null ? generic.iconRenderer : null;
            if (renderer == null || renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null) { Debug.LogError($"Powerup visual probe: {types[i]} has no icon material or shader"); continue; }
            // Arrange icons in camera space, away from the player and each other.
            Vector3 current = camera.WorldToViewportPoint(renderer.bounds.center);
            Vector3 target = new Vector3(.32f + (i % 4) * .12f, .68f - (i / 4) * .13f, current.z);
            pickup.transform.position += camera.ViewportToWorldPoint(target) - renderer.bounds.center;
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            Texture icon = block.GetTexture("_MainTex");
            Vector3 viewport = camera.WorldToViewportPoint(renderer.bounds.center);
            Debug.Log($"Powerup visual probe: {types[i]} shader={renderer.sharedMaterial.shader.name} icon={(icon != null ? icon.name : "<missing>")} viewport={viewport} active={renderer.enabled && renderer.gameObject.activeInHierarchy}");
        }
        Debug.Log("Powerup visual probe: capture the screen now; material state does not prove rendered transparency");
        yield return new WaitForSecondsRealtime(30f);
        foreach (Pickup pickup in created)
        {
            if (manager == null || PickupManager.Instance != manager) break;
            if (pickup == null || !pickup.gameObject.activeInHierarchy) continue;
            pickup.enabled = true;
            manager.DespawnPickup(pickup);
        }
    }
}
#endif
