using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts._Data;
using Assets.Scripts._Data.Tomes;
using Inventory__Items__Pickups.Xp_and_Levels;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Newtonsoft.Json;

[InitializeOnLoad]
public static class DeathQuestFlowCheck {
    const string Pending="DeathQuestFlowCheck.pending", Path="Assets/DeathQuestFlowCheckTemporary.unity";
    static DeathQuestFlowCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);
        var screen=UnityEngine.Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
        var quests=UnityEngine.Object.FindAnyObjectByType<QuestsCompletedUi>(FindObjectsInactive.Include);
        if(screen==null || quests==null || screen.statsWindow==null || !quests.transform.IsChildOf(screen.statsWindow.transform) || !screen.statsWindow.transform.IsChildOf(screen.transform)) throw new Exception("unexpected game-over hierarchy");
        if(screen.b_continue==null || screen.b_continue.GetComponent<Button>()==null) throw new Exception("continue button missing");
        Debug.Log("DeathQuestFlowCheck source hierarchy "+screen.name+" / "+screen.statsWindow.name+" / "+quests.name);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var clone=UnityEngine.Object.Instantiate(screen.gameObject);clone.SetActive(false);SceneManager.MoveGameObjectToScene(clone,scene);
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("runner").AddComponent<DeathQuestFlowRunner>().Begin();}}
}
public class DeathQuestFlowRunner:MonoBehaviour {
    string tempDirectory,oldSavePath;bool oldLoaded,oldTestSaves;ProgressionSaveFile testProgression;
    void Cleanup(){
        if(testProgression!=null)testProgression.OnDestroy();ChallengesTracker.Cleanup();
        if(tempDirectory!=null){SaveManager.loaded=oldLoaded;typeof(SaveManager).GetField("defaultSavesPath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,oldSavePath);typeof(SaveManager).GetField("TEST_SAVES",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,oldTestSaves);if(System.IO.Directory.Exists(tempDirectory))System.IO.Directory.Delete(tempDirectory,true);tempDirectory=null;}
    }
    sealed class Provider : ITableProvider {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable {
            var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);
        }
    }
    string unexpectedError;
    void Capture(string condition,string stack,LogType type){if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) unexpectedError=condition+"\n"+stack;}
    static void Check(bool ok,string message){if(!ok)throw new Exception("DeathQuestFlowCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Cleanup();Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");LocalizationSettings.StringDatabase.TableProvider=new Provider();
        Application.logMessageReceived+=Capture;new GameObject("test audio listener").AddComponent<AudioListener>();
        var state=new GameObject("synthetic progression");state.SetActive(false);
        var save=state.AddComponent<SaveManager>();save.progression=new ProgressionSaveFile();save.stats=new StatsSaveFile();typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        oldLoaded=SaveManager.loaded;var savePathField=typeof(SaveManager).GetField("defaultSavesPath",BindingFlags.Static|BindingFlags.NonPublic);oldSavePath=(string)savePathField.GetValue(null);var testSavesField=typeof(SaveManager).GetField("TEST_SAVES",BindingFlags.Static|BindingFlags.NonPublic);oldTestSaves=(bool)testSavesField.GetValue(null);
        tempDirectory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"megabonk-death-save-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory,"CloudDir","69"));savePathField.SetValue(null,tempDirectory);testSavesField.SetValue(null,false);SaveManager.loaded=true;
        save.stats.Init();save.progression.Init();testProgression=save.progression;ChallengesTracker.Init();
        var map=AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null,new object[]{map});MapController.runConfig=new RunConfig{mapData=map,mapTierIndex=1};
        var data=state.AddComponent<DataManager>();DataManager.Instance=data;TrackStats.Init();
        var quest=AssetDatabase.LoadAssetAtPath<MyAchievement>("Assets/MonoBehaviour/a_revolver.asset");var weapon=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset");Check(quest!=null && weapon!=null,"original assets");quest.SetUnlockable(weapon);
        var lookup=(Dictionary<string,MyAchievement>)typeof(DataManager).GetField("achievementsData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data);lookup.Add(quest.internalName,quest);
        Check(MyAchievements.TryUnlock(quest.internalName) && RunStats.achievements.Count==1,"real achievement event records quest");
        var managerObject=new GameObject("test game manager");managerObject.SetActive(false);var game=managerObject.AddComponent<GameManager>();game.enabled=false;GameManager.Instance=null;
        var player=state.AddComponent<MyPlayer>();MyPlayer.Instance=player;player.playerRenderer=state.AddComponent<PlayerRenderer>();player.playerMovement=state.AddComponent<PlayerMovement>();
        player.character=ECharacter.Fox;player.inventory=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));player.inventory.characterData=Instantiate(AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"));player.inventory.characterData.localizedName=new LocalizedString("Characters","FOX_NAME");player.inventory.playerXp=new PlayerXp{level=7,xp=50};player.inventory.itemInventory=new ItemInventory();player.inventory.weaponInventory=new WeaponInventory();player.inventory.tomeInventory=new TomeInventory();
        var shownWeapon=Instantiate(weapon);shownWeapon.localizedName=new LocalizedString();shownWeapon.localizedDescription=new LocalizedString();
        var ownedWeapon=(WeaponBase)FormatterServices.GetUninitializedObject(typeof(WeaponBase));ownedWeapon.weaponData=shownWeapon;ownedWeapon.level=4;ownedWeapon.Enable();player.inventory.weaponInventory.weapons.Add(shownWeapon.eWeapon,ownedWeapon);
        var tome=Instantiate(AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeDamage.asset"));tome.localizedName=new LocalizedString();player.inventory.tomeInventory.tomeLevels.Add(tome.eTome,3);
        ((Dictionary<ETome,TomeData>)typeof(DataManager).GetField("tomeData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data)).Add(tome.eTome,tome);
        var item=Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Beacon.asset"));item.localizedName=new LocalizedString("ItemsBlue","Beacon_NAME");item.localizedDescription=new LocalizedString("ItemsBlue","Beacon_DESC");item.GetDummyItem();
        ((Dictionary<EItem,ItemData>)typeof(DataManager).GetField("itemData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data)).Add(item.eItem,item);
        player.inventory.itemInventory.items.Add(item.eItem,new ItemBeacon(player.inventory.itemInventory){amount=2});
        RunStats.AddValue(EMyStat.kills,12345);RunStats.AddValue(EMyStat.goldEarned,67);RunStats.AddValue(EMyStat.silverEarned,890);MyTime.runTimer=125f;
        var cameraState=new GameObject("synthetic player camera");cameraState.SetActive(false);var playerCamera=cameraState.AddComponent<PlayerCamera>();playerCamera.deathCamera=new GameObject("death camera").AddComponent<Camera>();PlayerCamera.Instance=playerCamera;
        var screen=UnityEngine.Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);var ui=screen.GetComponentInChildren<QuestsCompletedUi>(true);Check(ui!=null,"original quests child");
        var uiManager=state.AddComponent<UiManager>();UiManager.Instance=uiManager;uiManager.deathScreen=screen;managerObject.SetActive(true);Check(GameManager.Instance==game,"game manager subscribes at startup");
        player.inventory.statusEffects=new PlayerStatusEffects();
        var playerStats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));playerStats.stats=new Dictionary<EStat,float>();foreach(EStat stat in Enum.GetValues(typeof(EStat)))playerStats.stats[stat]=1f;
        playerStats.stats[EStat.MaxHealth]=100;playerStats.stats[EStat.Armor]=0;playerStats.stats[EStat.DamageReductionMultiplier]=0;playerStats.stats[EStat.Shield]=0;player.inventory.playerStats=playerStats;
        player.inventory.playerHealth=new PlayerHealth(playerStats);
        var originalStats=screen.GetComponentInChildren<DeathStatsUi>(true);int weaponBefore=originalStats.weaponsParent.childCount,tomeBefore=originalStats.tomesParent.childCount,itemBefore=originalStats.itemsParent.childCount;
        int before=ui.contentParent.childCount;screen.statsWindow.SetActive(false);screen.b_continue.gameObject.SetActive(false);screen.gameObject.SetActive(true);Time.timeScale=2f;
        int gameOvers=0;Action onGameOver=()=>gameOvers++;GameManager.A_GameOver+=onGameOver;
        player.inventory.playerHealth.DamagePlayerExternal(200,0,Vector3.back,false,"test",DcFlags.BypassEvade);player.inventory.playerHealth.KillPlayer();Check(player.inventory.playerHealth.IsDead() && game.isGameOver && gameOvers==1 && RunStats.GetStat(EMyStat.runs)==1,"lethal damage triggers one game-over and run statistic");
        yield return new WaitForSeconds(5.8f);Check(game.cutscene && MyTime.paused && playerCamera.deathCamera.gameObject.activeSelf,"death animation pauses the run and enables death camera");
        string progressionPath=(string)typeof(SaveManager).GetMethod("GetProgressionPath",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(save,null);string statsPath=(string)typeof(SaveManager).GetMethod("GetStatsPath",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(save,null);
        Check(System.IO.File.Exists(progressionPath) && System.IO.File.Exists(statsPath),"death coroutine writes progression and stats");
        var loadedProgress=JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(System.IO.File.ReadAllText(progressionPath)));var loadedStats=JsonConvert.DeserializeObject<StatsSaveFile>(SimpleEncryptor.Decrypt(System.IO.File.ReadAllText(statsPath)));
        Check(loadedProgress.GetCharacterProgression(ECharacter.Fox).xp==5 && loadedProgress.GetCharacterProgression(ECharacter.Fox).numRuns==1 && loadedProgress.menuMeta.GetMapProgress(map.eMap).GetNumTierRuns(1)==1 && loadedProgress.achievements.Contains(quest.internalName),"encrypted save contains character, map run and quest progression");
        Check(loadedStats.stats["runs"].value==1,"encrypted stats contain recorded death");
        Check(screen.b_continue.gameObject.activeInHierarchy,"continue appears after death transition");
        var button=screen.b_continue.GetComponent<Button>();Check(button.onClick.GetPersistentEventCount()>0,"original continue binding exists");button.onClick.Invoke();
        Check(screen.statsWindow.activeInHierarchy && !screen.b_continue.gameObject.activeSelf,"bound continue opens stats");
        yield return new WaitForSeconds(1f);Check(ui.contentParent.childCount==before+1,"quest card appears through stats screen");
        var entry=ui.contentParent.GetChild(before).GetComponent<QuestsCompletedEntry>();Check(entry!=null && entry.icon.texture==quest.GetIcon(),"original quest card and icon");
        var stats=screen.GetComponentInChildren<DeathStatsUi>(true);Check(stats!=null,"original stats panel");
        string kills=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_RoundOver_en.asset").GetEntry("SUMMARY_KILLS").LocalizedValue;
        string time=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_RoundOver_en.asset").GetEntry("SUMMARY_TIME").LocalizedValue;
        Check(stats.t_stats.text==string.Format("{0}: {1:N0}\n{2}: 2:05\nLVL 7\n",kills,12345,time),"localized kills, time and level: "+stats.t_stats.text);
        Check(stats.t_gold.text.Contains("+67 ") && stats.t_silver.text.Contains("+890 ") && stats.t_unlocks.text.Contains(" 1 "),"gold, silver and unlocked quest count");
        Check(stats.t_characterName.text==player.inventory.characterData.GetName() && stats.i_character.texture==player.inventory.characterData.GetIcon(),"localized character name and icon");
        Check(stats.weaponsParent.childCount==weaponBefore+1 && stats.tomesParent.childCount==tomeBefore+1 && stats.itemsParent.childCount==itemBefore+1 && stats.tomesParent.gameObject.activeSelf,"one card per owned weapon, tome and item");
        var weaponCard=stats.weaponsParent.GetChild(weaponBefore).GetComponent<InventoryItemPrefabUI>();var tomeCard=stats.tomesParent.GetChild(tomeBefore).GetComponent<InventoryItemPrefabUI>();var itemCard=stats.itemsParent.GetChild(itemBefore).GetComponent<InventoryItemPrefabUI>();
        Check(weaponCard.icon.texture==shownWeapon.GetIcon() && weaponCard.t_level.text=="4" && tomeCard.icon.texture==tome.GetIcon() && tomeCard.t_level.text=="3" && itemCard.icon.texture==item.GetIcon() && itemCard.t_level.text=="2","inventory cards show original icons and owned levels");
        player.inventory.weaponInventory.weapons.Clear();player.inventory.tomeInventory.tomeLevels.Clear();player.inventory.itemInventory.items.Clear();typeof(DeathStatsUi).GetMethod("ShowInventory",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(stats,null);Check(!stats.tomesParent.gameObject.activeSelf,"empty tome row is hidden");
        RunStats.achievements.Clear();typeof(DeathStatsUi).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(stats,null);Check(stats.t_unlocks.text=="","no unlocks hides counter");
        Check(unexpectedError==null,"unexpected Unity error: "+unexpectedError);player.inventory.playerHealth.OnDestroy();GameManager.A_GameOver-=onGameOver;Application.logMessageReceived-=Capture;TrackStats.Cleanup();Cleanup();Debug.Log("DeathQuestFlowCheck PASSED: lethal damage, game over, encrypted saves, camera transition, original continue, stats, inventory and recorded quest card");EditorApplication.Exit(0);
    }
}
