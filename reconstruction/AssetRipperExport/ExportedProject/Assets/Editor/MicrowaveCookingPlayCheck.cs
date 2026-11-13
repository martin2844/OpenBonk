using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.InGame.Levelup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[InitializeOnLoad]
public static class MicrowaveCookingPlayCheck {
    const string Pending="MicrowaveCookingPlayCheck.pending",Path="Assets/MicrowaveCookingCheckTemporary.unity";
    static MicrowaveCookingPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);var original=UnityEngine.Object.FindAnyObjectByType<MicrowaveUi>(FindObjectsInactive.Include);if(original==null)throw new Exception("Original microwave UI missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);var uiRoot=UnityEngine.Object.Instantiate(original.gameObject);uiRoot.SetActive(false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(uiRoot,scene);var sourceUi=uiRoot.GetComponent<MicrowaveUi>();if(!sourceUi.itemPrefab.transform.IsChildOf(uiRoot.transform))throw new Exception("Microwave UI prefab reference outside clone");foreach(var script in uiRoot.GetComponentsInChildren<MonoBehaviour>(true))if(script!=sourceUi && !(script is MicrowaveItemButton) && !(script is TMPro.TMP_Text))script.enabled=false;EditorSceneManager.CloseScene(source,true);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Microwave.prefab");if(prefab==null)throw new Exception("Original microwave prefab missing");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);root.SetActive(false);var machine=root.GetComponentInChildren<InteractableMicrowave>(true);if(machine==null || machine.progressBarProgress==null || machine.itemIcon==null || machine.microwaveCenterTransform==null)throw new Exception($"Original microwave references missing: component={machine!=null}, progress={(machine!=null && machine.progressBarProgress!=null)}, icon={(machine!=null && machine.itemIcon!=null)}, center={(machine!=null && machine.microwaveCenterTransform!=null)}");
        foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))if(script!=machine)script.enabled=false;
        EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("microwave cooking runner").AddComponent<MicrowaveCookingRunner>().Begin();}}
}
public class MicrowaveTestItem:ItemBase {
    public MicrowaveTestItem(ItemInventory inventory):base(inventory){} public override void Init(){} public override void Cleanup(){} protected override void OnInitOrAmountChanged(){} public override void Tick(){} public override void PreAttack(DamageContainer dc,StatComponents stat){} public override void ProcOnHitEffects(DamageContainer dc){} public override bool HasOnHitEffectProc()=>false;public override bool HasPreAttackProc()=>false;
}
public class MicrowaveFixtureWindow:BaseEncounterWindow { public bool opened; public override void Open(EEncounter e){opened=e==EEncounter.Microwave;} public override void OnClose(){opened=false;} public override void ChooseOffer(int i){} }
public class MicrowaveCookingRunner:MonoBehaviour {
    sealed class Provider:ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable { var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");return Addressables.ResourceManager.CreateCompletedOperation(table as T,null); } }
    static void Check(bool ok,string message){if(!ok)throw new Exception("MicrowaveCookingPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        var host=new GameObject("fixture managers");host.SetActive(false);GameManager.Instance=host.AddComponent<GameManager>();var player=host.AddComponent<MyPlayer>();MyPlayer.Instance=player;player.inventory=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var inventory=new ItemInventory();player.inventory.itemInventory=inventory;var stats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));stats.stats=new Dictionary<EStat,float>{{EStat.ChestPriceMultiplier,1f}};player.inventory.playerStats=stats;player.inventory.playerHealth=(PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth));player.inventory.playerHealth.hp=100;typeof(PlayerInventory).GetField("<gold>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player.inventory,100f);
        var clover=Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset"));var borgar=Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Borgar.asset"));Check(clover!=null && borgar!=null,"item assets");
        var data=host.AddComponent<DataManager>();DataManager.Instance=data;typeof(DataManager).GetField("itemData",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,new Dictionary<EItem,ItemData>{{clover.eItem,clover},{borgar.eItem,borgar}});
        inventory.items.Add(clover.eItem,new MicrowaveTestItem(inventory){amount=1});inventory.items.Add(borgar.eItem,new MicrowaveTestItem(inventory){amount=2});
        var machine=FindAnyObjectByType<InteractableMicrowave>(FindObjectsInactive.Include);Check(machine!=null,"original microwave");machine.transform.root.gameObject.SetActive(true);machine.gameObject.SetActive(true);yield return null;Check(machine.rarity>=EItemRarity.Common && machine.rarity<=EItemRarity.Legendary && machine.usesLeft>0,"initial rarity and uses");typeof(InteractableMicrowave).GetField("<rarity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(machine,EItemRarity.Common);typeof(InteractableMicrowave).GetField("<usesLeft>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(machine,1);
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");LocalizationSettings.StringDatabase.TableProvider=new Provider();
        var popupHost=new GameObject("popup fixture");var always=popupHost.AddComponent<AlwaysUi>();AlwaysUi.Instance=always;var popup=popupHost.AddComponent<UiTextPopup>();always.UiTextPopup=popup;popup.parent=new GameObject("popup visual");popup.parent.SetActive(false);popup.t_text=new GameObject("popup text",typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();popup.sfx=popupHost.AddComponent<RandomSfx>();popup.canvasGroup=popupHost.AddComponent<CanvasGroup>();
        var ui=host.AddComponent<UiManager>();UiManager.Instance=ui;var encounter=host.AddComponent<EncounterWindows>();ui.encounterWindows=encounter;var window=FindAnyObjectByType<MicrowaveUi>(FindObjectsInactive.Include);Check(window!=null,"original UI clone");encounter.microwaveWindow=window;MyTime.time=1f;Check(machine.CanInteract(),"ready to interact");
        var goldField=typeof(PlayerInventory).GetField("<gold>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);goldField.SetValue(player.inventory,0f);Check(!machine.Interact() && popup.parent.activeSelf && popup.t_text.color==MyColorUtility.interactDisabledOutlineColor && !encounter.encounterInProgress,"unaffordable interaction warns");goldField.SetValue(player.inventory,100f);
        inventory.items.Remove(borgar.eItem);popup.parent.SetActive(false);Check(!machine.Interact() && popup.parent.activeSelf && !encounter.encounterInProgress,"insufficient distinct items warns");int failedEvents=0;Action<EItem> failed=e=>failedEvents++;InteractableMicrowave.A_Used+=failed;machine.UseMicrowave(clover.eItem);InteractableMicrowave.A_Used-=failed;Check(!machine.isCooking && machine.usesLeft==1 && failedEvents==0 && Mathf.Approximately(player.inventory.gold,100f),"invalid cooking does not consume or emit event");inventory.items.Add(borgar.eItem,new MicrowaveTestItem(inventory){amount=2});
        Check(!machine.Interact() && window.gameObject.activeInHierarchy && encounter.encounterInProgress && InteractableMicrowave.currentlyInteracting==machine && MyTime.paused,"interaction queues microwave selection");
        int events=0;EItem selected=default;Action<EItem> handler=e=>{events++;selected=e;};InteractableMicrowave.A_Used+=handler;MyTime.deltaTime=0f;
        var buttons=(List<MicrowaveItemButton>)typeof(MicrowaveUi).GetField("itemPrefabs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);Check(buttons.Count==2 && buttons[0].GetButton().onClick.GetPersistentEventCount()>0,"original item selection buttons");buttons[0].GetButton().onClick.Invoke();Check(machine.isCooking && machine.usesLeft==0 && events==1 && selected==clover.eItem,"starts once and emits selected item");
        Check(inventory.GetAmount(clover.eItem)==1 && inventory.GetAmount(borgar.eItem)==1 && Mathf.Approximately(player.inventory.gold,90f),"consume different same-rarity item and price");
        Check(machine.progressBar.activeSelf && machine.cookingParticles.activeSelf && !machine.exclamationMark.activeSelf && !machine.CanInteract(),"cooking visual state");Check(!window.gameObject.activeInHierarchy && !MyTime.paused,"selection closes and resumes gameplay");
        yield return null;yield return null;Check(machine.isCooking && Mathf.Approximately(machine.progressBarProgress.transform.localScale.x,0f),"paused clock stops cooking");
        MyTime.deltaTime=.5f;yield return null;yield return null;Check(machine.isCooking && machine.progressBarProgress.transform.localScale.x>0 && machine.progressBarProgress.transform.localScale.x<1,"progress advances on game clock");
        for(int i=0;i<10 && machine.isCooking;i++)yield return null;
        var hasItem=typeof(InteractableMicrowave).GetField("hasItem",BindingFlags.Instance|BindingFlags.NonPublic);var newItem=typeof(InteractableMicrowave).GetField("newItem",BindingFlags.Instance|BindingFlags.NonPublic);
        Check(!machine.isCooking && (bool)hasItem.GetValue(machine) && (EItem)newItem.GetValue(machine)==clover.eItem,"finished item ready");Check(!machine.GetInteractString().Contains("Missing localization"),"localized ready prompt");
        Check(machine.itemIcon.texture==clover.icon && machine.exclamationMark.activeSelf && !machine.progressBar.activeSelf && !machine.cookingParticles.activeSelf,"finished visual state");
        int exploded=0;Action explosionEvent=()=>exploded++;InteractableMicrowave.A_Exploded+=explosionEvent;Check(machine.CanInteract() && machine.Interact() && inventory.GetAmount(clover.eItem)==2 && !(bool)hasItem.GetValue(machine),"collects finished item");Check(!machine.CanInteract(),"no uses after collection");yield return new WaitForSeconds(.7f);Check(exploded==1 && machine.explosion.activeSelf && !machine.minimapIcon.activeSelf,"last use triggers explosion");InteractableMicrowave.A_Exploded-=explosionEvent;InteractableMicrowave.A_Used-=handler;inventory.Cleanup();Debug.Log("MicrowaveCookingPlayCheck PASSED: original prefab, UI button, cooking, collect and last-use explosion");EditorApplication.Exit(0);
    }
}
