using System;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEngine;

public static class ObjectStageMaterialPortCheck
{
    public static void Run()
    {
        int oldIndex = MapController.index;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Landmark3_0.prefab");
        if (prefab == null) throw new Exception("Original landmark missing");
        var instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            var component = instance.GetComponent<ObjectStageMaterial>();
            if (component == null || component.meshRenderer == null || component.stageMaterials == null || component.stageMaterials.Length == 0)
                throw new Exception("Landmark material references missing");
            var expected = component.stageMaterials[0];
            if (expected.material == null) throw new Exception("Stage material missing");
            var initial = component.meshRenderer.sharedMaterial;
            var start = typeof(ObjectStageMaterial).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            MapController.index = expected.stageIndex + 10;
            start.Invoke(component, null);
            if (component.meshRenderer.sharedMaterial != initial) throw new Exception("Unmatched stage changed material");
            MapController.index = expected.stageIndex;
            start.Invoke(component, null);
            if (component.meshRenderer.sharedMaterial != expected.material) throw new Exception("Matching stage did not apply original material");
            var renderer = component.meshRenderer; component.meshRenderer = null;
            typeof(ObjectStageMaterial).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
            if (component.meshRenderer != renderer) throw new Exception("Renderer validation lookup failed");
            Debug.Log("ObjectStageMaterialPortCheck passed: original landmark stage material, unmatched stage and renderer lookup");
        }
        finally { MapController.index = oldIndex; UnityEngine.Object.DestroyImmediate(instance); }
    }
}
