using System;
using System.Reflection;
using RetroArsenal;
using UnityEditor;
using UnityEngine;

public static class RetroPitchRandomizerPortCheck
{
    public static void Run()
    {
        var originalState = UnityEngine.Random.state;
        var method = typeof(RetroPitchRandomizer).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        int checkedCount = 0;
        try
        {
            foreach (string name in new[] { "AmericanExplosion", "AmericanExplosionStart", "BanditStatue" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
                if (prefab == null) throw new Exception(name + " prefab missing");
                var instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    foreach (var component in instance.GetComponentsInChildren<RetroPitchRandomizer>(true))
                    {
                        var audio = component.GetComponent<AudioSource>();
                        if (audio == null) throw new Exception(name + " missing AudioSource");
                        audio.pitch = 0.8f;
                        UnityEngine.Random.InitState(421);
                        float expected = audio.pitch * (1f + UnityEngine.Random.Range(component.randomPercent / -100f, component.randomPercent / 100f));
                        UnityEngine.Random.InitState(421);
                        method.Invoke(component, null);
                        if (Mathf.Abs(audio.pitch - expected) > 0.00001f) throw new Exception(name + " incorrect pitch");
                        checkedCount++;
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            if (checkedCount < 3) throw new Exception("expected randomizer components on three prefabs");
            var go = new GameObject("default pitch test");
            try
            {
                var randomizer = go.AddComponent<RetroPitchRandomizer>();
                if (randomizer.randomPercent != 10f) throw new Exception("incorrect default");
                method.Invoke(randomizer, null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            Debug.Log("RetroPitchRandomizerPortCheck passed: " + checkedCount + " prefab components");
        }
        finally { UnityEngine.Random.state = originalState; }
    }
}
