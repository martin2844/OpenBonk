using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class ShopQuestChainPlayCheck {
    const string Pending="ShopQuestChainPlayCheck.pending"; const string Path="Assets/ShopQuestChainTemporary.unity";
    static ShopQuestChainPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() {
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);
        var original=UnityEngine.Object.FindAnyObjectByType<ShopWindow>(FindObjectsInactive.Include);
        if (original==null || original.shopContainerPrefab==null) throw new Exception("original shop missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var shop=UnityEngine.Object.Instantiate(original.gameObject); shop.SetActive(false); SceneManager.MoveGameObjectToScene(shop,scene);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source,true); EditorSceneManager.SaveScene(scene,Path);
        SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s) { if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("runner").AddComponent<ShopQuestChainRunner>().Begin(); } }
}
public class ShopQuestChainRunner:MonoBehaviour {
    static void Check(bool value,string msg) { if(!value) throw new Exception("ShopQuestChainPlayCheck: "+msg); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test=Run(); while(true) { bool more; try { more=test.MoveNext(); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if(!more) yield break; yield return test.Current; } }
    IEnumerator Run() {
        var state=new GameObject("synthetic game state"); state.SetActive(false);
        var save=state.AddComponent<SaveManager>(); save.progression=new ProgressionSaveFile(); save.stats=new StatsSaveFile(); save.progression.silver=1000;
        foreach(EShopItem item in Enum.GetValues(typeof(EShopItem))) save.progression.shopItems[item]=0;
        for(int i=0;i<19;i++) save.progression.achievements.Add("fixture_previous_"+i);
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var data=state.AddComponent<DataManager>(); DataManager.Instance=data;
        var shopItem=AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/Refresh.asset");
        var killQuest=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_revolver.asset");
        var refreshQuest=shopItem.unlockRequirement;
        Check(killQuest!=null && killQuest.statName=="kills" && killQuest.targetValue==7500 && refreshQuest.targetValue==20,"original quest thresholds");
        refreshQuest.SetUnlockable(shopItem); data.unsortedAchievements=new List<MyAchievement>{killQuest,refreshQuest}; data.shopItems.Add(shopItem.eShopItem,shopItem);
        var lookup=(Dictionary<string,MyAchievement>)typeof(DataManager).GetField("achievementsData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data);
        lookup.Add(killQuest.internalName,killQuest); lookup.Add(refreshQuest.internalName,refreshQuest);
        MyStats.SetValueForce("kills",7499); MyStats.SetValueForce("questsCompleted",19);
        SaveManager.loaded=true; MyAchievements.Init(); TrackStats.Init();
        var window=UnityEngine.Object.FindAnyObjectByType<ShopWindow>(FindObjectsInactive.Include); window.gameObject.SetActive(true); yield return null;
        var card=window.shopContainerPrefab.GetComponent<ShopContainer>(); var button=card.GetComponentInChildren<MyButtonShop>(true); var events=new GameObject("event system").AddComponent<EventSystem>();
        button.OnSelect(new BaseEventData(events)); Check(window.shopFooter.requirementsContainer.gameObject.activeSelf,"shop starts locked");
        var enemyGo=new GameObject("synthetic enemy"); enemyGo.SetActive(false); var enemy=enemyGo.AddComponent<Enemy>();
        var enemyData=ScriptableObject.CreateInstance<EnemyData>(); enemyData.enemyName=Actors.Enemies.EEnemy.Goblin;
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy,new object[]{enemyData});
        var movement=enemyGo.AddComponent<EnemyMovementRb>(); movement.rb=enemyGo.AddComponent<Rigidbody>(); enemy.enemyMovement=movement;
        var dissolve=enemyGo.AddComponent<EnemyDissolve>(); dissolve.enemyRenderer=enemyGo.AddComponent<MeshRenderer>(); dissolve.Reset(); enemy.dissolve=dissolve;
        enemy.EnemyDied(null); Check(MyStats.GetStat("kills")==7500,"enemy death increments real kill statistic");
        yield return new WaitForSecondsRealtime(1.2f); MyAchievements.Update();
        Check(save.progression.achievements.Contains(killQuest.internalName) && MyStats.GetStat("questsCompleted")==20,"kill quest raises completed quest count");
        yield return new WaitForSecondsRealtime(1.2f); MyAchievements.Update();
        Check(save.progression.achievements.Contains(refreshQuest.internalName) && save.progression.newShopItems.Contains(shopItem.GetInternalName()),"quest count unlocks and flags refresh");
        card.Set(shopItem); button.OnSelect(new BaseEventData(events)); Check(window.shopFooter.buy.activeSelf && !window.shopFooter.requirementsContainer.gameObject.activeSelf,"original shop reflects unlock");
        yield return new WaitForSeconds(.2f); int price=shopItem.GetPrice(); window.btnBuy.GetButton().onClick.Invoke();
        Check(shopItem.GetLevel()==1 && save.progression.silver==1000-price,"original buy button purchases earned upgrade");
        string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"megabonk-quest-chain-"+Guid.NewGuid().ToString("N")); System.IO.Directory.CreateDirectory(dir);
        try {
            string path=System.IO.Path.Combine(dir,"progression.json");
            typeof(SaveManager).GetMethod("SaveTemp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(save,new object[]{JsonConvert.SerializeObject(save.progression),path,true});
            var restored=JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(System.IO.File.ReadAllText(path)));
            Check(restored.achievements.Contains(killQuest.internalName) && restored.achievements.Contains(refreshQuest.internalName) && restored.shopItems[EShopItem.Refresh]==1 && restored.silver==1000-price,"encrypted save round trip");
        } finally { System.IO.Directory.Delete(dir,true); }
        SaveManager.loaded=false; MyAchievements.Cleanup(); TrackStats.Cleanup();
        Debug.Log("ShopQuestChainPlayCheck PASSED: enemy death, two achievements, original shop purchase and encrypted save"); EditorApplication.Exit(0);
    }
}
