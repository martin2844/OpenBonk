using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CrosshairPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool b, string s) { if (!b) throw new Exception("CrosshairPortCheck: " + s); }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity", OpenSceneMode.Single);
        var crosshair = UnityEngine.Object.FindAnyObjectByType<CrosshairUi>(FindObjectsInactive.Include);
        Check(crosshair != null && crosshair.crosshairOuter != null && crosshair.crosshairInner != null && crosshair.parentCrosshair != null && crosshair.group != null, "original scene bindings");
        var root = new GameObject("crosshair test state"); root.SetActive(false);
        var oldPlayer = MyPlayer.Instance; var oldSave = SaveManager.Instance;
        try
        {
            var save = root.AddComponent<SaveManager>(); save.config = new ConfigSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerInput = root.AddComponent<PlayerInput>();
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.weaponInventory = new WeaponInventory();
            var aiming = typeof(PlayerInput).GetField("aiming", Private); var aimable = typeof(WeaponInventory).GetField("hasAimableWeapon", Private);
            save.config.cfGameSettings.crosshair_alpha = .4f; save.config.cfGameSettings.crosshair_size = 1.5f; save.config.cfGameSettings.crosshair_height = 0f;
            Call(crosshair, "Start"); Check(Mathf.Approximately(crosshair.group.alpha, .4f) && crosshair.parentCrosshair.localScale == Vector3.one * 1.5f, "alpha and size settings");
            Vector3 low = CrosshairUi.GetCrosshairRaycastPosition(); save.config.cfGameSettings.crosshair_height = 1f; Vector3 high = CrosshairUi.GetCrosshairUiPosition();
            Check(Mathf.Approximately(low.x, Screen.width * .5f) && Mathf.Approximately(low.y, Screen.height * .525f) && Mathf.Approximately(high.y, Screen.height * .75f), "screen position bounds");
            aiming.SetValue(player.playerInput, true); Call(crosshair, "CheckVisible"); Check(!(bool)typeof(CrosshairUi).GetField("isVisible", Private).GetValue(crosshair), "aim without aimable weapon");
            aimable.SetValue(player.inventory.weaponInventory, true); Call(crosshair, "CheckVisible"); Check(crosshair.crosshairInner.gameObject.activeSelf && crosshair.crosshairOuter.gameObject.activeSelf, "aimable and aiming shows crosshair");
            aiming.SetValue(player.playerInput, false); Call(crosshair, "CheckVisible"); Check(!crosshair.crosshairInner.gameObject.activeSelf && !crosshair.crosshairOuter.gameObject.activeSelf, "release hides crosshair");
            save.config.cfGameSettings.crosshair_alpha = .7f; save.config.cfGameSettings.crosshair_size = .8f; typeof(CrosshairUi).GetMethod("OnSettingUpdated", Private).Invoke(crosshair, new object[] { "crosshair_alpha", null, null }); typeof(CrosshairUi).GetMethod("OnSettingUpdated", Private).Invoke(crosshair, new object[] { "crosshair_size", null, null });
            Check(Mathf.Approximately(crosshair.group.alpha, .7f) && crosshair.parentCrosshair.localScale == Vector3.one * .8f, "live settings update");
            Debug.Log("CrosshairPortCheck passed: original scene bindings, aim visibility, position and live settings");
        }
        finally { MyPlayer.Instance = oldPlayer; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave }); UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.CloseScene(scene, true); }
    }
}
