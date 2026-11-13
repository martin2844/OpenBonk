using System;
using UnityEditor;
using UnityEngine;

public static class ForestOutlineNoisePortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_fillMiddle.mat");
        if (source == null || !source.IsKeywordEnabled("_MK_OUTLINE_NOISE")) throw new Exception("Forest material or outline keyword missing");
        var material = new Material(source);
        var model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        model.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("outline noise camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -4);
        camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var target = new RenderTexture(256, 256, 24);
        var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        try
        {
            material.SetColor("_OutlineColor", Color.green);
            material.SetFloat("_Outline", 3f); material.SetFloat("_OutlineSize", 300f);
            material.SetFloat("_OutlineNoise", 0f);
            camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
            var baseline = image.GetPixels32();
            material.SetFloat("_OutlineNoise", 1f);
            camera.Render(); image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
            var changed = image.GetPixels32(); int different = 0;
            for (int i = 0; i < baseline.Length; i++) if (!baseline[i].Equals(changed[i])) different++;
            if (different < 10) throw new Exception("Outline noise did not change the rendered image: " + different);
            material.DisableKeyword("_MK_OUTLINE_NOISE");
            camera.Render(); image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
            var disabled = image.GetPixels32(); int resetDifference = 0;
            for (int i = 0; i < baseline.Length; i++) if (!baseline[i].Equals(disabled[i])) resetDifference++;
            if (resetDifference != 0) throw new Exception("Disabling keyword did not restore baseline: " + resetDifference);
            Debug.Log("ForestOutlineNoisePortCheck passed: changed pixels=" + different + ", reset difference=" + resetDifference);
        }
        finally
        {
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(model); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
