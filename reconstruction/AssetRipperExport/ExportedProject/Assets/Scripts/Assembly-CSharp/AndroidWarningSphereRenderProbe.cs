#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Optional test of the actual pooled warning mesh and material on Android.
internal sealed class AndroidWarningSphereRenderProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_warning_sphere_render_probe", false)) return;
        var root = new GameObject("warning sphere render probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidWarningSphereRenderProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EffectManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.warningSpherePool == null)) yield return null;
        if (EffectManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.warningSpherePool == null) { Debug.LogError("Warning sphere render INCONCLUSIVE: pool unavailable"); yield break; }
        Vector3 origin = new Vector3(0, 1000, 0);
        CircleWarning warning = EffectManager.Instance.WarningSphere(origin, 3f, 10f, null);
        var renderers = warning.GetComponentsInChildren<Renderer>(true);
        var transforms = warning.GetComponentsInChildren<Transform>(true);
        var layers = new int[transforms.Length];
        for (int i = 0; i < transforms.Length; i++) { layers[i] = transforms[i].gameObject.layer; transforms[i].gameObject.layer = 30; }
        var cameraObject = new GameObject("warning render camera");
        var cam = cameraObject.AddComponent<Camera>(); cam.enabled = false; cam.orthographic = true; cam.orthographicSize = 4f;
        cam.transform.position = origin + new Vector3(0, 8, -8); cam.transform.LookAt(origin);
        cam.cullingMask = 1 << 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.allowHDR = false; cam.allowMSAA = false;
        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        cam.targetTexture = rt;
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && warning.filler.localScale.x < 3f) yield return null;
        warning.enabled = false;
        cam.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
        Color32[] withWarning = image.GetPixels32();
        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/warning-sphere-controlled.png", image.EncodeToPNG());
        bool[] wasEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) { wasEnabled[i] = renderers[i].enabled; renderers[i].enabled = false; }
        cam.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
        Color32[] withoutWarning = image.GetPixels32(); int changed = 0;
        for (int i = 0; i < withWarning.Length; i++) if (withWarning[i].r != withoutWarning[i].r || withWarning[i].g != withoutWarning[i].g || withWarning[i].b != withoutWarning[i].b) changed++;
        Debug.Log($"Warning sphere render {(changed > 10 ? "PASS" : "INCONCLUSIVE")}: changedPixels={changed}, scale={warning.filler.localScale.x:F2}, renderers={renderers.Length}, material={(renderers.Length > 0 && renderers[0].sharedMaterial != null ? renderers[0].sharedMaterial.name : "none")}, instancing={(renderers.Length > 0 && renderers[0].sharedMaterial != null && renderers[0].sharedMaterial.enableInstancing)}, gpu={SystemInfo.graphicsDeviceType}");
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = wasEnabled[i];
        for (int i = 0; i < transforms.Length; i++) transforms[i].gameObject.layer = layers[i];
        // Render through the real gameplay camera over its normal scene, without
        // advancing simulation between the enabled/disabled images.
        var gameplayCamera = Camera.main;
        if (gameplayCamera != null)
        {
            warning.transform.position = MyPlayer.Instance.transform.position;
            var oldTarget = gameplayCamera.targetTexture;
            gameplayCamera.targetTexture = rt;
            gameplayCamera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
            withWarning = image.GetPixels32();
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/warning-sphere-gameplay.png", image.EncodeToPNG());
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
            gameplayCamera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
            withoutWarning = image.GetPixels32();
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/warning-sphere-gameplay-without.png", image.EncodeToPNG());
            changed = 0;
            for (int i = 0; i < withWarning.Length; i++) if (withWarning[i].r != withoutWarning[i].r || withWarning[i].g != withoutWarning[i].g || withWarning[i].b != withoutWarning[i].b) changed++;
            Debug.Log($"Warning sphere gameplay render: changedPixels={changed}, camera={gameplayCamera.name}, playerPosition={MyPlayer.Instance.transform.position}");
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = wasEnabled[i];
            gameplayCamera.targetTexture = oldTarget;
        }
        else Debug.Log("Warning sphere gameplay render INCONCLUSIVE: no main camera");
        warning.enabled = true; warning.ReleaseToPool(); RenderTexture.active = null; cam.targetTexture = null;
        rt.Release(); Destroy(rt); Destroy(image); Destroy(cameraObject); Destroy(gameObject);
    }
}
#endif
