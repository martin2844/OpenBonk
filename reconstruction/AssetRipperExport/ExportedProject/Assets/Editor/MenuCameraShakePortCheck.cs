using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MilkShake;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MenuCameraShakePortCheck
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var camera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CameraMovement>(true)).Single();
        if (!camera.isActiveAndEnabled || camera.shaker == null || camera.preset == null)
            throw new Exception("Menu camera or its original shake references are unavailable");

        var field = typeof(Shaker).GetField("activeShakes", BindingFlags.Instance | BindingFlags.NonPublic);
        var shakes = (List<ShakeInstance>)field.GetValue(camera.shaker);
        var before = shakes.Count;
        typeof(CameraMovement).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(camera, null);
        if (shakes.Count != before + 1 || shakes[before].ShakeParameters.ShakeType != camera.preset.ShakeType ||
            shakes[before].ShakeParameters.Strength != camera.preset.Strength)
            throw new Exception("Menu camera did not start its serialized shake preset");

        Debug.Log($"MenuCameraShakePortCheck passed: preset={camera.preset.name}, type={camera.preset.ShakeType}, strength={camera.preset.Strength}");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
