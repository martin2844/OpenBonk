#if UNITY_ANDROID && !UNITY_EDITOR && (!OPENBONK_BETA_NO_PROBES || OPENBONK_UI_TARGET_QA)
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;
using UnityEngine.UI;

// Opt-in actual UI views; never installed in a normal launch or release build.
internal sealed class AndroidUiRenderTargetsProbe : MonoBehaviour
{
    private readonly Dictionary<string, int> renders = new Dictionary<string, int>();
    private bool passed = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_ui_targets_probe", false)) return;
        var root = new GameObject("UI render targets probe");
        DontDestroyOnLoad(root); root.AddComponent<AndroidUiRenderTargetsProbe>();
    }

    private IEnumerator Start()
    {
        Camera.onPostRender += OnCameraRendered;
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() ||
            GameManager.Instance.cutscene || MyPlayer.Instance == null || UiManager.Instance == null ||
            UiManager.Instance.encounterWindows == null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("UI targets probe INCONCLUSIVE: gameplay unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        yield return new WaitForEndOfFrame();
        Capture("MinimapTexture", "minimap");

        var pause = UiManager.Instance.pause;
        if (pause == null) { passed = false; Debug.LogError("UI targets probe FAIL: pause map unavailable"); }
        else
        {
            pause.Pause(); pause.GoToMap();
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            Capture("MapTexture", "map");
            ScreenCapture.CaptureScreenshot("ui-target-map.png");
            yield return new WaitForSecondsRealtime(.3f);
            pause.GoToMain(); pause.Resume();
        }

        var encounters = UiManager.Instance.encounterWindows;
        var chest = encounters.chestWindow as ChestWindowUi;
        if (chest == null || chest.chestOpening == null)
        {
            passed = false; Debug.LogError("UI targets probe FAIL: chest preview unavailable");
        }
        else
        {
            encounters.AddEncounter(EEncounter.ChestNormal);
            yield return new WaitForSecondsRealtime(1f);
            yield return new WaitForEndOfFrame();
            Capture("ChestOpening", "chest");
            ScreenCapture.CaptureScreenshot("ui-target-chest.png");
            var target = chest.chestOpening.cam.targetTexture;
            int identity = target != null ? target.GetInstanceID() : 0;
            if (target != null) target.Release();
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture("ChestOpening", "chest-recovered");
            passed &= target != null && target.GetInstanceID() == identity && chest.chestOpening.cam.targetTexture == target;
            yield return new WaitForSecondsRealtime(.3f);
            encounters.RewardFinished();
        }
        Debug.Log($"UI targets probe {(passed ? "PASS" : "FAIL")}: actual minimap/map/chest pixel variation, shared RawImage bindings and lost-target recovery; gpu={SystemInfo.graphicsDeviceName}, api={SystemInfo.graphicsDeviceType}");
        Camera.onPostRender -= OnCameraRendered;
        Destroy(gameObject);
    }

    private void OnCameraRendered(Camera camera)
    {
        if (!OpenBonkUiRenderTargets.IsUiTarget(camera.targetTexture)) return;
        string name = camera.targetTexture.name;
        renders.TryGetValue(name, out int count); renders[name] = count + 1;
    }

    private void OnDestroy() { Camera.onPostRender -= OnCameraRendered; }

    private void Capture(string targetName, string stage)
    {
        Camera camera = null;
        foreach (var candidate in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (candidate.targetTexture != null && candidate.targetTexture.name == targetName) { camera = candidate; break; }
        var target = camera != null ? camera.targetTexture : null;
        if (target == null || !target.IsCreated())
        {
            passed = false; Debug.LogError($"UI targets probe FAIL: {stage}, target not created"); return;
        }
        int bindings = 0, activeBindings = 0, directBindings = 0, materialBindings = 0;
        foreach (var raw in FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // RawImage.mainTexture is what Graphic.UpdateMaterial submits to
            // CanvasRenderer. The pause map inherits MapFogOfWar._MainTex;
            // its texture field is null, unlike the inactive hold-map overlay.
            if (raw.mainTexture != target) continue;
            bindings++;
            bool direct = raw.texture == target;
            if (direct) directBindings++; else materialBindings++;
            bool visible = raw.isActiveAndEnabled && raw.canvas != null && raw.canvas.isActiveAndEnabled &&
                !raw.canvasRenderer.cull && raw.canvasRenderer.GetInheritedAlpha() > .01f && raw.color.a > .01f &&
                raw.rectTransform.rect.width > 1f && raw.rectTransform.rect.height > 1f;
            if (visible) activeBindings++;
            Debug.Log($"UI target binding: stage={stage}, image={raw.name}, source={(direct ? "texture" : "material._MainTex")}, material={(raw.material != null ? raw.material.name : "none")}, visible={visible}, targetId={raw.mainTexture.GetInstanceID()}");
        }
        var previous = RenderTexture.active;
        var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
            var colors = new HashSet<int>();
            int bright = 0;
            foreach (var c in pixels.GetPixels32())
            {
                if (Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > 40) bright++;
                colors.Add(((c.r >> 3) << 10) | ((c.g >> 3) << 5) | (c.b >> 3));
            }
            renders.TryGetValue(targetName, out int frames);
            bool ok = camera.isActiveAndEnabled && frames > 0 && bindings > 0 && activeBindings > 0 && bright > 32 && colors.Count > 8;
            passed &= ok;
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "ui-target-" + stage + "-pixels.png"), pixels.EncodeToPNG());
            Debug.Log($"UI targets probe {(ok ? "PASS" : "FAIL")}: stage={stage}, created={target.IsCreated()}, color={target.graphicsFormat}, depth={target.depthStencilFormat}, frames={frames}, bindings={bindings}/{activeBindings}, direct={directBindings}, material={materialBindings}, bright={bright}, colors={colors.Count}, hdr={camera.allowHDR}, msaa={target.antiAliasing}");
        }
        finally { RenderTexture.active = previous; Destroy(pixels); }
    }
}
#endif
