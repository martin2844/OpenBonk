using System;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using UnityEditor;
using UnityEngine;

public static class OriginalMapRoutePortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("OriginalMapRoutePortCheck: " + message); }
    public static void Run()
    {
        var route=typeof(MapGenerationController).GetMethod("CanUseRecoveredMap",BindingFlags.Static|BindingFlags.NonPublic);
        Check(route != null, "route selector exists");
        foreach (string name in new[]{"MapForest", "MapGraveyard", "MapDungeon", "MapDesert", "MapHell"}) {
            var map=AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/"+name+".asset");
            Check(map != null && (bool)route.Invoke(null,new object[]{map}),name+" selects recovered generation");
        }
        Check(!(bool)route.Invoke(null,new object[]{null}),"missing map is rejected");
        var unknown=ScriptableObject.CreateInstance<MapData>();
        try { unknown.mapType=(EMapType)999; Check(!(bool)route.Invoke(null,new object[]{unknown}),"unknown map type is rejected"); }
        finally { UnityEngine.Object.DestroyImmediate(unknown); }
        Debug.Log("OriginalMapRoutePortCheck passed: original map assets select recovered tile or procedural generation, missing/unknown maps rejected");
    }
}
