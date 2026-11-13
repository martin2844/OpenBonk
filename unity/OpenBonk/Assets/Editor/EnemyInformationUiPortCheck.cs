using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class EnemyInformationUiPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string why) { if (!ok) throw new Exception("EnemyInformationUiPortCheck: " + why); }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    static RawImage Image(Transform parent, string name, float width = 100) { var g = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); g.transform.SetParent(parent, false); var r = g.GetComponent<RawImage>(); r.rectTransform.sizeDelta = new Vector2(width, 11); return r; }
    static TextMeshProUGUI Text(Transform parent) { var g = new GameObject("text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); g.transform.SetParent(parent, false); return g.GetComponent<TextMeshProUGUI>(); }
    static ScalingEntry Entry(Transform parent) { var e = new GameObject("entry").AddComponent<ScalingEntry>(); e.transform.SetParent(parent, false); e.t_text = Text(e.transform); e.parentSquare = Image(e.transform, "parent", 120); e.timeSquare = Image(e.transform, "time"); e.stageSquare = Image(e.transform, "stage"); e.finalSwarmSquare = Image(e.transform, "swarm"); return e; }
    public static void Run()
    {
        var state = new GameObject("enemy info state"); state.SetActive(false);
        var game = state.AddComponent<GameManager>(); GameManager.Instance = game; Set(game, "isPlaying", true);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> { [EStat.Difficulty] = 0f, [EStat.EnemyScalingMultiplier] = 1f, [EStat.EnemyAmountMultiplier] = 1f }; player.inventory.playerStats = stats;
        var manager = state.AddComponent<EnemyManager>(); EnemyManager.Instance = manager; manager.numEnemies = 55;
        manager.summonerController = (SummonerController)FormatterServices.GetUninitializedObject(typeof(SummonerController));
        manager.summonerController.stageSummoner = (StageSummoner)FormatterServices.GetUninitializedObject(typeof(StageSummoner));
        var map = ScriptableObject.CreateInstance<MapData>(); map.stageDuration = 600f;
        typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map }); MapController.index = 1;
        var save = state.AddComponent<SaveManager>(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save }); save.config = new ConfigSaveFile();
        var root = new GameObject("enemy info root"); var ui = root.AddComponent<EnemyInformationUi>();
        var panel = new GameObject("panel"); panel.transform.SetParent(root.transform); ui.parent = panel;
        ui.t_enemies = Text(panel.transform); ui.i_enemies = Image(panel.transform, "enemy fill");
        ui.scalingEntryHp = Entry(panel.transform); ui.scalingEntryDmg = Entry(panel.transform); ui.scalingEntrySpeed = Entry(panel.transform);
        try
        {
            var row = ui.scalingEntryHp; row.Set("test", 1, 2, 3);
            Check(row.t_text.text == "test" && Mathf.Abs(row.timeSquare.rectTransform.sizeDelta.x - 20) < .001f && Mathf.Abs(row.stageSquare.rectTransform.sizeDelta.x - 40) < .001f && Mathf.Abs(row.finalSwarmSquare.rectTransform.sizeDelta.x - 60) < .001f, "segment proportions");
            Check(Mathf.Abs(row.stageSquare.rectTransform.anchoredPosition.x - 20) < .001f && Mathf.Abs(row.finalSwarmSquare.rectTransform.anchoredPosition.x - 60) < .001f, "segment placement");
            Call(ui, "Awake"); Call(ui, "Start"); Check(!panel.activeSelf, "hidden by default");
            save.config.cfGameSettings.debug_enemy_scaling = 1; CurrentSettings.A_SettingUpdated?.Invoke("debug_enemy_scaling", 0, 1); Check(panel.activeSelf, "setting enables display");
            MyTime.time = 10f; MyTime.stageTimer = 300f; MyTime.difficultyTimer = 120f; MyTime.finalSwarmTimer = 60f;
            int target = manager.summonerController.stageSummoner.GetNumTargetEnemies(); Call(ui, "FixedUpdate");
            Check(ui.t_enemies.text == string.Format("{0} / {1}", 55, target), "enemy count and target");
            Check(Mathf.Abs(ui.i_enemies.transform.localScale.x - Mathf.Clamp01(55f / target)) < .001f, "enemy fill");
            float hp = CombatScaling.GetHpMultiplierAddition(out var time, out var swarm, out var stage);
            Check(ui.scalingEntryHp.t_text.text == string.Format("{0:N1}x", hp), "HP scaling value");
            Check(Mathf.Abs(ui.scalingEntryHp.timeSquare.rectTransform.sizeDelta.x - 120f * time / (time + stage + swarm)) < .001f, "actual HP segment");
            manager.numEnemies = 10; Call(ui, "FixedUpdate"); Check(ui.t_enemies.text.StartsWith("55 /"), "half second throttle");
            MyTime.time += .5f; Call(ui, "FixedUpdate"); Check(ui.t_enemies.text.StartsWith("10 /"), "throttle expires");
            save.config.cfGameSettings.debug_enemy_scaling = 0; CurrentSettings.A_SettingUpdated?.Invoke("debug_enemy_scaling", 1, 0); Check(!panel.activeSelf, "setting hides display");
            Call(ui, "OnDestroy"); save.config.cfGameSettings.debug_enemy_scaling = 1; CurrentSettings.A_SettingUpdated?.Invoke("debug_enemy_scaling", 0, 1); Check(!panel.activeSelf, "listener cleanup");
            Debug.Log("EnemyInformationUiPortCheck passed: setting, timing, counts and three-part scaling");
        }
        finally { Call(ui, "OnDestroy"); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null }); GameManager.Instance = null; MyPlayer.Instance = null; EnemyManager.Instance = null; typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { null }); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(state); }
    }
}
