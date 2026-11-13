using System;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using UnityEditor;
using UnityEngine;

public static class EnemyDisplayPortCheck
{
    public static void Run()
    {
        var oldManager = DataManager.Instance;
        var oldSelection = MyButtonLog.A_EnemySelected;
        var managerObject = new GameObject("display data"); managerObject.SetActive(false);
        var displayObject = new GameObject("enemy display"); displayObject.SetActive(false);
        var modelObject = new GameObject("enemy model"); modelObject.SetActive(false);
        var cameraObject = new GameObject("display camera");
        try
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Miniboss_Pig.asset");
            if (data == null || data.animation == null || data.material == null) throw new Exception("Original enemy data missing");
            DataManager.Instance = managerObject.AddComponent<DataManager>();
            typeof(DataManager).GetField("enemyData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(DataManager.Instance, new Dictionary<EEnemy, EnemyData> { [data.enemyName] = data });
            var filter = modelObject.AddComponent<MeshFilter>();
            var renderer = modelObject.AddComponent<MeshRenderer>();
            var animated = modelObject.AddComponent<AnimatedMesh>();
            typeof(AnimatedMesh).GetField("Filter", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animated, filter);
            var camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 60f;
            var display = displayObject.AddComponent<EnemyDisplay>(); display.animatedMesh = animated; display.meshRenderer = renderer; display.camera = camera;
            typeof(EnemyDisplay).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(display, null);
            MyButtonLog.A_EnemySelected(data.enemyName);
            if (!modelObject.activeSelf || renderer.sharedMaterial != data.material || filter.sharedMesh != data.animation.Animations[0].Meshes[0]) throw new Exception("Selection did not load original animation and material");
            if (modelObject.layer != LayerMask.NameToLayer("UiCamera")) throw new Exception("Preview model not on UiCamera layer");
            var bounds = renderer.bounds; float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = largest / (2f * Mathf.Tan(30f * Mathf.Deg2Rad)) + largest * .5f;
            Vector3 expected = bounds.center - camera.transform.forward * distance;
            if (Vector3.Distance(camera.transform.position, expected) > .001f) throw new Exception("Camera framing differs from original bounds calculation");
            display.SetEnemy((EEnemy)int.MaxValue);
            if (modelObject.activeSelf) throw new Exception("Unknown enemy should hide preview model");
            typeof(EnemyDisplay).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(display, null);
            if (MyButtonLog.A_EnemySelected != oldSelection) throw new Exception("Selection handler was not removed");
            Debug.Log("EnemyDisplayPortCheck passed");
        }
        finally
        {
            DataManager.Instance = oldManager; MyButtonLog.A_EnemySelected = oldSelection;
            UnityEngine.Object.DestroyImmediate(displayObject); UnityEngine.Object.DestroyImmediate(modelObject);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
