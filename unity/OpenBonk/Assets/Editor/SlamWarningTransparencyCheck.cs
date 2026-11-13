using System;
using UnityEditor;
using UnityEngine;

public static class SlamWarningTransparencyCheck
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SphereSlam.prefab");
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/WarningFlat.mat");
        if (!prefab || !original || !original.shader.isSupported || ShaderUtil.ShaderHasError(original.shader)) throw new Exception("warning prefab or shader unavailable");
        MeshFilter selected = null;
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer && renderer.sharedMaterial == original) { selected = filter; break; }
        }
        if (!selected || !selected.sharedMesh) throw new Exception("original warning mesh binding missing");
        var root = new GameObject("warning transparency test");
        var filterCopy = root.AddComponent<MeshFilter>(); filterCopy.sharedMesh = selected.sharedMesh;
        var rendererCopy = root.AddComponent<MeshRenderer>(); rendererCopy.sharedMaterial = original;
        var cameraObject = new GameObject("warning camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.blue; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        try
        {
            // The warning mesh is a horizontal, upward-facing disk.
            camera.transform.position = new Vector3(0, 4, 0); camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); image.Apply();
            Color center = image.GetPixel(32, 32);
            if (center.r < .02f || center.r > .4f || center.b < .55f) throw new Exception("warning is not translucent over blue background: " + center);
            Debug.Log("SlamWarningTransparencyCheck passed: original warning mesh and material blend over background, center=" + center);
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(root);
        }
        EditorApplication.Exit(0);
    }
}
