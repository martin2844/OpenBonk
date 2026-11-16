using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TriPlanarAdditionalLightCheck
{
    public static void Run()
    {
        var shader = Shader.Find("Toon/Lit Tri Planar Noise Blend");
        if (shader == null || !shader.isSupported) throw new Exception("Triplanar shader unavailable");
        var material = new Material(shader);
        var surface = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var cameraObject = new GameObject("Triplanar test camera");
        var mainObject = new GameObject("Triplanar test main light");
        var pointObject = new GameObject("Triplanar test additional light");
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var previousTarget = RenderTexture.active;
        var previousAmbient = RenderSettings.ambientLight;
        var previousAmbientMode = RenderSettings.ambientMode;
        var previousFog = RenderSettings.fog;
        var previousPixelLights = QualitySettings.pixelLightCount;
        try
        {
            surface.GetComponent<Renderer>().sharedMaterial = material;
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
            material.SetTexture("_TopTex2", Texture2D.whiteTexture);
            material.SetTexture("_MainTexSide", Texture2D.whiteTexture);
            material.SetTexture("_BelowYTexture", Texture2D.whiteTexture);
            material.SetTexture("_Ramp", Texture2D.whiteTexture);
            material.SetColor("_Color", new Color(.15f, .15f, .15f, 1));
            material.SetColor("_EdgeColor", Color.black);
            material.SetFloat("_OutlineWidth", 0);
            if (material.FindPass("FORWARD_ADD") < 0) throw new Exception("Additional light pass missing");
            var camera = cameraObject.AddComponent<Camera>();
            camera.renderingPath = RenderingPath.Forward;
            camera.transform.position = new Vector3(0, 0, -4);
            camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true; camera.orthographicSize = 1;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.targetTexture = target;
            var main = mainObject.AddComponent<Light>();
            main.type = LightType.Directional; main.intensity = .2f; main.transform.rotation = Quaternion.identity;
            var point = pointObject.AddComponent<Light>();
            point.type = LightType.Point; point.range = 8; point.intensity = 0;
            point.renderMode = LightRenderMode.ForcePixel; point.transform.position = new Vector3(0, 0, -2);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = false; QualitySettings.pixelLightCount = 4;
            point.intensity = 2;
            material.SetShaderPassEnabled("ForwardAdd", false);
            camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); image.Apply();
            var before = image.GetPixel(32, 32);
            material.SetShaderPassEnabled("ForwardAdd", true);
            camera.Render(); image.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); image.Apply();
            var after = image.GetPixel(32, 32);
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) throw new Exception(message.message);
            if (after.r <= before.r + .05f) throw new Exception("Additional light did not brighten the surface: " + before + " -> " + after);
            Debug.Log("TriPlanarAdditionalLightCheck passed: " + before + " -> " + after);
        }
        finally
        {
            RenderTexture.active = previousTarget; RenderSettings.ambientLight = previousAmbient;
            RenderSettings.ambientMode = previousAmbientMode; RenderSettings.fog = previousFog;
            QualitySettings.pixelLightCount = previousPixelLights;
            var camera = cameraObject.GetComponent<Camera>();
            if (camera != null) camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(surface);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(mainObject); UnityEngine.Object.DestroyImmediate(pointObject);
        }
    }
}
