#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class OpenBonkLightningAppearanceCheck
{
    public static void Run()
    {
        var type = typeof(DataManager).Assembly.GetType("OpenBonkLightningAppearance", true);
        var apply = type.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (string path in new[]{"Assets/GameObject/LightningStrikeFx.prefab", "Assets/GameObject/ChainLightning.prefab"})
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new Exception("missing prefab " + path);
            var obj = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var particles = obj.GetComponentsInChildren<ParticleSystem>(true);
                var lines = obj.GetComponentsInChildren<LineRenderer>(true);
                var source = obj.GetComponent<AudioSource>();
                var initial = particles.Length == 0 ? default(ParticleSystem.MinMaxGradient) : particles[0].main.startColor;
                var initialLine = lines.Length == 0 ? Color.clear : lines[0].startColor;
                var initialClip = source == null ? null : source.clip;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/OpenBonkModAuthoring/Pikachu/thunderbolt-zap.wav");
                if (clip == null) throw new Exception("missing zap audio");
                var blue = new Color(.24f,.79f,1f,1f);
                apply.Invoke(null,new object[]{obj,(Color?)blue,clip});
                if (particles.Length > 0 && particles[0].main.startColor.color != blue) throw new Exception("particle color did not apply");
                if (lines.Length > 0 && !((Color32)lines[0].startColor).Equals((Color32)blue)) throw new Exception("line color did not apply");
                if (source != null && source.clip != clip) throw new Exception("sound did not apply");
                var component = obj.GetComponent(type);
                type.GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);
                if (particles.Length > 0 && particles[0].main.startColor.mode != initial.mode) throw new Exception("particle mode not restored");
                if (particles.Length > 0 && particles[0].main.startColor.color != initial.color) throw new Exception("particle color not restored");
                if (lines.Length > 0 && !((Color32)lines[0].startColor).Equals((Color32)initialLine)) throw new Exception("line color not restored");
                if (source != null && source.clip != initialClip) throw new Exception("sound not restored");
                Debug.Log("Thunderbolt appearance PASS: " + path + " particles=" + particles.Length + " lines=" + lines.Length + " audio=" + (source != null));
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }
    }
}
#endif
