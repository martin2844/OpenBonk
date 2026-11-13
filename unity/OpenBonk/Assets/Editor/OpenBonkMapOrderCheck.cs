#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class OpenBonkMapOrderCheck
{
    public static void Run()
    {
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var desert = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapDesert.asset");
        var a = ScriptableObject.CreateInstance<OpenBonkExternalMapData>();
        var b = ScriptableObject.CreateInstance<OpenBonkExternalMapData>();
        var c = ScriptableObject.CreateInstance<OpenBonkExternalMapData>();
        try
        {
            a.displayName = "Beta"; a.externalId = "z:beta"; a.unlockOrder = -100;
            b.displayName = "alpha"; b.externalId = "x:alpha"; b.unlockOrder = 100;
            c.displayName = "beta"; c.externalId = "a:beta"; c.unlockOrder = 0;
            var result = MapSelectionWindow.SortMaps(new MapData[] { a, desert, null, c, forest, b });
            if (result.Count != 5 || result[0] != forest || result[1] != desert || result[2] != b || result[3] != c || result[4] != a)
                throw new Exception("Original and external map ordering incorrect");
            Debug.Log("OpenBonk map ordering PASS: original unlock order, sorted mods, stable IDs, null ignored");
        }
        finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(c); }
    }
}
#endif
