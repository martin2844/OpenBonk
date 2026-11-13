using System;
using System.Reflection;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers;
using UnityEditor;
using UnityEngine;

public static class ChallengeLavaPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Height(GameObject obj, float expected, string message) { Check(Mathf.Abs(obj.transform.position.y - expected) < .01f, message + ": " + obj.transform.position.y); }
    public static void Run()
    {
        var effectObject = new GameObject("effect manager"); var effects = effectObject.AddComponent<EffectManager>(); EffectManager.Instance = effects;
        effects.floorIsLava = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/FloorIsLava.prefab"); Check(effects.floorIsLava != null, "exported water prefab missing");
        var terrain = GameObject.CreatePrimitive(PrimitiveType.Cube); terrain.transform.position = new Vector3(1f, 20f, 3f); terrain.transform.localScale = new Vector3(100f, 40f, 100f);
        var mesh = terrain.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(terrain.GetComponent<BoxCollider>()); var collider = terrain.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; Physics.SyncTransforms();
        var display = terrain.AddComponent<MapDisplay>(); display.meshCollider = collider;
        var mapObject = new GameObject("map controller"); var map = mapObject.AddComponent<MapGenerationController>(); var generator = mapObject.AddComponent<MapGenerator>(); map.proceduralMapMeshGenerator = generator;
        typeof(MapGenerator).GetProperty("display").GetSetMethod(true).Invoke(generator, new object[] { display });
        MapInfo.mapCenter = new Vector3(1f, 20f, 3f); MapInfo.mapSize = new Vector3(100f, 40f, 100f);
        var modifier = ScriptableObject.CreateInstance<ChallengeModifierLava>(); modifier.Init(null); MapGenerationController.A_GenerationComplete?.Invoke();
        var lava = (GameObject)typeof(ChallengeModifierLava).GetField("lavaObject", Private).GetValue(modifier); Check(lava != null, "lava created on generation");
        Check((bool)typeof(Water).GetField("isDamageZone", Private).GetValue(lava.GetComponent<Water>()), "lava is damaging");
        modifier.Tick(); Height(lava, -5f, "delay keeps lava below terrain");
        typeof(ChallengeModifierLava).GetField("startTime", Private).SetValue(modifier, -1f);
        MyTime.stageTimer = 5f; modifier.Tick(); Height(lava, -5f, "cycle starts low");
        MyTime.stageTimer = 20f; modifier.Tick(); Height(lava, 15f, "rise halfway");
        MyTime.stageTimer = 35f; modifier.Tick(); Height(lava, 35f, "reaches top");
        MyTime.stageTimer = 50f; modifier.Tick(); Height(lava, 35f, "holds at top");
        MyTime.stageTimer = 57.5f; modifier.Tick(); Height(lava, 15f, "lowers halfway");
        MyTime.stageTimer = 65f; modifier.Tick(); Height(lava, -5f, "holds at bottom");
        MyTime.stageTimer = 85f; modifier.Tick(); Height(lava, -5f, "cycle repeats");
        modifier.Cleanup(); UnityEngine.Object.DestroyImmediate(lava); MapGenerationController.A_GenerationComplete?.Invoke();
        Check(GameObject.Find("FloorIsLava(Clone)") == null, "cleanup removes generation callback");
        EffectManager.Instance = null; UnityEngine.Object.DestroyImmediate(effectObject); UnityEngine.Object.DestroyImmediate(terrain); UnityEngine.Object.DestroyImmediate(mapObject); UnityEngine.Object.DestroyImmediate(modifier);
        Debug.Log("ChallengeLavaPortCheck passed: prefab, terrain bounds, rise, hold, lower, repeat and cleanup");
    }
}
