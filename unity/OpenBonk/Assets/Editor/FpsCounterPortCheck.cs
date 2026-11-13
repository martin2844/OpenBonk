using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FpsCounterPortCheck
{
    public static void Run()
    {
        foreach (string scene in new[] { "GeneratedMap", "FinalBossMap" })
        {
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + scene + ".unity");
            var counters = UnityEngine.Object.FindObjectsOfType<FpsCounter>(true);
            if (counters.Length == 0) throw new Exception(scene + ": no counters");
            foreach (var counter in counters)
            {
                if (counter.text == null) throw new Exception(scene + ": missing text");
                var samples = new List<float> { 30, 60, 90 };
                typeof(FpsCounter).GetField("samples", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(counter, samples);
                typeof(FpsCounter).GetMethod("UpdateFps", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(counter, null);
                if (counter.text.text != "60" || samples.Count != 0) throw new Exception(scene + ": averaging or sample reset failed");
                typeof(FpsCounter).GetMethod("UpdateFps", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(counter, null);
                if (counter.text.text != "60") throw new Exception(scene + ": empty sample guard failed");
            }
            Debug.Log("FpsCounterPortCheck " + scene + ": " + counters.Length + " scene counters, average and reset passed");
        }
    }
}
