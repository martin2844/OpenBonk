using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ChestTextAnimationPlayCheck {
    const string Pending="ChestTextAnimationPlayCheck.pending", Path="Assets/ChestTextCheckTemporary.unity";
    static ChestTextAnimationPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() {
        var source=EditorSceneManager.OpenScene("Assets/ScenesAndroid/GeneratedMap.unity",OpenSceneMode.Single);
        var original=UnityEngine.Object.FindAnyObjectByType<ChestWindowUi>(FindObjectsInactive.Include);
        if(original==null || original.t_itemName==null || original.t_itemDesc==null || original.t_itemRarity==null) throw new Exception("Original chest text references missing");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("chest text fixture",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(root,scene);var chest=root.AddComponent<ChestWindowUi>();
        TextMeshProUGUI Copy(TextMeshProUGUI text) { var go=UnityEngine.Object.Instantiate(text.gameObject,root.transform);go.SetActive(true);return go.GetComponent<TextMeshProUGUI>(); }
        chest.t_itemName=Copy(original.t_itemName);chest.t_itemDesc=Copy(original.t_itemDesc);chest.t_itemRarity=Copy(original.t_itemRarity);chest.itemData=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset");
        if(chest.itemData==null)throw new Exception("Clover asset missing");
        EditorSceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true);EditorSceneManager.SaveScene(scene,Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state) { if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false);new GameObject("chest text runner").AddComponent<ChestTextAnimationRunner>().Begin(); } }
}
public class ChestTextAnimationRunner:MonoBehaviour {
    sealed class Provider:ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name,Locale locale) where T:LocalizationTable { var table=AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/"+name+"_en.asset");return Addressables.ResourceManager.CreateCompletedOperation(table as T,null); } }
    static void Check(bool condition,string message) { if(!condition)throw new Exception("ChestTextAnimationPlayCheck: "+message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test=Run();while(true) { bool more;try { more=test.MoveNext(); }catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1);yield break; }if(!more)yield break;yield return test.Current; } }
    IEnumerator Run() {
        LocalizationSettings.Instance=ScriptableObject.CreateInstance<LocalizationSettings>();LocalizationSettings.SelectedLocale=Locale.CreateLocale("en");LocalizationSettings.StringDatabase.TableProvider=new Provider();
        var chest=UnityEngine.Object.FindAnyObjectByType<ChestWindowUi>();Check(chest!=null && chest.itemData!=null,"fixture");chest.itemData.GetDummyItem();
        chest.t_itemName.CrossFadeAlpha(0,0,true);chest.t_itemDesc.CrossFadeAlpha(0,0,true);chest.t_itemRarity.CrossFadeAlpha(0,0,true);yield return null;
        var method=typeof(ChestWindowUi).GetMethod("AnimateText",BindingFlags.Instance|BindingFlags.NonPublic);chest.StartCoroutine((IEnumerator)method.Invoke(chest,null));
        Check(chest.t_itemName.text==chest.itemData.GetName() && chest.t_itemRarity.text==LocalizationUtility.GetRarity(chest.itemData.rarity),"item and localized rarity text");
        Check(chest.t_itemRarity.color==MyColorUtility.GetItemRarityColor(chest.itemData.rarity),"rarity color");
        yield return new WaitForSeconds(.12f);
        Check(chest.t_itemName.canvasRenderer.GetAlpha()>0 && chest.t_itemName.canvasRenderer.GetAlpha()<1 && chest.t_itemDesc.canvasRenderer.GetAlpha()<.01f,"name fades before description");
        Check(chest.t_itemName.transform.localScale.x>1f,"name scales down during fade");
        yield return new WaitForSeconds(.8f);
        Check(chest.t_itemName.canvasRenderer.GetAlpha()>.99f && chest.t_itemDesc.canvasRenderer.GetAlpha()>.99f && chest.t_itemRarity.canvasRenderer.GetAlpha()>.99f,"all text visible");
        Check(Vector3.Distance(chest.t_itemName.transform.localScale,Vector3.one)<.001f && Vector3.Distance(chest.t_itemDesc.transform.localScale,Vector3.one)<.001f,"scale resets");
        Debug.Log("ChestTextAnimationPlayCheck PASSED: original text objects, rarity localization/color and staged fade");EditorApplication.Exit(0);
    }
}
