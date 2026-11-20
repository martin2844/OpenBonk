using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public static class UiRenderTargetCompatibilityCheck
{
    public static void Run()
    {
        foreach (string name in new[] { "MinimapTexture", "MapTexture", "ChestOpening", "EnemyDisplay" })
            CheckTarget(name);
        CheckUnrelatedCamera();
        Debug.Log("UiRenderTargetCompatibilityCheck PASS: four UI targets render color/alpha, retain depth/stencil and shared bindings, and recover after Release; unrelated camera unchanged");
    }

    private static void CheckTarget(string name)
    {
        var source = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/RenderTexture/" + name + ".renderTexture");
        Require(source != null, "Missing imported target " + name);
        var target = UnityEngine.Object.Instantiate(source);
        target.name = name;
        target.width = 96; target.height = 64;
        var root = new GameObject("UI target check");
        var camera = root.AddComponent<Camera>();
        camera.enabled = false;
        camera.targetTexture = target;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.cullingMask = 0;
        camera.backgroundColor = new Color(.125f, .5f, .75f, .375f);
        camera.allowHDR = true; camera.allowMSAA = true;
        var imageObject = new GameObject("shared RawImage binding", typeof(RectTransform), typeof(RawImage));
        var raw = imageObject.GetComponent<RawImage>(); raw.texture = target;
        var shader = Shader.Find("Unlit/Texture");
        Require(shader != null, "Unlit/Texture unavailable for binding check");
        var material = new Material(shader); material.mainTexture = target;
        var pixels = new Texture2D(96, 64, TextureFormat.RGBA32, false, true);
        var oldActive = RenderTexture.active;
        try
        {
            for (int cycle = 0; cycle < 3; cycle++)
            {
                if (cycle == 2) target.Release(); // simulate a lost graphics allocation
                Require(OpenBonkUiRenderTargets.Prepare(camera), name + " preparation failed");
                Require(target.IsCreated(), name + " GPU target missing");
                Require(ReferenceEquals(camera.targetTexture, target) && ReferenceEquals(raw.texture, target) && ReferenceEquals(material.mainTexture, target), name + " shared binding replaced");
                Require(target.width == 96 && target.height == 64 && target.antiAliasing == 1 && !target.useMipMap && !target.autoGenerateMips, name + " dimensions/sampling changed unexpectedly");
                Require(target.graphicsFormat == GraphicsFormat.R8G8B8A8_UNorm || target.graphicsFormat == GraphicsFormat.R8G8B8A8_SRGB, name + " not RGBA8");
                Require(target.depthStencilFormat == GraphicsFormat.D24_UNorm_S8_UInt || target.depthStencilFormat == GraphicsFormat.D32_SFloat_S8_UInt, name + " stencil attachment lost");
                Require(!camera.allowHDR && !camera.allowMSAA, name + " HDR/MSAA intermediate enabled");
                IntPtr allocation = target.GetNativeTexturePtr();
                Require(OpenBonkUiRenderTargets.Prepare(camera) && target.GetNativeTexturePtr() == allocation, name + " repeated preparation reallocates");
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 96, 64), 0, 0); pixels.Apply();
                Color color = pixels.GetPixel(48, 32);
                Require(color.b > .4f && color.g > color.r + .1f && color.a > .3f && color.a < .45f, name + " color/alpha readback failed: " + color);
            }
            Debug.Log($"UI target check PASS: {name}, color={target.graphicsFormat}, depth={target.depthStencilFormat}, preservedBindings=true, restoredAfterRelease=true");
        }
        finally
        {
            RenderTexture.active = oldActive;
            camera.targetTexture = null;
            target.Release();
            foreach (var obj in new UnityEngine.Object[] { pixels, material, imageObject, root, target })
                UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    private static void CheckUnrelatedCamera()
    {
        var root = new GameObject("unrelated camera");
        var camera = root.AddComponent<Camera>(); camera.enabled = false;
        var target = new RenderTexture(48, 32, 16) { name = "UnrelatedGameplayTarget" };
        camera.targetTexture = target; camera.allowHDR = true; camera.allowMSAA = true;
        var before = target.descriptor;
        bool wasCreated = target.IsCreated();
        try
        {
            Require(!OpenBonkUiRenderTargets.Prepare(camera), "Unrelated camera accepted");
            Require(camera.allowHDR && camera.allowMSAA && camera.targetTexture == target && target.IsCreated() == wasCreated && target.descriptor.Equals(before), "Unrelated camera mutated");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(target); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
