using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class AndroidMenuPerformanceCheck
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        var menu = UnityEngine.Object.FindObjectOfType<MainMenu>();
        if (menu == null || menu.menuCamera == null || menu.menuCamera.camera == null)
            throw new Exception("Main menu camera is missing");
        var volume = menu.menuCamera.camera.GetComponent<PostProcessVolume>();
        if (volume == null || volume.sharedProfile == null ||
            !volume.sharedProfile.TryGetSettings(out DepthOfField shared) || !shared.active || !shared.enabled.value)
            throw new Exception("Expected enabled menu depth of field is missing");
        var camera = menu.menuCamera.camera;
        var layer = camera.GetComponent<PostProcessLayer>();
        if (layer == null || !layer.enabled) throw new Exception("Expected enabled menu post processing is missing");
        var method = typeof(MainMenu).Assembly.GetType("AndroidPerformanceSettings")?.GetMethod(
            "ConfigureMenuCamera", BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null) throw new Exception("Could not find menu configuration");
        method.Invoke(null, new object[] { camera });
        if (layer.enabled || camera.allowHDR || camera.allowMSAA)
            throw new Exception("The menu camera still uses full-screen effects or extra buffers");
        if (!shared.enabled.value) throw new Exception("The shared profile was changed");
        method.Invoke(null, new object[] { null });
        Debug.Log("AndroidMenuPerformanceCheck PASS");
    }
}
