using System;
using System.Collections;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class QuestFooterPlayCheck {
    const string Pending="QuestFooterPlayCheck.pending", Path="Assets/QuestFooterCheckTemporary.unity";
    static QuestFooterPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);
        var window=UnityEngine.Object.FindAnyObjectByType<QuestsWindowNew>(FindObjectsInactive.Include);
        var footer=UnityEngine.Object.FindAnyObjectByType<QuestsFooter>(FindObjectsInactive.Include);
        if(window==null || window.questPrefab==null || footer==null)throw new Exception("Original quest scene bindings missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var button=UnityEngine.Object.Instantiate(window.questPrefab); button.name="original quest button";button.SetActive(false);SceneManager.MoveGameObjectToScene(button,scene);
        var details=UnityEngine.Object.Instantiate(footer.gameObject);details.name="original quest footer";details.SetActive(false);SceneManager.MoveGameObjectToScene(details,scene);
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("runner").AddComponent<QuestFooterRunner>().Begin();}}
}
public class QuestFooterRunner:MonoBehaviour {
    static void Check(bool ok,string message){if(!ok)throw new Exception("QuestFooterPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        var state=new GameObject("fixture state");state.SetActive(false);var save=state.AddComponent<SaveManager>();save.progression=new ProgressionSaveFile();save.stats=new StatsSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var ach=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_revolver.asset"); var weapon=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");
        Check(ach!=null && weapon!=null && ach.targetValue==7500,"original kill quest and weapon"); ach.SetUnlockable(weapon); MyStats.SetValueForce("kills",3750);
        var footer=UnityEngine.Object.FindAnyObjectByType<QuestsFooter>(FindObjectsInactive.Include);var quest=UnityEngine.Object.FindAnyObjectByType<MyButtonQuest>(FindObjectsInactive.Include);
        Check(footer!=null && quest!=null && footer.achievementContainer!=null && footer.i_progressBar!=null,"quest UI references");
        footer.gameObject.SetActive(true);quest.gameObject.SetActive(true);yield return null;
        quest.Set(ach);var events=new GameObject("events").AddComponent<EventSystem>(); events.SetSelectedGameObject(quest.gameObject);
        Check(footer.t_progress.text.Contains("3,750") && footer.t_progress.text.Contains("7,500") && Mathf.Approximately(footer.i_progressBar.transform.localScale.x,.5f),"tracked quest progress");
        Check(!footer.checkMark.activeSelf && footer.t_unlock.text.Contains("Weapon") && footer.t_reward.text.Contains(ach.GetSilverReward().ToString()),"locked quest details");
        save.progression.achievements.Add(ach.internalName);MyStats.SetValueForce("kills",7500);quest.Set(ach);events.SetSelectedGameObject(null);events.SetSelectedGameObject(quest.gameObject);
        Check(footer.checkMark.activeSelf && footer.achievementContainer.isUnlocked && quest.claimOverlay.activeSelf,"completed quest state");
        yield return new WaitForSeconds(.15f); quest.GetButton().onClick.Invoke();
        Check(save.progression.claimedAchievements.Contains(ach.internalName) && save.progression.silver==ach.GetSilverReward() && !quest.claimOverlay.activeSelf,"bound claim awards silver");
        quest.GetButton().onClick.Invoke();Check(save.progression.silver==ach.GetSilverReward(),"claim cannot award twice");
        Debug.Log("QuestFooterPlayCheck PASSED: original quest UI, progress, completion and bound claim");EditorApplication.Exit(0);
    }
}
