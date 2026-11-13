using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[InitializeOnLoad]
public static class MicrowaveUiPlayCheck {
    const string Pending="MicrowaveUiPlayCheck.pending", Path="Assets/MicrowaveUiCheckTemporary.unity";
    static MicrowaveUiPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);
        var original=UnityEngine.Object.FindAnyObjectByType<MicrowaveUi>(FindObjectsInactive.Include);
        if(original==null || original.itemPrefab==null || original.t_price==null)throw new Exception("Original microwave UI references missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var root=UnityEngine.Object.Instantiate(original.gameObject);root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
        var ui=root.GetComponent<MicrowaveUi>();if(!ui.itemPrefab.transform.IsChildOf(root.transform) || !ui.t_price.transform.IsChildOf(root.transform))throw new Exception("UI references outside cloned hierarchy");
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))if(component!=ui && !(component is MicrowaveItemButton) && !(component is TMPro.TMP_Text))component.enabled=false;
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("microwave UI runner").AddComponent<MicrowaveUiPlayRunner>().Begin();}}
}
public class MicrowaveUiPlayRunner:MonoBehaviour {
    sealed class Provider:ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable { var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");return Addressables.ResourceManager.CreateCompletedOperation(table as T,null); } }
    static void Check(bool ok,string message){if(!ok)throw new Exception("MicrowaveUiPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        var host=new GameObject("fixture managers");host.SetActive(false);var game=host.AddComponent<GameManager>();GameManager.Instance=game;
        var player=host.AddComponent<MyPlayer>();MyPlayer.Instance=player;player.inventory=(PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));player.inventory.itemInventory=new ItemInventory();
        var stats=(PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));stats.stats=new Dictionary<EStat,float>{{EStat.ChestPriceMultiplier,1f}};player.inventory.playerStats=stats;
        var clover=Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset"));var borgar=Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Borgar.asset"));Check(clover!=null && borgar!=null,"real item assets");
        var data=host.AddComponent<DataManager>();DataManager.Instance=data;typeof(DataManager).GetField("itemData",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,new Dictionary<EItem,ItemData>{{clover.eItem,clover},{borgar.eItem,borgar}});
        player.inventory.itemInventory.items.Add(clover.eItem,new ItemClover(player.inventory.itemInventory){amount=2});player.inventory.itemInventory.items.Add(borgar.eItem,new ItemClover(player.inventory.itemInventory){amount=1});
        var microwave=host.AddComponent<InteractableMicrowave>();InteractableMicrowave.currentlyInteracting=microwave;typeof(InteractableMicrowave).GetField("<rarity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(microwave,EItemRarity.Common);
        var ui=FindAnyObjectByType<MicrowaveUi>(FindObjectsInactive.Include);Check(ui!=null,"original UI clone");ui.Open(EEncounter.Microwave);yield return null;
        var buttons=(List<MicrowaveItemButton>)typeof(MicrowaveUi).GetField("itemPrefabs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
        Check(buttons.Count==2 && buttons[0].gameObject.activeSelf && buttons[1].gameObject.activeSelf,"two matching items and reused template");Check(buttons[0].icon.texture==clover.icon && buttons[1].icon.texture==borgar.icon,"correct item icons");Check(ui.t_price.text=="<size=110%><sprite name=gold></size> 10","original price display");
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");LocalizationSettings.StringDatabase.TableProvider=new Provider();
        var popupHost=new GameObject("popup host");var always=popupHost.AddComponent<AlwaysUi>();AlwaysUi.Instance=always;var popup=popupHost.AddComponent<UiTextPopup>();always.UiTextPopup=popup;
        popup.parent=new GameObject("popup visual");popup.parent.SetActive(false);popup.t_text=new GameObject("popup text",typeof(RectTransform),typeof(CanvasRenderer),typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();popup.sfx=popupHost.AddComponent<RandomSfx>();popup.canvasGroup=popupHost.AddComponent<CanvasGroup>();
        clover.maxAmount=2;Check(buttons[0].GetButton().onClick.GetPersistentEventCount()>0,"original button has persistent binding");buttons[0].GetButton().onClick.Invoke();
        Check(popup.parent.activeSelf && popup.t_text.color==Color.red && !string.IsNullOrEmpty(popup.t_text.text),"max stack invokes original button warning");
        player.inventory.itemInventory.items.Remove(borgar.eItem);ui.OnClose();ui.Open(EEncounter.Microwave);yield return null;
        Check(buttons.Count==2 && buttons[0].gameObject.activeSelf && !buttons[1].gameObject.activeSelf,"reopening hides stale buttons");
        typeof(InteractableMicrowave).GetField("<rarity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(microwave,EItemRarity.Rare);ui.OnClose();ui.Open(EEncounter.Microwave);yield return null;
        Check(!buttons[0].gameObject.activeSelf && !buttons[1].gameObject.activeSelf,"filters other rarity");ui.OnClose();Check(!ui.gameObject.activeSelf,"close hides screen");
        player.inventory.itemInventory.Cleanup();Debug.Log("MicrowaveUiPlayCheck PASSED: original UI item filtering, icon reuse, price and close");EditorApplication.Exit(0);
    }
}
