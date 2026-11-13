using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class QuestWindowPlayCheck {
    const string Pending="QuestWindowPlayCheck.pending", Path="Assets/QuestWindowCheckTemporary.unity";
    static QuestWindowPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);
        var window=UnityEngine.Object.FindAnyObjectByType<QuestsWindowNew>(FindObjectsInactive.Include);
        if(window==null || window.questPrefab==null || window.tabPrefab==null || window.tabNavigation==null)throw new Exception("Original quest window bindings missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var clone=UnityEngine.Object.Instantiate(window.gameObject);clone.SetActive(false);SceneManager.MoveGameObjectToScene(clone,scene);
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("runner").AddComponent<QuestWindowRunner>().Begin();}}
}
public class QuestWindowRunner:MonoBehaviour {
    static void Check(bool ok,string message){if(!ok)throw new Exception("QuestWindowPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        var state=new GameObject("fixture state");state.SetActive(false);var save=state.AddComponent<SaveManager>();save.progression=new ProgressionSaveFile();save.stats=new StatsSaveFile();save.config=new ConfigSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});var data=state.AddComponent<DataManager>();DataManager.Instance=data;
        var character=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_athena.asset");var weapon=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_revolver.asset");var general=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_refresh.asset");
        var gun=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");var refresh=AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Refresh.asset");
        Check(character!=null && weapon!=null && general!=null && gun!=null && refresh!=null,"original quest assets");weapon.SetUnlockable(gun);general.SetUnlockable(refresh);
        data.unsortedAchievements=new List<MyAchievement>{character,weapon,general}; MyStats.SetValueForce("kills",3750); save.progression.achievements.Add(weapon.internalName);
        var events=new GameObject("events").AddComponent<EventSystem>();var window=UnityEngine.Object.FindAnyObjectByType<QuestsWindowNew>(FindObjectsInactive.Include);
        Check(!save.progression.menuMeta.hasVisitedQuests,"quests initially unvisited");
        window.gameObject.SetActive(true);yield return null;
        Check(save.progression.menuMeta.hasVisitedQuests,"opening original quests window records visit");
        Check(window.tabNavigation.GetNumButtons()==3 && window.tabNavigation.current==0,"original quest tabs initialized");
        Check(window.questPrefab.GetComponent<MyButtonQuest>().achievement==character,"default character quest");
        window.tabNavigation.ButtonPressed(1,true);yield return null;
        var quest=window.questPrefab.GetComponent<MyButtonQuest>();Check(quest.achievement==weapon && quest.claimOverlay.activeSelf,"weapon tab shows completed unclaimed quest");
        var footer=window.GetComponentInChildren<QuestsFooter>(true);Check(footer!=null,"original footer");events.SetSelectedGameObject(quest.gameObject);
        Check(footer.t_progress.text.Contains("3,750") && footer.t_unlock.text.Contains("Weapon"),"selected quest updates details");
        yield return new WaitForSeconds(.2f);quest.GetButton().onClick.Invoke();
        Check(save.progression.claimedAchievements.Contains(weapon.internalName) && save.progression.silver==weapon.GetSilverReward(),"bound quest claim");
        window.tabNavigation.ButtonPressed(2,true);yield return null;Check(window.questPrefab.GetComponent<MyButtonQuest>().achievement==general,"general tab updates quest list");
        window.tabNavigation.ButtonPressed(1,true);yield return null;Check(!quest.claimOverlay.activeSelf && quest.achievement==weapon,"claimed state persists across tab switch");
        window.gameObject.SetActive(false);yield return null;window.gameObject.SetActive(true);yield return null;
        Check(!quest.claimOverlay.activeSelf && quest.achievement==weapon && save.progression.silver==weapon.GetSilverReward(),"claimed state survives closing and reopening");
        string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"megabonk-claim-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(dir);
        try {
            string path=System.IO.Path.Combine(dir,"progression.json");
            typeof(SaveManager).GetMethod("SaveTemp",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(save,new object[]{Newtonsoft.Json.JsonConvert.SerializeObject(save.progression),path,true});
            var restored=Newtonsoft.Json.JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(System.IO.File.ReadAllText(path)));
            Check(restored.claimedAchievements.Contains(weapon.internalName) && restored.silver==weapon.GetSilverReward(),"claimed reward survives encrypted save round trip");
        } finally { System.IO.Directory.Delete(dir,true); }
        Debug.Log("QuestWindowPlayCheck PASSED: original full window, tabs, detail selection and claim");EditorApplication.Exit(0);
    }
}
