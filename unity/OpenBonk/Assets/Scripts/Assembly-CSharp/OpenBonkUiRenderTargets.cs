using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// The imported UI targets request RGBA16_UNorm / D32S8 desktop formats.
// Keep their asset identity: scene cameras, RawImages and materials share it.
public static class OpenBonkUiRenderTargets
{
    private static readonly Dictionary<int, float> retryAfter = new Dictionary<int, float>();

#if UNITY_ANDROID && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        retryAfter.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Camera.onPreCull -= BeforeCameraRender;
        Camera.onPreCull += BeforeCameraRender;
    }
#endif

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        retryAfter.Clear();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                Prepare(camera);
    }

    private static void BeforeCameraRender(Camera camera) { Prepare(camera); }

    public static bool IsUiTarget(RenderTexture target)
    {
        if (target == null) return false;
        switch (target.name)
        {
            case "MinimapTexture":
            case "MapTexture":
            case "ChestOpening":
            case "EnemyDisplay":
                return true;
            default:
                return false;
        }
    }

    private static bool SupportsColor(GraphicsFormat format)
    {
        return SystemInfo.IsFormatSupported(format, FormatUsage.Render) &&
               SystemInfo.IsFormatSupported(format, FormatUsage.Sample);
    }

    public static bool Prepare(Camera camera)
    {
        if (camera == null || !IsUiTarget(camera.targetTexture)) return false;
        var target = camera.targetTexture;
        // These LDR UI cameras do not need an HDR intermediate or MSAA resolve.
        camera.allowHDR = false;
        camera.allowMSAA = false;
        int id = target.GetInstanceID();
        if (retryAfter.TryGetValue(id, out float retry) && Time.realtimeSinceStartup < retry)
            return false;

        var color = target.sRGB ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
        // Preserve the native stencil attachment (chest outlines/preview effects)
        // rather than falling back to a depth-only format.
        var depth = SystemInfo.IsFormatSupported(GraphicsFormat.D24_UNorm_S8_UInt, FormatUsage.Render)
            ? GraphicsFormat.D24_UNorm_S8_UInt
            : SystemInfo.IsFormatSupported(GraphicsFormat.D32_SFloat_S8_UInt, FormatUsage.Render)
                ? GraphicsFormat.D32_SFloat_S8_UInt : GraphicsFormat.None;
        if (!SupportsColor(color) || depth == GraphicsFormat.None)
            return Failed(target, "no supported RGBA8 color and depth/stencil attachment");

        var descriptor = target.descriptor;
        bool changed = descriptor.graphicsFormat != color || descriptor.depthStencilFormat != depth ||
            descriptor.msaaSamples != 1 || descriptor.useMipMap || descriptor.autoGenerateMips ||
            descriptor.bindMS || descriptor.enableRandomWrite || descriptor.memoryless != RenderTextureMemoryless.None;
        if (changed)
        {
            target.Release();
            descriptor.graphicsFormat = color;
            descriptor.depthStencilFormat = depth;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.mipCount = 1;
            descriptor.enableRandomWrite = false;
            descriptor.memoryless = RenderTextureMemoryless.None;
            target.descriptor = descriptor;
        }

        // Lost targets (for example after suspend) must be recreated in place.
        if (target.IsCreated()) return true;
        if (!target.Create() || !target.IsCreated()) return Failed(target, "Create failed");
        retryAfter.Remove(id);
        Debug.Log($"OpenBonk UI render target ready: {target.name}, {target.width}x{target.height}, color={target.graphicsFormat}, depth={target.depthStencilFormat}, gpu={SystemInfo.graphicsDeviceName}, api={SystemInfo.graphicsDeviceType}");
        return true;
    }

    private static bool Failed(RenderTexture target, string reason)
    {
        retryAfter[target.GetInstanceID()] = Time.realtimeSinceStartup + 5f;
        Debug.LogWarning($"OpenBonk UI render target unavailable: {target.name}, {reason}, gpu={SystemInfo.graphicsDeviceName}, api={SystemInfo.graphicsDeviceType}");
        return false;
    }
}
