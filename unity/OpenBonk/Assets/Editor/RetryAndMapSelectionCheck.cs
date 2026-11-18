using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.UI.DynamicWindows;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RetryAndMapSelectionCheck
{
    private const string Pending = "RetryAndMapSelectionCheck.pending";

    static RetryAndMapSelectionCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Retry test runner").AddComponent<RetryAndMapSelectionRunner>().Begin();
    }
}

public sealed class RetryAndMapSelectionRunner : MonoBehaviour
{
    public void Begin() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        yield return null;
        try
        {
            CheckMapSelection();
            CheckRetry();
            Debug.Log("RetryAndMapSelectionCheck passed: saved map and tier, locked-map fallback, retry on lethal damage, resume, decline and forced death");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void CheckMapSelection()
    {
        var state = new GameObject("Map selection fixture");
        state.SetActive(false);
        var save = state.AddComponent<SaveManager>();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        save.progression = new ProgressionSaveFile();
        var forest = NewMap(EMap.Forest, 1);
        var desert = NewMap(EMap.Desert, 3);
        var graveyard = NewMap(EMap.Graveyard, 1);
        var meta = save.progression.menuMeta;
        meta.SetTierCompletion(EMap.Desert, 0);
        meta.RememberRun(desert, 1);
        meta.RememberRun(graveyard, 0);
        save.progression.menuMeta = JsonConvert.DeserializeObject<MenuMeta>(JsonConvert.SerializeObject(meta));

        var selection = state.AddComponent<MapSelectionUi>();
        selection.defaultMapData = forest;
        selection.mapSelectionGroup = state.AddComponent<SelectionGroupToggleSingle>();
        AddMapButton(state.transform, forest);
        var desertButton = AddMapButton(state.transform, desert);
        var graveyardButton = AddMapButton(state.transform, graveyard);
        selection.Init();
        Require(selection.runConfig.mapData == graveyard && selection.runConfig.stageData == graveyard.stages[0] &&
            graveyardButton.isSelected && selection.startBtn == graveyardButton, "the new menu selects and focuses Graveyard");

        graveyardButton.CanSelect(false);
        selection.runConfig.mapData = null;
        selection.Init();
        Require(selection.runConfig.mapData == forest, "a locked saved map falls back to Forest");
        save.progression.menuMeta.RememberRun(desert, 1);
        selection.Init();
        Require(selection.runConfig.mapData == desert && selection.runConfig.stageData == desert.stages[1] &&
            desertButton.isSelected, "the saved Desert tier is restored");

        var custom = ScriptableObject.CreateInstance<OpenBonkExternalMapData>();
        custom.eMap = (EMap)256;
        custom.externalId = "example:crypt";
        custom.stages = forest.stages;
        save.progression.menuMeta.RememberRun(custom, 0);
        save.progression.menuMeta = JsonConvert.DeserializeObject<MenuMeta>(JsonConvert.SerializeObject(save.progression.menuMeta));
        selection.Init();
        Require(selection.runConfig.mapData == desert, "an unavailable modded map falls back to the saved built-in map");
        var customButton = AddMapButton(state.transform, custom);
        selection.Init();
        Require(selection.runConfig.mapData == custom && customButton.isSelected, "a modded map is restored by stable package ID");
    }

    private static void CheckRetry()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.SelectedLocale = Locale.CreateLocale("en");
        var fixture = new GameObject("Retry fixture");
        fixture.SetActive(false);
        var player = fixture.AddComponent<MyPlayer>();
        MyPlayer.Instance = player;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));
        player.inventory = inventory;
        inventory.playerHealth = health;
        inventory.playerXp = new PlayerXp { level = 7 };
        health.hp = health.maxHp = 100;
        health.maxShield = 30;
        player.transform.position = new Vector3(3f, 4f, 5f);
        var ui = fixture.AddComponent<AlwaysUi>();
        AlwaysUi.Instance = ui;
        ui.dynamicWindows = fixture.AddComponent<DynamicWindows>();
        ui.dynamicWindows.contentParent = new GameObject("Prompt content").transform;
        ui.dynamicWindows.dynamicWindowPromptPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DynamicWindowPrompt.prefab");
        new GameObject("Event system").AddComponent<EventSystem>();
        var game = new GameObject("Test game manager").AddComponent<GameManager>();
        game.StartPlaying();
        MyTime.Unpause();
        MyTime.runTimer = 125f;
        MyTime.stageTimer = 45f;
        int deaths = 0;
        int gameOvers = 0;
        PlayerHealth.A_Died += () => deaths++;
        GameManager.A_GameOver += () => gameOvers++;
        OpenBonkOptionalUiPolicy.Apply("retry_on_death", true);

        health.PlayerDied();
        health.PlayerDied();
        var prompt = ui.dynamicWindows.contentParent.GetComponentInChildren<DWindowPrompt>();
        Require(prompt != null && ui.dynamicWindows.contentParent.childCount == 1 &&
            health.IsDead() && MyTime.paused && !game.isGameOver && deaths == 0 && gameOvers == 0, "lethal damage pauses and offers one retry");
        var retry = FindChoice(prompt, "Retry");
        Require(prompt.GetComponent<Window>().startBtn.GetButton() == retry && retry.navigation.selectOnRight == FindChoice(prompt, "End run"),
            "focus starts on Retry and the controller can navigate to End run");
        retry.onClick.Invoke();
        Require(health.hp == health.maxHp && health.shield == health.maxShield && health.DamageCooldown() && !MyTime.paused &&
            !game.cutscene && !game.isGameOver && inventory == player.inventory && inventory.playerXp.level == 7 &&
            player.transform.position == new Vector3(3f, 4f, 5f) && MyTime.runTimer == 125f && MyTime.stageTimer == 45f &&
            deaths == 0 && gameOvers == 0, "retry restores health in the unchanged run without reporting a defeat");

        health.PlayerDied();
        prompt = ui.dynamicWindows.contentParent.GetComponentInChildren<DWindowPrompt>();
        var endRun = FindChoice(prompt, "End run");
        endRun.onClick.Invoke();
        endRun.onClick.Invoke();
        Require(game.isGameOver && health.IsDead() && deaths == 1 && gameOvers == 1, "ending the run reports a single defeat");

        DestroyImmediate(game.gameObject);
        game = new GameObject("Forced-death game manager").AddComponent<GameManager>();
        game.StartPlaying();
        MyTime.Unpause();
        health.hp = health.maxHp;
        health.KillPlayer();
        Require(game.isGameOver && deaths == 2 && gameOvers == 2 &&
            ui.dynamicWindows.contentParent.GetComponentInChildren<DWindowPrompt>() == null, "forced deaths do not offer retry");
    }

    private static MapData NewMap(EMap id, int tiers)
    {
        var map = ScriptableObject.CreateInstance<MapData>();
        map.eMap = id;
        map.stages = new StageData[tiers];
        for (int i = 0; i < tiers; i++) map.stages[i] = ScriptableObject.CreateInstance<StageData>();
        return map;
    }

    private static SelectionGroupToggleSingleButton AddMapButton(Transform parent, MapData map)
    {
        var entry = new GameObject(map.eMap.ToString(), typeof(RectTransform), typeof(Button), typeof(MapEntry), typeof(SelectionGroupToggleSingleButton));
        entry.transform.SetParent(parent);
        var button = entry.GetComponent<SelectionGroupToggleSingleButton>();
        var mapEntry = entry.GetComponent<MapEntry>();
        mapEntry.buttonScript = button;
        mapEntry.Set(map);
        return button;
    }

    private static Button FindChoice(DWindowPrompt prompt, string text)
    {
        foreach (var button in prompt.GetComponentsInChildren<Button>())
            if (button.GetComponentInChildren<TextMeshProUGUI>()?.text == text) return button;
        throw new Exception("Retry prompt button missing: " + text);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("RetryAndMapSelectionCheck: " + message);
    }
}
