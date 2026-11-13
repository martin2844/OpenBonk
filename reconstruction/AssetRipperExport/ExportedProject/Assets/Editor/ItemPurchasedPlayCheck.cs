using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ItemPurchasedPlayCheck {
    const string Pending="ItemPurchasedPlayCheck.pending",Path="Assets/ItemPurchasedCheckTemporary.unity";
    static ItemPurchasedPlayCheck(){EditorApplication.playModeStateChanged+=State;}
    public static void Run(){
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity",OpenSceneMode.Single);var original=UnityEngine.Object.FindAnyObjectByType<ItemPurchasedUI>(FindObjectsInactive.Include);
        if(original==null || original.content==null || !original.content.transform.IsChildOf(original.transform) || original.background==null || original.itemDisplay==null || original.itemNameText==null || original.extraText==null)throw new Exception("Original purchase panel references missing or outside hierarchy");
        bool bound=false;foreach(var button in original.GetComponentsInChildren<Button>(true))for(int i=0;i<button.onClick.GetPersistentEventCount();i++)if(button.onClick.GetPersistentTarget(i)==original && button.onClick.GetPersistentMethodName(i)=="Close")bound=true;
        if(!bound)throw new Exception("Original purchase panel has no close binding");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var clone=UnityEngine.Object.Instantiate(original.gameObject);clone.SetActive(false);SceneManager.MoveGameObjectToScene(clone,scene);EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("purchase test").AddComponent<ItemPurchasedPlayRunner>().Begin();}}
}
public class ItemPurchasedPlayRunner:MonoBehaviour {
    sealed class Provider:ITableProvider {public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable {var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);}}
    string error;void Capture(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)error=message+"\n"+stack;}
    static void Check(bool ok,string message){if(!ok)throw new Exception("ItemPurchasedPlayCheck: "+message);}
    public void Begin(){StartCoroutine(Guard());}
    IEnumerator Guard(){var test=Run();while(true){bool more;try{more=test.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)yield break;yield return test.Current;}}
    IEnumerator Run(){
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");LocalizationSettings.StringDatabase.TableProvider=new Provider();Application.logMessageReceived+=Capture;new GameObject("audio").AddComponent<AudioListener>();
        var item=Instantiate(AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponRevolver.asset"));Check(item!=null,"original weapon");item.localizedName=new LocalizedString("Weapons","REVOLVER_NAME");
        var state=new GameObject("synthetic data");state.SetActive(false);var data=state.AddComponent<DataManager>();DataManager.Instance=data;var fox=Instantiate(AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"));fox.localizedName=new LocalizedString("Characters","FOX_NAME");((Dictionary<ECharacter,CharacterData>)typeof(DataManager).GetField("characterData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data)).Add(fox.eCharacter,fox);
        var skin=Instantiate(AssetDatabase.LoadAssetAtPath<SkinData>("Assets/MonoBehaviour/FoxIce.asset"));skin.localizedName=new LocalizedString("Skins","ICE");
        int beforeItems=UnlocksFooter.A_Purchased?.GetInvocationList().Length ?? 0,beforeSkins=SkinContainer.A_Purchased?.GetInvocationList().Length ?? 0,beforeClaims=ProgressionSaveFile.A_AchievementClaimed?.GetInvocationList().Length ?? 0;
        var panel=UnityEngine.Object.FindAnyObjectByType<ItemPurchasedUI>(FindObjectsInactive.Include);Check(panel!=null,"panel");panel.content.SetActive(false);panel.gameObject.SetActive(true);yield return null;
        UnlocksFooter.A_Purchased?.Invoke(item);yield return new WaitForSeconds(.05f);Check(!panel.content.activeSelf,"original delay");yield return new WaitForSeconds(.3f);
        Check(panel.content.activeSelf && panel.itemDisplay.texture==item.GetIcon() && panel.itemNameText.text=="Revolver","purchase event displays original icon and localized name");
        Check(panel.extraText.text.Contains("Purchased!") && panel.extraText.text.Contains("<sprite name=\"Check\""),"localized purchase confirmation");
        yield return new WaitForSeconds(.3f);Check(panel.itemDisplay.transform.localScale.x>0f && panel.itemNameText.transform.localScale.x>0f,"card and text animate");
        Button close=null;foreach(var b in panel.GetComponentsInChildren<Button>(true))for(int i=0;i<b.onClick.GetPersistentEventCount();i++)if(b.onClick.GetPersistentTarget(i)==panel && b.onClick.GetPersistentMethodName(i)=="Close")close=b;
        Check(close!=null,"original close button");close.onClick.Invoke();Check(!panel.content.activeSelf,"bound close hides panel");
        UnlocksFooter.A_Purchased?.Invoke(item);panel.Close();yield return new WaitForSeconds(.2f);Check(!panel.content.activeSelf,"close cancels pending display");
        SkinContainer.A_Purchased?.Invoke(skin);yield return new WaitForSeconds(.3f);Check(panel.content.activeSelf && panel.itemDisplay.texture==skin.GetIcon() && panel.itemNameText.text==skin.GetName() && panel.extraText.text.Contains("Skin"),"skin purchase event displays original skin");panel.Close();
        ProgressionSaveFile.A_AchievementClaimed?.Invoke(null);yield return new WaitForSeconds(.2f);Check(!panel.content.activeSelf,"original achievement-claimed handler is empty");
        Destroy(panel.gameObject);yield return null;Check((UnlocksFooter.A_Purchased?.GetInvocationList().Length ?? 0)==beforeItems && (SkinContainer.A_Purchased?.GetInvocationList().Length ?? 0)==beforeSkins && (ProgressionSaveFile.A_AchievementClaimed?.GetInvocationList().Length ?? 0)==beforeClaims,"purchase and claim event subscriptions are removed");
        Check(error==null,"unexpected Unity error: "+error);Application.logMessageReceived-=Capture;Debug.Log("ItemPurchasedPlayCheck PASSED: original panel, purchase event, delay, text, animation and bound close");EditorApplication.Exit(0);
    }
}
