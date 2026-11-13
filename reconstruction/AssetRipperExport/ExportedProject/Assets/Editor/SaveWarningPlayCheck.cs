using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Assets.Scripts.UI.DynamicWindows;
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
public static class SaveWarningPlayCheck
{
    const string Pending = "SaveWarningPlayCheck.pending", ScenePath = "Assets/SaveWarningCheckTemporary.unity";
    static SaveWarningPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run()
    {
        var source = EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var original = UnityEngine.Object.FindAnyObjectByType<ConfigWarning>(FindObjectsInactive.Include);
        var windows = UnityEngine.Object.FindAnyObjectByType<DynamicWindows>(FindObjectsInactive.Include);
        if (original == null || windows == null) throw new Exception("original warning/window bindings missing");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var clone = UnityEngine.Object.Instantiate(original.gameObject); clone.SetActive(false); SceneManager.MoveGameObjectToScene(clone, scene);
        var windowClone = UnityEngine.Object.Instantiate(windows.gameObject); windowClone.SetActive(false); SceneManager.MoveGameObjectToScene(windowClone, scene);
        EditorSceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true); EditorSceneManager.SaveScene(scene, ScenePath);
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("save warning runner").AddComponent<SaveWarningRunner>().Begin(); } }
}
public class SaveWarningRunner : MonoBehaviour
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_" + locale.Identifier.Code + ".asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Check(bool ok, string msg) { if (!ok) throw new Exception("SaveWarningPlayCheck: " + msg); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var it=Run(); while(true) { bool more; try { more=it.MoveNext(); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if(!more) yield break; yield return it.Current; } }
    IEnumerator Run()
    {
        LocalizationSettings.StringDatabase.TableProvider = new Provider(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("es");
        var warning = FindAnyObjectByType<ConfigWarning>(FindObjectsInactive.Include); var windows = FindAnyObjectByType<DynamicWindows>(FindObjectsInactive.Include);
        Check(warning != null && warning.overlay != null && warning.t_warning != null && windows != null && windows.dynamicWindowPromptPrefab != null, "original scene bindings");
        foreach(var b in warning.GetComponentsInChildren<Button>(true)) { string calls=""; for(int i=0;i<b.onClick.GetPersistentEventCount();i++) calls+=b.onClick.GetPersistentTarget(i)+"."+b.onClick.GetPersistentMethodName(i)+","; Debug.Log("Save warning button: "+b.name+" calls="+calls); }
        warning.gameObject.SetActive(true); windows.gameObject.SetActive(true);
        var root = new GameObject("isolated save state"); root.SetActive(false);
        var save = root.AddComponent<SaveManager>(); var always = root.AddComponent<AlwaysManager>(); var alwaysUi = root.AddComponent<AlwaysUi>();
        AlwaysManager.Instance = always; AlwaysUi.Instance = alwaysUi; always.alwaysUi = alwaysUi; alwaysUi.configWarning = warning; alwaysUi.dynamicWindows = windows;
        var pathField = typeof(SaveManager).GetField("defaultSavesPath", BindingFlags.Static | BindingFlags.NonPublic);
        var idField = typeof(SaveManager).GetField("steamIdSave", BindingFlags.Static | BindingFlags.NonPublic);
        string oldPath=(string)pathField.GetValue(null); ulong oldId=(ulong)idField.GetValue(null);
        string name="codex-warning-check-"+Guid.NewGuid().ToString("N"), dir=Path.Combine(Application.persistentDataPath,name);
        const string pref="saves_last_steamid"; bool hadPref=PlayerPrefs.HasKey(pref); string oldPref=PlayerPrefs.GetString(pref,"");
        pathField.SetValue(null,name); idField.SetValue(null,69UL); SaveManager.loaded=false; PlayerPrefs.DeleteKey(pref);
        string reason, path; Check(save.Load(0,false,out reason,out path),"initial temporary load: "+reason);
        save.config.cfVideoSettings.fov=80f;save.SaveConfig();save.config.cfVideoSettings.fov=90f;save.SaveConfig();
        string config=Path.Combine(dir,"LocalDir","config.json"); string backup=File.ReadAllText(config+".bak");
        File.WriteAllText(config,"{broken");SaveManager.loaded=false;save.Load(false);
        yield return null; yield return null;
        Check(warning.overlay.activeSelf && warning.t_warning.text.Contains(config) && warning.t_warning.text.Contains("No se pudo cargar"),"original warning displays localized error and path: "+warning.t_warning.text);
        Button backupButton=null;
        foreach(var button in warning.GetComponentsInChildren<Button>(true)) for(int i=0;i<button.onClick.GetPersistentEventCount();i++) if(button.onClick.GetPersistentTarget(i)==warning && button.onClick.GetPersistentMethodName(i)=="LoadBackup") backupButton=button;
        Check(backupButton!=null,"original backup button binding");backupButton.onClick.Invoke();
        Check(SaveManager.loaded && !warning.overlay.activeSelf && Mathf.Approximately(save.config.cfVideoSettings.fov,80f),"bound backup button loads backup");
        save.SaveConfig();Check(File.ReadAllText(config+".bak")==backup,"good backup retained after recovery save");
        warning.ShowWarning("test error",config);yield return null;yield return null;
        Button closeButton=null; foreach(var b in warning.GetComponentsInChildren<Button>(true)) if(b.name=="B_Back") closeButton=b;
        Check(closeButton!=null,"original close button"); closeButton.onClick.Invoke(); yield return null;
        var prompt=windows.GetComponentInChildren<DWindowPrompt>(true);
        Check(prompt!=null && prompt.t_content.text.Contains("NO") && warning.overlay.activeSelf,"ignore requires original Spanish confirmation");
        string before=File.ReadAllText(config);prompt.Accept();yield return null;
        save.SaveConfig();save.SaveStats();save.SaveProgression();
        Check(!warning.overlay.activeSelf && SaveManager.loaded && File.ReadAllText(config)==before,"confirming no-save leaves disk unchanged");
        AlwaysManager.Instance=null;AlwaysUi.Instance=null;typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{null});SaveManager.loaded=false;
        pathField.SetValue(null,oldPath);idField.SetValue(null,oldId);if(hadPref)PlayerPrefs.SetString(pref,oldPref);else PlayerPrefs.DeleteKey(pref);
        Destroy(root);if(Directory.Exists(dir))Directory.Delete(dir,true);
        Debug.Log("SaveWarningPlayCheck PASSED: original warning, Spanish text, bound backup recovery and no-save confirmation");EditorApplication.Exit(0);
    }
}
