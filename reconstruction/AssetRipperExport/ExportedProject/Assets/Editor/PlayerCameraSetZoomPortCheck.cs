using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEngine;

public static class PlayerCameraSetZoomPortCheck
{
    public static void Run()
    {
        var instance = typeof(SaveManager).GetProperty("Instance").GetSetMethod(true);
        var prior = SaveManager.Instance;
        var root = new GameObject("camera zoom fixture"); root.SetActive(false);
        var saves = root.AddComponent<SaveManager>(); saves.config = new ConfigSaveFile();
        var playerCamera = root.AddComponent<PlayerCamera>();
        var defaultZ = typeof(PlayerCamera).GetField("defaultZ", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            instance.Invoke(null, new object[] { saves });
            defaultZ.SetValue(playerCamera, 9.5f);
            saves.config.cfVideoSettings.camera_distance = 2.5f;
            playerCamera.SetZoom(-99f);
            if (Mathf.Abs(playerCamera.offset3rdPerson.z - 12f) > .0001f) throw new Exception("saved zoom not applied");
            saves.config.cfVideoSettings.camera_distance = -2f;
            playerCamera.SetZoom(99f);
            if (Mathf.Abs(playerCamera.offset3rdPerson.z - 7.5f) > .0001f) throw new Exception("updated saved zoom not applied");
            instance.Invoke(null, new object[] { null });
            playerCamera.SetZoom(0f);
            if (Mathf.Abs(playerCamera.offset3rdPerson.z - 7.5f) > .0001f) throw new Exception("missing-save guard changed zoom");
            Debug.Log("PlayerCameraSetZoomPortCheck passed: saved camera distance, ignored ratio and missing-save guard");
        }
        finally { instance.Invoke(null, new object[] { prior }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
