using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class WaterSmoothnessRenderCheck
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/WaterForest.mat");
        var lava = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/LavaCrypt.mat");
        if (source == null || lava == null || source.GetFloat("_Smoothness") != 10f || lava.GetFloat("_Smoothness") != 0f)
            throw new Exception("Expected original Forest water and crypt lava smoothness values");
        var material = new Material(source);
        material.SetFloat("_NormalStrenght", 0f);
        material.SetColor("_BottomColor", Color.white); material.SetColor("_MidColor", Color.white);
        material.SetColor("_TopColor", Color.black); material.SetColor("_FoamColor", Color.black);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("water check camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 0f, -4f); camera.orthographic = true; camera.orthographicSize = .7f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.depthTextureMode = DepthTextureMode.Depth;
        var lightObject = new GameObject("water check light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(35f, 25f, 0f); light.intensity = 1f;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.black;
        var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        try
        {
            material.SetFloat("_Smoothness", 1f); var one = Capture(camera, target, image);
            material.SetFloat("_Smoothness", 10f); var ten = Capture(camera, target, image);
            int changed = 0; long difference = 0;
            for (int i = 0; i < one.Length; i++) {
                int delta = Math.Abs(one[i].r-ten[i].r)+Math.Abs(one[i].g-ten[i].g)+Math.Abs(one[i].b-ten[i].b);
                if (delta > 3) changed++; difference += delta;
            }
            if (changed < 100) throw new Exception($"Smoothness 10 did not change the GPU result: changed={changed} delta={difference}");
            Debug.Log($"WaterSmoothnessRenderCheck PASS: water=10 lava=0 changed={changed} rgbDelta={difference}");
        }
        finally {
            camera.targetTexture=null; RenderTexture.active=null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(quad);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }
    static Color32[] Capture(Camera camera, RenderTexture target, Texture2D image)
    {
        camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); return image.GetPixels32();
    }
}
