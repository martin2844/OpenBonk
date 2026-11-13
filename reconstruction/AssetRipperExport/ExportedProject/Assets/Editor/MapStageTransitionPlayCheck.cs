using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MapStageTransitionPlayCheck
{
    const string Pending = "MapStageTransitionPlayCheck.pending";
    static MapStageTransitionPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("stage transition runner").AddComponent<MapStageTransitionRunner>().Begin();
        }
    }
}

public class MapStageTransitionRunner : MonoBehaviour
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception("MapStageTransition: " + message); }
    public void Begin() { try { Run(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } }
    void Run()
    {
        var transition = new GameObject("transition").AddComponent<TransitionUI>();
        TransitionUI.Instance = transition;
        var managerRoot = new GameObject("inactive managers"); managerRoot.SetActive(false);
        var manager = managerRoot.AddComponent<GameManager>(); GameManager.Instance = manager;
        var player = managerRoot.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var liveInventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var previousInventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory = liveInventory;
        var stage1 = ScriptableObject.CreateInstance<StageData>();
        var stage2 = ScriptableObject.CreateInstance<StageData>();
        var map = ScriptableObject.CreateInstance<MapData>(); map.stages = new[] { stage1, stage2 };
        MapController.runConfig = new RunConfig { mapData = map, stageData = stage1 };
        MapController.index = 0;
        var inventoryField = typeof(MapController).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Static);
        var resettingField = typeof(MapController).GetField("reseting", BindingFlags.NonPublic | BindingFlags.Static);
        inventoryField.SetValue(null, previousInventory); resettingField.SetValue(null, false);
        int start = 0, mapStart = 0;
        Action onStart = () => start++; Action onMapStart = () => mapStart++;
        TransitionUI.A_transitionStart += onStart; TransitionUI.A_MapTransitionStart += onMapStart;
        MapController.LoadNextStage();
        Check(MapController.index == 1 && MapController.currentStage == stage2, "advance to next stage");
        Check(ReferenceEquals(inventoryField.GetValue(null), liveInventory), "live inventory retained");
        Check(start == 1 && mapStart == 1 && transition.isTransitioning, "actual transition and events start");
        transition.StopAllCoroutines(); transition.StopTransition();
        MapController.index = -1; inventoryField.SetValue(null, previousInventory); resettingField.SetValue(null, true);
        MapController.LoadNextStage();
        Check(MapController.index == 0 && MapController.currentStage == stage1, "restart selects first stage");
        Check(ReferenceEquals(inventoryField.GetValue(null), previousInventory), "restart does not copy old live player inventory");
        Check(!(bool)resettingField.GetValue(null) && start == 2 && mapStart == 2, "restart flag clears and transition dispatches");
        transition.StopAllCoroutines(); transition.StopTransition();
        TransitionUI.A_transitionStart -= onStart; TransitionUI.A_MapTransitionStart -= onMapStart;
        Debug.Log("MapStageTransitionPlayCheck passed: live inventory, restart guard, stage selection and real transition events");
        EditorApplication.Exit(0);
    }
}
