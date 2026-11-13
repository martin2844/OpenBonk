using System;
using System.Collections.Generic;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSettingsTypes;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs.ConfigSettingsTypes;
using UnityEngine;

public static class SettingsMetadataPortCheck
{
    static void Require(bool value, string why) { if (!value) throw new Exception("SettingsMetadataPortCheck: " + why); }
    static void Headers(CFSettings settings, params string[] expected)
    {
        List<SettingHeader> list = settings.GetHeaders(); Require(list.Count == expected.Length, "header count");
        for (int i = 0; i < list.Count; i++) Require(list[i].index + ":" + list[i].header == expected[i], "header order and value");
    }
    public static void Run()
    {
        var game = new CFGameSettings(); var video = new CFVideoSettings(); var controls = new CFControlSettings();
        Headers(game, "0:GAME", "4:UI", "9:OTHER", "25:ADVANCED");
        Headers(video, "0:SCREEN", "7:GRAPHICS");
        Headers(controls, "0:GENERAL", "9:KEYBOARD INPUT", "19:CONTROLLER INPUT", "28:EXTRA", "31:ADVANCED");
        Headers(new CFAudioSettings(), "0:AUDIO"); Headers(new CFVisualsSettings(), "0:VISUALS", "6:ADVANCED"); Headers(new CFOtherSettings(), "0:SAVES");
        Require(ConfigSettingsUtility.GetSettingType(typeof(CFGameSettings).GetField("camera_shake")) == SettingType.Enum, "integer setting");
        Require(ConfigSettingsUtility.GetSettingType(typeof(CFVideoSettings).GetField("fov")) == SettingType.Slider, "float setting");
        Require(ConfigSettingsUtility.GetSettingType(typeof(CFVideoSettings).GetField("resolution")) == SettingType.Resolution, "resolution setting");
        Require(ConfigSettingsUtility.GetSettingType(typeof(CFGameSettings).GetField("language")) == SettingType.Language, "language setting");
        Require(ConfigSettingsUtility.GetSettingType(typeof(CFControlSettings).GetField("controller")) == SettingType.ControllerDisplay, "controller display");
        Require(ConfigSettingsUtility.GetSettingType(typeof(CFControlSettings).GetField("keyboard_jump")) == SettingType.Control, "control binding");
        int scanned = 0, nativeBindings = 0;
        foreach (var category in new CFSettings[] { game, video, controls, new CFAudioSettings(), new CFVisualsSettings(), new CFOtherSettings() })
            foreach (var field in category.GetType().GetFields())
            {
                var kind = ConfigSettingsUtility.GetSettingType(field);
                Require(kind != SettingType.ControlNew, category.GetType().Name + "." + field.Name + " would use unreconstructed Rewired control row");
                if (category == controls && field.Name.StartsWith("controller_") && field.FieldType == typeof(string))
                {
                    Require(kind == SettingType.Control, field.Name + " must use native control row");
                    nativeBindings++;
                }
                scanned++;
            }
        Require(nativeBindings == 7, "all controller action rows use native control prefab");
        Debug.Log($"SettingsMetadataPortCheck: {scanned} fields, {nativeBindings} native controller binding rows, no ControlNew rows");
        var obj = new GameObject("settings metadata test"); var current = obj.AddComponent<CurrentSettings>(); string changed = null; object old = null, next = null;
        Action<string,object,object> listener = (name, oldValue, value) => { changed = name; old = oldValue; next = value; };
        CurrentSettings.A_SettingUpdated += listener;
        try
        {
            current.BetterUpdateCfSettings("fov", 90f, video);
            Require(video.fov == 90f && changed == "fov" && (float)old == 85f && (float)next == 90f, "setting mutation and old/new notification");
            current.BetterUpdateCfSettings("camera_shake", 0, game);
            Require(game.camera_shake == 0 && (int)old == 1 && (int)next == 0, "integer mutation");
            Debug.Log("SettingsMetadataPortCheck passed: configured headers, field types, mutation and notifications");
        }
        finally { CurrentSettings.A_SettingUpdated -= listener; UnityEngine.Object.DestroyImmediate(obj); }
    }
}
