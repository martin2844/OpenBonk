using System;
using System.Reflection;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.Utility;
using UnityEngine;

public static class PauseMenuPortCheck
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Invoke(PauseUi menu, string method)
    {
        typeof(PauseUi).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
    }

    public static void Run()
    {
        var managerObject = new GameObject("PauseCheckGameManager");
        var manager = managerObject.AddComponent<GameManager>();
        GameManager.Instance = manager;
        typeof(GameManager).GetField("isPlaying", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, true);
        var uiObject = new GameObject("PauseCheckUiManager");
        var ui = uiObject.AddComponent<UiManager>();
        UiManager.Instance = ui;
        ui.encounterWindows = uiObject.AddComponent<EncounterWindows>();
        var root = new GameObject("PauseCheckMenu");
        root.SetActive(false);
        var menu = root.AddComponent<PauseUi>();
        menu.main = new GameObject("Main");
        menu.options = new GameObject("Options");
        menu.map = new GameObject("Map");
        menu.main.transform.SetParent(root.transform);
        menu.options.transform.SetParent(root.transform);
        menu.map.transform.SetParent(root.transform);
        menu.options.SetActive(false);
        menu.map.SetActive(false);
        menu.inventory = new GameObject("Inventory").AddComponent<UpgradeInventoryUI>();
        menu.inventory.transform.SetParent(root.transform);
        var originalMode = Physics.simulationMode;
        bool originalPaused = MyTime.paused;
        try
        {
            MyTime.paused = false;
            Invoke(menu, "Awake");
            Require(menu.CanPause(), "eligible run cannot pause");
            menu.Pause();
            Require(menu.IsPaused() && menu.main.activeInHierarchy, "pause menu did not open");
            Invoke(menu, "OnEnable");
            Require(MyTime.paused && Physics.simulationMode == SimulationMode.Script, "game did not pause");
            menu.GoToOptions();
            Require(menu.options.activeSelf && !menu.main.activeSelf, "options navigation failed");
            menu.GoToMap();
            Require(menu.map.activeSelf && !menu.options.activeSelf, "map navigation failed");
            menu.GoToMain();
            menu.Toggle();
            Require(!menu.IsPaused(), "menu did not close");
            Invoke(menu, "OnDisable");
            Require(!MyTime.paused && Physics.simulationMode == SimulationMode.FixedUpdate, "game did not resume");
            MyTime.paused = true;
            Invoke(menu, "OnEnable");
            Invoke(menu, "OnDisable");
            Require(MyTime.paused, "previous pause state was lost");
            manager.cutscene = true;
            Require(!menu.CanPause(), "pause was allowed during cutscene");
            Debug.Log("PauseMenuPortCheck passed: eligibility, panels, pause, resume, and prior pause state");
        }
        finally
        {
            MyTime.paused = originalPaused;
            Physics.simulationMode = originalMode;
            GameManager.Instance = null;
            UiManager.Instance = null;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(uiObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }
}
