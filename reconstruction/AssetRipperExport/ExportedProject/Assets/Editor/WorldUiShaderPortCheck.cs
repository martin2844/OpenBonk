using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WorldUiShaderPortCheck
{
    public static void Run()
    {
        var cameraObject = new GameObject("ui test camera"); var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.orthographic = true; camera.orthographicSize = 5f; camera.transform.position = new Vector3(0,0,-10);
        var rt = new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); camera.targetTexture = rt;
        var occluder = GameObject.CreatePrimitive(PrimitiveType.Cube); occluder.transform.position = new Vector3(0,0,-9.5f); occluder.transform.localScale = new Vector3(30,30,.1f); var green = new Material(Shader.Find("Unlit/Color")); green.color = Color.green; occluder.GetComponent<Renderer>().sharedMaterial = green;
        var root = new GameObject("ui canvas", typeof(RectTransform), typeof(Canvas)); var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        var imageObject = new GameObject("image", typeof(RectTransform), typeof(RawImage)); imageObject.transform.SetParent(root.transform,false); var image = imageObject.GetComponent<RawImage>(); image.rectTransform.sizeDelta = new Vector2(160,100);
        var texture = new Texture2D(2,1,TextureFormat.RGBA32,false); texture.filterMode = FilterMode.Point; texture.SetPixels(new [] { Color.white, Color.clear }); texture.Apply(); image.texture = texture; image.color = Color.red;
        image.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_TopUILinuxSupportAsWellUseThisLol.mat"); if (image.material == null) throw new Exception("original material missing");
        Canvas.ForceUpdateCanvases(); camera.Render(); var capture = new Texture2D(256,256,TextureFormat.RGBA32,false); RenderTexture.active = rt; capture.ReadPixels(new Rect(0,0,256,256),0,0); capture.Apply();
        int red=0, greenCount=0; for(int y=78;y<178;y++) for(int x=48;x<208;x++) { var c=capture.GetPixel(x,y); if(c.r>.7f && c.g<.2f) red++; if(c.g>.7f && c.r<.2f) greenCount++; }
        File.WriteAllBytes("/home/your-user/megabonk/build/android/world-ui-shader-check.png", capture.EncodeToPNG());
        if(red<3000 || greenCount<3000) throw new Exception("expected red tinted image above occluder and transparent pixels: red="+red+" green="+greenCount);
        Debug.Log("WorldUiShaderPortCheck passed: original material, per-vertex red tint, transparent pixels and always-on-top depth; red="+red+" green="+greenCount);
        RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(capture); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(occluder); UnityEngine.Object.DestroyImmediate(green); UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
