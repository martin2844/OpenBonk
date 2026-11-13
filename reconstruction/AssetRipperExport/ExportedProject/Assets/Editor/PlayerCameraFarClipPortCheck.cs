using System;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerCameraFarClipPortCheck
{
    public static void Run()
    {
        var stageField = typeof(MapController).GetField("<currentStage>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var start = typeof(PlayerCamera).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        if (stageField == null || start == null) throw new Exception("camera or stage binding absent");
        var oldStage = stageField.GetValue(null);
        try
        {
            foreach (string scene in new[] { "GeneratedMap", "FinalBossMap" })
            {
                EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + scene + ".unity");
                var cameras = UnityEngine.Object.FindObjectsOfType<PlayerCamera>(true);
                if (cameras.Length != 1 || cameras[0].camera == null) throw new Exception(scene + ": player camera binding missing");
                var controller = cameras[0];
                float original = controller.camera.farClipPlane;
                foreach (string name in new[] { "StageForest1", "StageDesert2", "StageDesert3" })
                {
                    var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/MonoBehaviour/" + name + ".asset");
                    if (stage == null) throw new Exception(name + ": missing stage");
                    stageField.SetValue(null, stage);
                    controller.camera.farClipPlane = 100f;
                    start.Invoke(controller, null);
                    if (Mathf.Abs(controller.camera.farClipPlane - stage.farClipPlane) > .01f) throw new Exception(scene + ": incorrect far clip for " + name);
                    Debug.Log("PlayerCameraFarClipPortCheck " + scene + " " + name + " far=" + controller.camera.farClipPlane);
                }
                stageField.SetValue(null, null);
                controller.camera.farClipPlane = 123f;
                start.Invoke(controller, null);
                if (Mathf.Abs(controller.camera.farClipPlane - 123f) > .01f) throw new Exception("missing-stage guard failed");
                controller.camera.farClipPlane = original;
            }
        }
        finally { stageField.SetValue(null, oldStage); }
    }
}
