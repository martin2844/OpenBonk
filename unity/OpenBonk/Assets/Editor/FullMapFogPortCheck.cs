using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FullMapFogPortCheck
{
	const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
	public static void Run()
	{
		var go = new GameObject("fog check");
		go.SetActive(false);
		var map = go.AddComponent<FullMap>();
		var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/MapFogOfWar.mat");
		if (!source || !source.shader.isSupported) throw new Exception("Map fog shader unavailable");
		var material = new Material(source);
		map.mapMaterial = material;
		typeof(FullMap).GetField("worldSize", Private).SetValue(map, 1000f);
		typeof(FullMap).GetMethod("InitFogTexture", Private).Invoke(map, null);
		var texture = (Texture2D)typeof(FullMap).GetField("fogTexture", Private).GetValue(map);
		try
		{
			if (texture.GetPixel(32, 32).r > .01f) throw new Exception("Fog mask should begin hidden");
			var queued = (HashSet<int>)typeof(FullMap).GetField("revealedIndices", Private).GetValue(map);
			queued.Add(32 * 64 + 32);
			typeof(FullMap).GetMethod("RevealFog", Private).Invoke(map, null);
			if (texture.GetPixel(32, 32).r < .99f || texture.GetPixel(0, 0).r > .01f || queued.Count != 0)
				throw new Exception("Fog reveal did not uncover the center and preserve distant fog");
			CheckShader(material);
			Debug.Log("FullMapFogPortCheck passed: hidden mask, local reveal, distant fog and queue cleanup");
		}
		finally { UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(go); }
	}

	private static void CheckShader(Material material)
	{
		var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.GetComponent<Renderer>().sharedMaterial = material;
		var cameraObject = new GameObject("fog shader camera");
		var camera = cameraObject.AddComponent<Camera>();
		camera.transform.position = new Vector3(0, 0, -2);
		camera.transform.LookAt(Vector3.zero);
		camera.orthographic = true;
		camera.orthographicSize = .5f;
		var target = new RenderTexture(32, 32, 24);
		var image = new Texture2D(32, 32, TextureFormat.RGBA32, false);
		camera.targetTexture = target;
		material.SetTexture("_MainTex", Texture2D.whiteTexture);
		material.SetTexture("_FogNoise", Texture2D.blackTexture);
		material.SetColor("_FogColor", Color.red);
		material.SetFloat("_FogIntensity", 1f);
		try
		{
			Color Render(Texture mask)
			{
				material.SetTexture("_FogTex", mask);
				camera.Render(); RenderTexture.active = target;
				image.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); image.Apply();
				return image.GetPixel(16, 16);
			}
			Color hidden = Render(Texture2D.blackTexture), revealed = Render(Texture2D.whiteTexture);
			if (hidden.r < .8f || hidden.g > .1f || revealed.g < .8f) throw new Exception($"Fog shader mismatch: hidden={hidden}, revealed={revealed}");
		}
		finally
		{
			RenderTexture.active = null;
			UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
			UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad);
		}
	}
}
