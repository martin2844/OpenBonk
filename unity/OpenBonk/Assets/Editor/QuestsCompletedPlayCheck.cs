using System;
using System.Collections;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class QuestsCompletedPlayCheck {
    const string Pending="QuestsCompletedPlayCheck.pending", Path="Assets/QuestsCompletedCheckTemporary.unity";
    static QuestsCompletedPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);
        var original=UnityEngine.Object.FindAnyObjectByType<QuestsCompletedUi>(FindObjectsInactive.Include);
        if(original==null || original.prefab==null || original.contentParent==null || original.scrollRect==null)throw new Exception("original completed-quests UI bindings missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var clone=UnityEngine.Object.Instantiate(original.gameObject);clone.SetActive(false);SceneManager.MoveGameObjectToScene(clone,scene);
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("runner").AddComponent<QuestsCompletedRunner>().Begin();}}
}
public class QuestsCompletedRunner:MonoBehaviour {
    static void Check(bool ok,string message){if(!ok)throw new Exception("QuestsCompletedPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        var state=new GameObject("synthetic progression");state.SetActive(false);
        var save=state.AddComponent<SaveManager>();save.progression=new ProgressionSaveFile();save.stats=new StatsSaveFile();typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var data=state.AddComponent<DataManager>();DataManager.Instance=data;
        TrackStats.Init();var first=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_revolver.asset");var second=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_refresh.asset");
        var weapon=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");var shop=AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Refresh.asset");
        Check(first!=null && second!=null && weapon!=null && shop!=null,"original assets");first.SetUnlockable(weapon);second.SetUnlockable(shop);
        var lookup=(Dictionary<string,MyAchievement>)typeof(DataManager).GetField("achievementsData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data);
        lookup.Add(first.internalName,first);lookup.Add(second.internalName,second);
        Check(MyAchievements.TryUnlock(first.internalName) && MyAchievements.TryUnlock(second.internalName) && RunStats.achievements.Count==2,"real achievement events populate run results");
        var ui=UnityEngine.Object.FindAnyObjectByType<QuestsCompletedUi>(FindObjectsInactive.Include);Check(ui!=null,"cloned original UI");int before=ui.contentParent.childCount;
        Time.timeScale=1f;ui.gameObject.SetActive(true);yield return new WaitForSeconds(.15f);Check(ui.contentParent.childCount==before,"initial delay");
        yield return new WaitForSeconds(1.2f);Check(ui.contentParent.childCount==before+2,"two quest cards created");
        var entry0=ui.contentParent.GetChild(before).GetComponent<QuestsCompletedEntry>();var entry1=ui.contentParent.GetChild(before+1).GetComponent<QuestsCompletedEntry>();
        Check(entry0!=null && entry1!=null && entry0.icon.texture==first.GetIcon() && entry1.icon.texture==second.GetIcon(),"original icons and order");
        Check(entry0.t_description.text==first.GetUnlockRequirement() && entry1.t_description.text==second.GetUnlockRequirement() && entry0.t_unlock.text.Contains("Weapon"),"quest text and reward");
        Check(entry0.myButton.gameObject.activeSelf && entry1.myButton.gameObject.activeSelf && Mathf.Approximately(entry0.transform.localScale.x,1f),"entry visibility and completed animation");
        Check(ui.source==null || Mathf.Approximately(ui.source.pitch,1.25f),"original pitch progression");
        TrackStats.Cleanup();Debug.Log("QuestsCompletedPlayCheck PASSED: achievement events, original game-over UI, timed entries, text and pitch");EditorApplication.Exit(0);
    }
}
