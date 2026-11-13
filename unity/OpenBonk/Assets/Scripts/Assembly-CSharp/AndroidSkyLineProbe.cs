#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in diagnostic: isolate the visible source of lines above the horizon.
internal sealed class AndroidSkyLineProbe : MonoBehaviour
{
    Camera observedCamera;
    Quaternion observedRotation;
    bool lockCamera;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_sky_line_probe", false)) return;
        var root = new GameObject("Sky line probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidSkyLineProbe>();
    }
    void LateUpdate() { if (lockCamera && observedCamera != null) observedCamera.transform.rotation = observedRotation; }
    Color32[] CaptureScene()
    {
        var target = RenderTexture.GetTemporary(960, 540, 24, RenderTextureFormat.ARGB32);
        var previousTarget = observedCamera.targetTexture;
        var previousActive = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            observedCamera.targetTexture = target;
            observedCamera.Render();
            RenderTexture.active = target;
            pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            return pixels.GetPixels32();
        }
        finally
        {
            observedCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (pixels != null) Destroy(pixels);
            RenderTexture.ReleaseTemporary(target);
        }
    }
    void CompareEffect(string label, Color32[] baseline, Color32[] current)
    {
        int count = 0, minX = 960, minY = 540, maxX = -1, maxY = -1;
        // Restrict to the upper half, where the streaks were observed.
        for (int y = 270; y < 540; y++) for (int x = 0; x < 960; x++)
        {
            int i = y * 960 + x;
            int delta = Mathf.Abs(baseline[i].r - current[i].r) + Mathf.Abs(baseline[i].g - current[i].g) + Mathf.Abs(baseline[i].b - current[i].b);
            if (delta <= 18) continue;
            count++; minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
        }
        Debug.Log($"Sky line isolated effect: {label}, changed={count}, bounds={minX},{minY}..{maxX},{maxY}");
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || Camera.main == null)) yield return null;
        if (MyPlayer.Instance == null || Camera.main == null) { Debug.LogError("Sky line probe: player or camera unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        observedCamera = Camera.main;
        observedRotation = Quaternion.Euler(-12f, observedCamera.transform.eulerAngles.y, 0f);
        lockCamera = true;
        float oldTime = Time.timeScale; Time.timeScale = 0f;
        var terrain = new List<Renderer>(); var widths = new Dictionary<Material,float>();
        foreach (var r in FindObjectsOfType<Renderer>()) foreach (var m in r.sharedMaterials)
            if (m != null && m.shader != null && m.shader.name.StartsWith("Toon/Lit Tri Planar")) {
                if (r.enabled && !terrain.Contains(r)) terrain.Add(r);
                if (m.HasProperty("_OutlineWidth") && !widths.ContainsKey(m)) widths.Add(m,m.GetFloat("_OutlineWidth"));
            }
        Debug.Log($"Sky line probe: terrain renderers={terrain.Count}, materials={widths.Count}");
        yield return new WaitForSecondsRealtime(3f);
        // Compare each edge renderer's outline with itself, holding camera and
        // scene fixed. Restore every renderer and material before normal phases.
        foreach (var selected in terrain)
        {
            if (selected == null) continue;
            foreach (var renderer in terrain) if (renderer != null) renderer.enabled = renderer == selected;
            foreach (var kv in widths) if (kv.Key != null) kv.Key.SetFloat("_OutlineWidth", kv.Value);
            var outlined = CaptureScene();
            foreach (var kv in widths) if (kv.Key != null) kv.Key.SetFloat("_OutlineWidth", 0f);
            CompareEffect("outline-only/" + selected.name, CaptureScene(), outlined);
        }
        foreach (var renderer in terrain) if (renderer != null) renderer.enabled = true;
        foreach (var kv in widths) if (kv.Key != null) kv.Key.SetFloat("_OutlineWidth", kv.Value);
        Debug.Log("Sky line probe: CAPTURE baseline"); yield return new WaitForSecondsRealtime(12f);
        foreach (var kv in widths) kv.Key.SetFloat("_OutlineWidth",0f);
        Debug.Log("Sky line probe: CAPTURE no-outlines"); yield return new WaitForSecondsRealtime(12f);
        // The earlier no-outline capture still contained thin colored streaks.
        // Disable only currently enabled particle, trail and line renderers so
        // the same frozen scene can identify whether they originate in VFX.
        var effects = new List<Renderer>();
        foreach (var r in FindObjectsOfType<Renderer>())
            if (r.enabled && (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer))
            {
                effects.Add(r);
                Debug.Log($"Sky line probe effect: type={r.GetType().Name}, object={r.name}, material={(r.sharedMaterial != null ? r.sharedMaterial.name : "none")}");
                r.enabled = false;
            }
        Debug.Log($"Sky line probe: CAPTURE no-vfx, disabled={effects.Count}"); yield return new WaitForSecondsRealtime(12f);
        Color32[] baseline = CaptureScene();
        CompareEffect("baseline-repeat", baseline, CaptureScene());
        foreach (var effect in effects)
        {
            if (effect == null) continue;
            effect.enabled = true;
            CompareEffect(effect.name + "/" + (effect.sharedMaterial != null ? effect.sharedMaterial.name : "none"), baseline, CaptureScene());
            effect.enabled = false;
        }
        foreach (var r in effects) if (r != null) r.enabled = true;
        foreach (var kv in widths) if (kv.Key != null) kv.Key.SetFloat("_OutlineWidth",kv.Value);
        foreach (var r in terrain) if (r != null) r.enabled = false;
        Debug.Log("Sky line probe: CAPTURE no-terrain"); yield return new WaitForSecondsRealtime(12f);
        foreach (var r in terrain) if (r != null) r.enabled = true;
        var oldSky = RenderSettings.skybox; RenderSettings.skybox = null;
        Debug.Log("Sky line probe: CAPTURE no-skybox"); yield return new WaitForSecondsRealtime(12f);
        RenderSettings.skybox = oldSky; Time.timeScale = oldTime; lockCamera = false;
        Debug.Log("Sky line probe: restored");
    }
}
#endif
