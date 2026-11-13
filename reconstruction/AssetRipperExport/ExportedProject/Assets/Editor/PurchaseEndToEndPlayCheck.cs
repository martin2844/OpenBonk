using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Newtonsoft.Json;

[InitializeOnLoad]
public static class PurchaseEndToEndPlayCheck {
    const string Pending="PurchaseEndToEndPlayCheck.pending",Path="Assets/PurchaseEndToEndCheckTemporary.unity";
    static PurchaseEndToEndPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);
        var ui=UnityEngine.Object.FindAnyObjectByType<UnlocksUi>(FindObjectsInactive.Include);var panel=UnityEngine.Object.FindAnyObjectByType<ItemPurchasedUI>(FindObjectsInactive.Include);
        if(ui==null || panel==null || !panel.content.transform.IsChildOf(panel.transform))throw new Exception("Original purchase hierarchies missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var a=UnityEngine.Object.Instantiate(ui.gameObject);a.SetActive(false);SceneManager.MoveGameObjectToScene(a,scene);
        var b=UnityEngine.Object.Instantiate(panel.gameObject);b.SetActive(false);SceneManager.MoveGameObjectToScene(b,scene);
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("purchase route runner").AddComponent<PurchaseEndToEndRunner>().Begin();}}
}
public class PurchaseEndToEndRunner:MonoBehaviour {
    sealed class Provider:ITableProvider {public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable {var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);}}
    string error;void Capture(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)error=message+"\n"+stack;}
    static void Check(bool ok,string message){if(!ok)throw new Exception("PurchaseEndToEndPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");LocalizationSettings.StringDatabase.TableProvider=new Provider();Application.logMessageReceived+=Capture;new GameObject("audio").AddComponent<AudioListener>();var events=new GameObject("events").AddComponent<EventSystem>();
        var state=new GameObject("test state");state.SetActive(false);var save=state.AddComponent<SaveManager>();save.progression=new ProgressionSaveFile();save.stats=new StatsSaveFile();save.config=new ConfigSaveFile();save.progression.silver=1000;foreach(EShopItem e in Enum.GetValues(typeof(EShopItem)))save.progression.shopItems[e]=0;typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{save});
        var data=state.AddComponent<DataManager>();DataManager.Instance=data;var gun=Instantiate(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset"));gun.localizedName=new LocalizedString("Weapons","REVOLVER_NAME");Check(gun.GetUnlockRequirement()!=null,"original Revolver unlock");
        data.unsortedCharacterData=new List<CharacterData>();data.unsortedWeapons=new List<WeaponData>{gun};data.unsortedTomes=new List<TomeData>();data.unsortedItems=new List<ItemData>();data.unsortedHats=new List<HatData>();foreach(var name in new[]{"Refresh","Skip","Banish"}){var shop=AssetDatabase.LoadAssetAtPath<ShopItemData>("Assets/MonoBehaviour/"+name+".asset");data.shopItems.Add(shop.eShopItem,shop);}save.progression.achievements.Add(gun.GetUnlockRequirement().internalName);
        var ui=UnityEngine.Object.FindAnyObjectByType<UnlocksUi>(FindObjectsInactive.Include);var panel=UnityEngine.Object.FindAnyObjectByType<ItemPurchasedUI>(FindObjectsInactive.Include);var footer=ui.GetComponentInChildren<UnlocksFooter>(true);panel.content.SetActive(false);panel.gameObject.SetActive(true);ui.gameObject.SetActive(true);yield return null;
        ui.tabButtons.ButtonPressed(1,true);yield return null;UnlockContainer card=null;foreach(var c in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true))if(c.gameObject.activeSelf && c.unlockable==gun)card=c;Check(card!=null && card.isUnlocked && !card.isPurchased,"original unlocked Revolver card");
        events.SetSelectedGameObject(card.gameObject);Check(footer.buyContainer.activeInHierarchy,"purchase footer visible");yield return new WaitForSeconds(.2f);card.GetComponent<Button>().onClick.Invoke();Check(events.currentSelectedGameObject==footer.buyButton.gameObject,"original card click focuses Buy");
        int price=gun.GetPrice();footer.buyButton.GetButton().onClick.Invoke();Check(save.progression.purchases.Contains(gun.GetInternalName()) && save.progression.silver==1000-price,"bound Buy records purchase and deducts silver");
        yield return new WaitForSeconds(.3f);Check(panel.content.activeInHierarchy && panel.itemDisplay.texture==gun.GetIcon() && panel.itemNameText.text=="Revolver" && panel.extraText.text.Contains("Purchased!"),"real purchase event opens original confirmation");
        var pathField=typeof(SaveManager).GetField("defaultSavesPath",BindingFlags.Static|BindingFlags.NonPublic);var testField=typeof(SaveManager).GetField("TEST_SAVES",BindingFlags.Static|BindingFlags.NonPublic);string oldPath=(string)pathField.GetValue(null);bool oldTest=(bool)testField.GetValue(null),oldLoaded=SaveManager.loaded;string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"megabonk-purchase-"+Guid.NewGuid().ToString("N"));
        try {System.IO.Directory.CreateDirectory(System.IO.Path.Combine(temp,"CloudDir","69"));pathField.SetValue(null,temp);testField.SetValue(null,false);SaveManager.loaded=true;save.SaveProgression();string savedPath=(string)typeof(SaveManager).GetMethod("GetProgressionPath",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(save,null);var reloaded=JsonConvert.DeserializeObject<ProgressionSaveFile>(SimpleEncryptor.Decrypt(System.IO.File.ReadAllText(savedPath)));Check(reloaded.purchases.Contains(gun.GetInternalName()) && reloaded.silver==1000-price,"purchase and silver survive encrypted save round trip");}
        finally {SaveManager.loaded=oldLoaded;pathField.SetValue(null,oldPath);testField.SetValue(null,oldTest);if(System.IO.Directory.Exists(temp))System.IO.Directory.Delete(temp,true);}
        Button close=null;foreach(var b in panel.GetComponentsInChildren<Button>(true))for(int i=0;i<b.onClick.GetPersistentEventCount();i++)if(b.onClick.GetPersistentTarget(i)==panel && b.onClick.GetPersistentMethodName(i)=="Close")close=b;Check(close!=null,"original close binding");close.onClick.Invoke();Check(!panel.content.activeSelf,"confirmation closes");
        Check(error==null,"unexpected Unity error: "+error);Application.logMessageReceived-=Capture;Debug.Log("PurchaseEndToEndPlayCheck PASSED: original unlock card, bound Buy, progression and purchase confirmation");EditorApplication.Exit(0);
    }
}
