using System;
using System.Reflection;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RunConfigUiPortCheck
{
    static TextMeshProUGUI Text(string name) => new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
    public static void Run()
    {
        var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        if (map == null) throw new Exception("Forest map asset missing");
        var oldConfig = MapController.runConfig;
        MapController.runConfig = new RunConfig { mapData = map, mapTierIndex = 1 };
        var obj = new GameObject("run config");
        var ui = obj.AddComponent<RunConfigUi>();
        ui.t_mapName = Text("name"); ui.t_mapTier = Text("tier"); ui.t_silverMultiplier = Text("silver");
        ui.mapOutline = new GameObject("outline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        ui.challenge = new GameObject("challenge"); ui.challenge.SetActive(false);
        typeof(RunConfigUi).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
        if (ui.t_mapName.text != map.GetName()) throw new Exception("map name");
        if (ui.t_mapName.color != MyColorUtility.GetTierColor(1) || ui.t_mapTier.color != MyColorUtility.GetTierColor(1)) throw new Exception("tier color");
        if (!ui.t_silverMultiplier.text.Contains("1.1") || ui.challenge.activeSelf) throw new Exception("silver or absent challenge");
        Debug.Log("RUN_CONFIG_UI_PORT_CHECK_OK");
        MapController.runConfig = oldConfig;
        UnityEngine.Object.DestroyImmediate(ui.t_mapName.gameObject); UnityEngine.Object.DestroyImmediate(ui.t_mapTier.gameObject);
        UnityEngine.Object.DestroyImmediate(ui.t_silverMultiplier.gameObject); UnityEngine.Object.DestroyImmediate(ui.mapOutline.gameObject);
        UnityEngine.Object.DestroyImmediate(ui.challenge); UnityEngine.Object.DestroyImmediate(obj);
    }
}
