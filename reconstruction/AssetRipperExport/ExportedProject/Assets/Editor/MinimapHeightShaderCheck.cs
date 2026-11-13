using System;
using UnityEditor;
using UnityEngine;

public static class MinimapHeightShaderCheck
{
	public static void Run()
	{
		var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_meshHeight.mat");
		if (!source || !source.shader.isSupported) throw new Exception("Minimap material unavailable");
		var material = new Material(source);
		material.SetColor("_Color", Color.black);
		material.SetColor("_MaxColor", Color.white);
		material.SetFloat("_MinDistance", -1f);
		material.SetFloat("_MaxDistance", 1f);
		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.layer = 30;
		quad.transform.localScale = new Vector3(2, 2, 1);
		quad.GetComponent<Renderer>().sharedMaterial = material;
		var cameraObject = new GameObject("minimap height check camera");
		var camera = cameraObject.AddComponent<Camera>();
		camera.cullingMask = 1 << 30;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = Color.black;
		camera.renderingPath = RenderingPath.Forward;
		camera.transform.position = new Vector3(0, 0, -4);
		camera.transform.LookAt(Vector3.zero);
		camera.orthographic = true;
		camera.orthographicSize = 1f;
		var target = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32);
		var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
		camera.targetTexture = target;
		bool oldFog = RenderSettings.fog;
		int oldPixelLights = QualitySettings.pixelLightCount;
		var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		var lightStates = new bool[lights.Length];
		for (int i=0; i<lights.Length; i++) { lightStates[i]=lights[i].enabled; lights[i].enabled=false; }
		GameObject directional = null, point = null;
		try
		{
			RenderSettings.fog = false;
			camera.Render();
			RenderTexture.active = target;
			image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
			image.Apply();
			float low = image.GetPixel(64, 16).r, high = image.GetPixel(64, 112).r;
			if (high - low < .35f) throw new Exception($"Height gradient missing: low={low}, high={high}");
			material.SetColor("_Color", new Color(.12f,.12f,.12f,1));
			material.SetColor("_MaxColor", new Color(.12f,.12f,.12f,1));
			camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
			float baseline=image.GetPixel(64,64).r;
			QualitySettings.pixelLightCount=4;
			directional=new GameObject("minimap directional"); var sun=directional.AddComponent<Light>(); sun.type=LightType.Directional; sun.cullingMask=1<<30; sun.intensity=1f;
			point=new GameObject("minimap point"); point.transform.position=new Vector3(0,0,-2); var lamp=point.AddComponent<Light>(); lamp.type=LightType.Point; lamp.renderMode=LightRenderMode.ForcePixel; lamp.range=10; lamp.cullingMask=1<<30;
			camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
			float lit=image.GetPixel(64,64).r;
			if(lit<baseline*1.3f) throw new Exception($"Additional light pass absent: baseline={baseline:F3}, lit={lit:F3}");
			lamp.renderMode=LightRenderMode.ForceVertex;
			camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
			float vertexLit=image.GetPixel(64,64).r;
			if(vertexLit<=baseline+.01f) throw new Exception($"Vertex lights absent: baseline={baseline:F3}, vertexLit={vertexLit:F3}");
			Debug.Log($"MinimapHeightShaderCheck passed: low={low:F3}, high={high:F3}, baseline={baseline:F3}, lit={lit:F3}, vertexLit={vertexLit:F3}");
		}
		finally
		{
			RenderSettings.fog = oldFog;
			QualitySettings.pixelLightCount = oldPixelLights;
			for(int i=0; i<lights.Length; i++) if(lights[i]!=null) lights[i].enabled=lightStates[i];
			if(directional!=null) UnityEngine.Object.DestroyImmediate(directional);
			if(point!=null) UnityEngine.Object.DestroyImmediate(point);
			RenderTexture.active = null;
			UnityEngine.Object.DestroyImmediate(image);
			UnityEngine.Object.DestroyImmediate(target);
			UnityEngine.Object.DestroyImmediate(cameraObject);
			UnityEngine.Object.DestroyImmediate(quad);
			UnityEngine.Object.DestroyImmediate(material);
		}
	}
}
