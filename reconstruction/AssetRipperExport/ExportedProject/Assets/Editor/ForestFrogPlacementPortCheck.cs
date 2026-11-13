using System;
using System.Reflection;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using UnityEditor;
using UnityEngine;

public static class ForestFrogPlacementPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ForestFrogPlacementPortCheck: " + message); }
    static InteractableEgg[] Eggs() => UnityEngine.Object.FindObjectsOfType<InteractableEgg>();
    public static void Run() {
        var root = new GameObject("frog placement fixture"); root.SetActive(false);
        var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
        var effect = root.AddComponent<EffectManager>();
        effect.frogQuest1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuestEggGreen.prefab");
        effect.frogQuest2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuestEggBlue.prefab");
        effect.frogQuest3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/QuestEggRed.prefab");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "frog test ground"; ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.localScale = new Vector3(200, 1, 200); ground.transform.position = Vector3.down * .5f;
        manager.whatIsGroundAndObjects = 1 << ground.layer; MapInfo.mapCenter = Vector3.zero; MapInfo.mapSize = new Vector3(100, 20, 100); Physics.SyncTransforms();
        var map = ScriptableObject.CreateInstance<MapData>(); var mapSetter = typeof(MapController).GetProperty("currentMap").GetSetMethod(true); mapSetter.Invoke(null, new object[] { map });
        try {
            Require(effect.frogQuest1 && effect.frogQuest2 && effect.frogQuest3, "original egg prefabs");
            typeof(EffectManager).GetMethod("Awake", P).Invoke(effect, null); map.eMap = EMap.Forest;
            string[] names = { "QuestEggGreen(Clone)", "QuestEggBlue(Clone)", "QuestEggRed(Clone)" };
            for (int i = 0; i < 3; i++) {
                MapController.index = i; MapGenerationController.A_GenerationComplete?.Invoke(); var eggs = Eggs();
                Require(eggs.Length == 1 && eggs[0].transform.root.name == names[i], "correct prefab for stage " + i);
                var eggRoot = eggs[0].transform.root;
                Require(Mathf.Abs(eggRoot.position.y) < .05f && Mathf.Abs(eggRoot.position.x) <= 50 && Mathf.Abs(eggRoot.position.z) <= 50, "grounded placement inside map");
                UnityEngine.Object.DestroyImmediate(eggRoot.gameObject);
            }
            MapController.index = 3; MapGenerationController.A_GenerationComplete?.Invoke(); Require(Eggs().Length == 0, "no egg for later stage");
            MapController.index = 0; map.eMap = EMap.Desert; MapGenerationController.A_GenerationComplete?.Invoke(); Require(Eggs().Length == 0, "no Forest egg in Desert");
            typeof(EffectManager).GetMethod("OnDestroy", P).Invoke(effect, null); map.eMap = EMap.Forest; MapGenerationController.A_GenerationComplete?.Invoke(); Require(Eggs().Length == 0, "event cleanup");
            Debug.Log("ForestFrogPlacementPortCheck passed: original egg prefabs, stages, ground, map gating and event cleanup");
        } finally { foreach (var egg in Eggs()) UnityEngine.Object.DestroyImmediate(egg.transform.root.gameObject); mapSetter.Invoke(null, new object[] { null }); EffectManager.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(root); }
    }
}
