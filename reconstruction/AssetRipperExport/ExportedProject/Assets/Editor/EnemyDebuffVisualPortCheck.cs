using System;
using System.IO;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using UnityEditor;
using UnityEngine;

public static class EnemyDebuffVisualPortCheck
{
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Enemy1.prefab");
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/EntStage2.asset");
        if (!prefab || !data || !data.material || !data.material.shader.isSupported) throw new Exception("Enemy prefab/material unavailable");
        var enemyObject = UnityEngine.Object.Instantiate(prefab);
        var enemy = enemyObject.GetComponent<Enemy>();
        var visual = enemyObject.GetComponentInChildren<EnemyRenderer>(true);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var sphereRenderer = sphere.GetComponent<Renderer>();
        var cameraObject = new GameObject("enemy visual test camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var lightObject = new GameObject("enemy visual test light");
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1f; light.transform.rotation = Quaternion.identity;
        var target = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image = new Texture2D(128,128,TextureFormat.RGBA32,false);
        camera.targetTexture = target;
        var oldMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.1f,.1f,.1f);
        try
        {
            if (!enemy || !visual || !visual.enemyRenderer) throw new Exception("Enemy prefab bindings unavailable");
            visual.Set(data);
            typeof(EnemyRenderer).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, null);
            sphereRenderer.sharedMaterial = data.material;
            var block = new MaterialPropertyBlock();
            Color32[] Render()
            {
                visual.enemyRenderer.GetPropertyBlock(block); sphereRenderer.SetPropertyBlock(block);
                camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); return image.GetPixels32();
            }
            var baseline = Render();
            enemy.debuffs.Add(EDebuff.Freeze, null); enemy.A_DebuffAdded(EDebuff.Freeze);
            var frozen = Render();
            int changed = 0; long sum = 0;
            for (int i = 0; i < baseline.Length; i++)
            {
                int delta = Math.Abs(baseline[i].r-frozen[i].r) + Math.Abs(baseline[i].g-frozen[i].g) + Math.Abs(baseline[i].b-frozen[i].b);
                if (delta > 8) changed++; sum += delta;
            }
            if (changed < 200) throw new Exception($"Freeze event did not visibly affect the original material: changed={changed}, sum={sum}");
            File.WriteAllBytes("/home/your-user/megabonk/build/android/enemy-freeze-visual.png", image.EncodeToPNG());
            enemy.debuffs.Clear(); enemy.A_DebuffRemoved(EDebuff.Freeze);
            var restored = Render();
            int remaining = 0;
            for (int i = 0; i < baseline.Length; i++) if (Math.Abs(baseline[i].r-restored[i].r)+Math.Abs(baseline[i].g-restored[i].g)+Math.Abs(baseline[i].b-restored[i].b)>8) remaining++;
            if (remaining > 10) throw new Exception($"Color did not return to normal: {remaining} pixels");
            Debug.Log($"ENEMY_DEBUFF_VISUAL_PORT_CHECK_OK changed={changed}, restoredDifference={remaining}, shader={data.material.shader.name}");
            typeof(EnemyRenderer).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(visual, null);
        }
        finally
        {
            RenderTexture.active = null; camera.targetTexture = null; RenderSettings.ambientMode = oldMode; RenderSettings.ambientLight = oldAmbient;
            UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(sphere); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
