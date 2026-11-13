using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FinalExitPortalPlayCheck
{
    const string Pending = "FinalExitPortalPlayCheck.pending";
    const string TestScene = "Assets/FinalExitPortalCheckTemporary.unity";
    static FinalExitPortalPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        Scene source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/FinalBossMap.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<InteractablePortalFinal>(FindObjectsInactive.Include);
        var cinematic = UnityEngine.Object.FindAnyObjectByType<FianlBossCinematic>(FindObjectsInactive.Include);
        if (original == null || original.cameraCircling == null || cinematic == null || cinematic.finalPortal != original.gameObject)
            throw new Exception("FinalExitPortalPlayCheck: original scene portal/cinematic bindings missing");
        Scene test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var portal = UnityEngine.Object.Instantiate(original.gameObject); portal.name = "original final exit";
        var camera = UnityEngine.Object.Instantiate(original.cameraCircling); camera.name = "original victory camera";
        var intro = UnityEngine.Object.Instantiate(cinematic.gameObject); intro.name = "original final cinematic";
        SceneManager.MoveGameObjectToScene(portal, test); SceneManager.MoveGameObjectToScene(camera, test); SceneManager.MoveGameObjectToScene(intro, test);
        portal.GetComponent<InteractablePortalFinal>().cameraCircling = camera; portal.SetActive(false); camera.SetActive(false);
        var introComponent = intro.GetComponent<FianlBossCinematic>(); introComponent.enabled = false; introComponent.finalPortal = portal;
        EditorSceneManager.SetActiveScene(test); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(test, TestScene);
        SessionState.SetBool(Pending,true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("exit test runner").AddComponent<FinalExitPortalRunner>().Begin(); } }
}
public class FinalExitPortalRunner : MonoBehaviour
{
    static void Check(bool ok,string msg) { if (!ok) throw new Exception("FinalExitPortalPlayCheck: " + msg); }
    public void Begin() => StartCoroutine(Guarded());
    IEnumerator Guarded() {
        var test = Run();
        while (true) { bool more; try { more = test.MoveNext(); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!more) yield break; yield return test.Current; }
    }
    IEnumerator Run()
    {
        var state = new GameObject("inactive state"); state.SetActive(false);
        var playerGo = new GameObject("player"); var transitionGo = new GameObject("transition");
        Action onGameOver = null; Action<bool> onBossDefeated = null; FinalFightController fight = null;
        try {
            var map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset"); Check(map != null,"original Forest map");
            typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null,new object[]{map}); MapController.runConfig = new RunConfig { mapData = map, mapTierIndex = 1 };
            typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null,new object[]{map.stages[0]});
            var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile(); save.stats = new StatsSaveFile(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game;
            var player = playerGo.AddComponent<MyPlayer>(); player.enabled = false; MyPlayer.Instance = player; player.character = ECharacter.Fox;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.statusEffects = new PlayerStatusEffects(); player.inventory.playerXp = new PlayerXp { xp = 50 }; player.inventory.weaponInventory = new WeaponInventory(); player.teleportEffect = new GameObject("teleport effect"); player.teleportEffect.SetActive(false);
            var pcGo = new GameObject("player camera"); pcGo.transform.SetParent(state.transform); var pc = pcGo.AddComponent<PlayerCamera>(); PlayerCamera.Instance = pc;
            var deathCam = new GameObject("death camera"); deathCam.transform.SetParent(state.transform); pc.deathCamera = deathCam.AddComponent<Camera>();
            var ui = state.AddComponent<UiManager>(); UiManager.Instance = ui; ui.hud = new GameObject("hud");
            var death = state.AddComponent<DeathScreen>(); ui.deathScreen = death; death.leaderboardsWindow = new GameObject("leaderboard"); death.statsWindow = new GameObject("stats"); death.statsWindow.SetActive(false); death.canvasGroup = new GameObject("canvas group").AddComponent<CanvasGroup>(); death.b_continue = new GameObject("continue").AddComponent<UiAnimation>();
            var transition = transitionGo.AddComponent<TransitionUI>(); TransitionUI.Instance = transition;
            var portal = UnityEngine.Object.FindAnyObjectByType<InteractablePortalFinal>(FindObjectsInactive.Include); Check(portal != null && portal.cameraCircling != null,"original scene portal and camera");
            var portalGo = portal.gameObject; Check(!portalGo.activeSelf && portalGo.CompareTag("Interactable"),"original portal starts inactive and interactable");
            save.progression.Init(); ChallengesTracker.Init(); RunStats.AddValue(EMyStat.kills,42); int gameOvers = 0; onGameOver = () => gameOvers++; GameManager.A_GameOver += onGameOver;
            var intro = UnityEngine.Object.FindAnyObjectByType<FianlBossCinematic>(FindObjectsInactive.Include); Check(intro != null && intro.finalPortal == portalGo,"original cinematic targets portal");
            onBossDefeated = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), intro, typeof(FianlBossCinematic).GetMethod("OnStageBossDied",BindingFlags.NonPublic | BindingFlags.Instance)); FinalFightController.A_BossDefeated += onBossDefeated;
            fight = state.AddComponent<FinalFightController>(); var bossGo = new GameObject("released boss"); bossGo.SetActive(false); fight.boss = bossGo.AddComponent<Enemy>();
            typeof(FinalFightController).GetMethod("Awake",BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fight,null);
            Enemy.A_EnemyReleasedFromPool?.Invoke(fight.boss); Check(!portalGo.activeSelf && fight.isBossDefeated,"boss release schedules the exit portal");
            Time.timeScale = 10f; yield return new WaitForSeconds(10.2f); Time.timeScale = 1f;
            Check(portalGo.activeSelf,"cinematic opens the original exit after the boss release");
            Check(portal.Interact(),"first interaction succeeds"); Check(!portal.Interact(),"second interaction rejected");
            Check(portalGo.CompareTag("Untagged") && game.cutscene && game.isGameOver && player.isTeleporting && gameOvers == 1,"teleport, game-over event and interaction gate");
            yield return new WaitForSeconds(2.4f);
            var progress = save.progression.menuMeta.GetMapProgress(map.eMap);
            Check(MyTime.paused && progress.GetNumTierRuns(1) == 2 && progress.IsTierCompleted(1) && progress.GetTierCompletionCharacters(1).Contains(ECharacter.Fox),$"pause and victory progression: paused={MyTime.paused}, runs={progress.GetNumTierRuns(1)}, completed={progress.IsTierCompleted(1)}, fox={progress.GetTierCompletionCharacters(1).Contains(ECharacter.Fox)}, timeScale={Time.timeScale}");
            Check(save.progression.GetCharacterProgression(ECharacter.Fox).xp == 5 && save.progression.GetCharacterProgression(ECharacter.Fox).numRuns == 1,"game-over subscriber records character progression");
            Check(progress.tierHighscores.TryGetValue(progress.lastSelectTier,out float kills) && kills == 42f,"kill record");
            string tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "megabonk-final-exit-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDirectory);
            try {
                string path = System.IO.Path.Combine(tempDirectory,"progression.json");
                typeof(SaveManager).GetMethod("SaveTemp",BindingFlags.NonPublic | BindingFlags.Instance).Invoke(save,new object[]{JsonConvert.SerializeObject(save.progression),path,true});
                string encrypted = System.IO.File.ReadAllText(path); Check(!encrypted.Contains("completedTiers"),"save file encrypted");
                var reloaded = JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(encrypted)); var restored = reloaded.menuMeta.GetMapProgress(map.eMap);
                Check(restored.IsTierCompleted(1) && restored.GetNumTierRuns(1) == 2 && restored.GetTierCompletionCharacters(1).Contains(ECharacter.Fox) && reloaded.GetCharacterProgression(ECharacter.Fox).xp == 5,"encrypted victory record survives save/reload");
            } finally { System.IO.Directory.Delete(tempDirectory,true); }
            yield return new WaitForSecondsRealtime(1.2f);
            Check(!pcGo.activeSelf && portal.cameraCircling.activeSelf && !ui.hud.activeSelf,"transition swaps cameras and hides HUD");
            yield return new WaitForSecondsRealtime(2f);
            Check(death.statsWindow.activeSelf && !death.b_continue.gameObject.activeSelf,"final statistics shown");
            Debug.Log("FinalExitPortalPlayCheck passed: boss release opens original portal; game-over, encrypted victory save/reload, transition and statistics"); EditorApplication.Exit(0);
        }
        finally { Time.timeScale = 1f; if (onBossDefeated != null) FinalFightController.A_BossDefeated -= onBossDefeated; if (fight != null) typeof(FinalFightController).GetMethod("OnDestroy",BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fight,null); if (onGameOver != null) GameManager.A_GameOver -= onGameOver; ChallengesTracker.Cleanup(); if (SaveManager.Instance != null && SaveManager.Instance.progression != null) SaveManager.Instance.progression.OnDestroy(); MyTime.Unpause(); }
    }
}
