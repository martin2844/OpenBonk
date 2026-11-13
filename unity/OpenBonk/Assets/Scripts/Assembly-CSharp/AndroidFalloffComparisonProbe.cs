#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Diagnostic only: compare two pinned texture payloads in one frozen frame.
internal sealed class AndroidFalloffComparisonProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_falloff_probe", false)) return;
        var root = new GameObject("falloff comparison probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidFalloffComparisonProbe>();
    }

    static byte[] Load(string name, string expected)
    {
        var path = Path.Combine(Application.persistentDataPath, name);
        var data = File.ReadAllBytes(path);
        using var hash = SHA256.Create();
        var actual = BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        if (data.Length != 3088 || actual != expected) throw new InvalidDataException("unexpected " + name);
        return data;
    }

    static Texture2D Texture(byte[] data)
    {
        var texture = new Texture2D(256, 8, TextureFormat.ETC2_RGBA8, true, true);
        texture.LoadRawTextureData(data); texture.Apply(false, false);
        texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; texture.anisoLevel = 1;
        return texture;
    }

    static void Capture(Camera camera, string path)
    {
        var rt = new RenderTexture(640, 320, 24, RenderTextureFormat.ARGB32);
        var prevTarget = camera.targetTexture; var prevActive = RenderTexture.active;
        var image = new Texture2D(640, 320, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 640, 320), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { camera.targetTexture = prevTarget; RenderTexture.active = prevActive; Destroy(image); rt.Release(); Destroy(rt); }
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || Camera.main == null)) yield return null;
        yield return new WaitForSecondsRealtime(5f);
        var player = MyPlayer.Instance;
        if (player == null || Camera.main == null) { Debug.LogError("Falloff comparison INCONCLUSIVE: player or camera unavailable"); yield break; }
        var projector = player.GetComponentsInChildren<Projector>(true).FirstOrDefault(p => p.name == "BlobShadowProjector");
        if (projector == null || !projector.enabled || projector.material == null || !projector.material.HasProperty("_FalloffTex")) { Debug.LogError("Falloff comparison INCONCLUSIVE: active player projector unavailable"); yield break; }
        Texture2D original = null, converted = null; Material replacement = null;
        var camera = Camera.main; var previousMaterial = projector.material; var position = camera.transform.position; var rotation = camera.transform.rotation;
        bool wasEnabled = projector.enabled, wasOrthographic = camera.orthographic; float oldSize = camera.orthographicSize, oldTimeScale = Time.timeScale;
        var post = camera.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>(); bool postEnabled = post != null && post.enabled;
        var renderers = player.GetComponentsInChildren<Renderer>(true); var rendererStates = renderers.Select(r => r.enabled).ToArray();
        try
        {
            original = Texture(Load("falloff-old.bin", "efde789ec1d454a6f31e9cb475b61ce5bdea72fa8d8644b07b76ab704beecfb5"));
            converted = Texture(Load("falloff-converted.bin", "7072b2fa0a74081fcd9aff6ed28bcc0bd3e0cce4e3de88000a13054faa2f0f0e"));
            replacement = new Material(previousMaterial); projector.material = replacement;
            var pivot = player.transform.position;
            Time.timeScale = 0f; if (post != null) post.enabled = false;
            foreach (var renderer in renderers) renderer.enabled = false;
            camera.orthographic = true; camera.transform.position = pivot + Vector3.up * 20f; camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (var factor in new[] { 1f, 2f, 4f })
            {
                camera.orthographicSize = 3f * factor;
                string stem = Path.Combine(Application.persistentDataPath, "falloff-" + (int)factor + "x-");
                projector.enabled = true; replacement.SetTexture("_FalloffTex", original); Capture(camera, stem + "old.png");
                replacement.SetTexture("_FalloffTex", converted); Capture(camera, stem + "converted.png");
                projector.enabled = false; Capture(camera, stem + "disabled.png");
            }
            Debug.Log("Falloff comparison CAPTURED: overhead orthographic sizes 3, 6, 12; compare projector-disabled control");
        }
        catch (Exception e) { Debug.LogError("Falloff comparison FAIL: " + e); }
        finally
        {
            camera.transform.position = position; camera.transform.rotation = rotation; camera.orthographic = wasOrthographic; camera.orthographicSize = oldSize;
            projector.enabled = wasEnabled; projector.material = previousMaterial; if (post != null) post.enabled = postEnabled; Time.timeScale = oldTimeScale;
            for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = rendererStates[i];
            if (replacement != null) Destroy(replacement); if (original != null) Destroy(original); if (converted != null) Destroy(converted);
        }
    }
}
#endif
