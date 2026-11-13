#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in: inspect the real pooled rocket prefab after the shared material change.
internal sealed class AndroidRocketMaterialProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_rocket_material_probe", false)) return;
        var root = new GameObject("Rocket material probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidRocketMaterialProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
               (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || PoolManager.Instance == null ||
                PoolManager.Instance.rocketPool == null || PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null))
            yield return null;
        if (PoolManager.Instance == null || PoolManager.Instance.rocketPool == null || PlayerCamera.Instance == null || PlayerCamera.Instance.camera == null)
        { Debug.LogError("Rocket material probe: required scene objects unavailable"); yield break; }
        var pool = PoolManager.Instance.rocketPool;
        var camera = PlayerCamera.Instance.camera;
        var rockets = new List<GameObject>();
        for (int i = 0; i < 3; i++)
        {
            var rocket = pool.Get(); rockets.Add(rocket);
            var behavior = rocket.GetComponent<Rocket>();
            if (behavior != null) behavior.enabled = false;
            foreach (var collider in rocket.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            rocket.transform.SetPositionAndRotation(camera.transform.TransformPoint(new Vector3((i - 1) * 1.5f, 1f, 7f)), camera.transform.rotation);
            rocket.transform.localScale = Vector3.one * 2f;
        }
        yield return new WaitForSecondsRealtime(2f);
        foreach (var rocket in rockets)
            foreach (var renderer in rocket.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material != null && material.shader != null && material.shader.name == "Standard")
                        Debug.Log($"Rocket material probe: renderer={renderer.name}, visible={renderer.isVisible}, material={material.name}, instancing={material.enableInstancing}");
        Debug.Log("Rocket material probe: capture window open");
        yield return new WaitForSecondsRealtime(12f);
        foreach (var rocket in rockets)
        {
            if (rocket == null) continue;
            var behavior = rocket.GetComponent<Rocket>(); if (behavior != null) behavior.enabled = true;
            foreach (var collider in rocket.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
            pool.Release(rocket);
        }
        Debug.Log("Rocket material probe: released pooled rockets");
    }
}
#endif
