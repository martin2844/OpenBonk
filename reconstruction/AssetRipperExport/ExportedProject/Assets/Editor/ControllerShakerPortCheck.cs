using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEngine;

public static class ControllerShakerPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var saveObject = new GameObject("vibration save");
        var save = saveObject.AddComponent<SaveManager>();
        save.Init(); save.config = new ConfigSaveFile();
        var owner = new GameObject("vibration listener");
        var shaker = owner.AddComponent<ControllerShaker>();
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        var shakeField = typeof(ControllerShaker).GetField("TestShake", flags);
        var stopField = typeof(ControllerShaker).GetField("TestStop", flags);
        int calls = 0, lastMotor = -1, stops = 0; float lastIntensity = 0, lastDuration = 0;
        shakeField.SetValue(null, new Action<int, float, float>((m, i, d) => { calls++; lastMotor = m; lastIntensity = i; lastDuration = d; }));
        stopField.SetValue(null, new Action(() => stops++));
        typeof(ControllerShaker).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shaker, null);
        try
        {
            var hp = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); hp.hp = 10;
            PlayerHealth.A_TakeDamage(hp, null, false);
            Require(calls == 1 && lastMotor == 2 && Mathf.Approximately(lastIntensity, .2f) && Mathf.Approximately(lastDuration, .15f), "living damage");
            hp.hp = 0; PlayerHealth.A_TakeDamage(hp, null, true);
            Require(calls == 2 && lastMotor == 1 && Mathf.Approximately(lastIntensity, .8f) && Mathf.Approximately(lastDuration, 1f), "fatal damage");
            PlayerMovement.A_Landed(25f); Require(calls == 2, "landing threshold");
            PlayerMovement.A_Landed(25.01f); Require(calls == 3 && lastMotor == 1 && Mathf.Approximately(lastIntensity, .08f) && Mathf.Approximately(lastDuration, .15f), "hard landing");
            save.config.cfControlSettings.controller_vibration = 0f;
            ControllerShaker.Shake(0, 1f, 1f); ControllerShaker.StopShakes(); Require(calls == 3 && stops == 0, "disabled setting");
            save.config.cfControlSettings.controller_vibration = .5f;
            ControllerShaker.Shake(0, .4f, .2f); ControllerShaker.StopShakes(); Require(calls == 4 && Mathf.Approximately(lastIntensity, .4f) && stops == 1, "positive setting gates without scaling");
            typeof(ControllerShaker).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(shaker, null);
            PlayerMovement.A_Landed?.Invoke(30f); Require(calls == 4, "event cleanup");
            Debug.Log("ControllerShakerPortCheck passed");
        }
        finally { shakeField.SetValue(null, null); stopField.SetValue(null, null); UnityEngine.Object.DestroyImmediate(owner); UnityEngine.Object.DestroyImmediate(saveObject); }
    }
}
