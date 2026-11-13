using System;
using System.Reflection;
using System.Collections.Generic;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public static class MenuProgressInfoPortCheck
{
    sealed class Provider : ITableProvider {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable {
            var table=ScriptableObject.CreateInstance<StringTable>(); table.SharedData=ScriptableObject.CreateInstance<SharedTableData>(); table.SharedData.TableCollectionName=name; table.LocaleIdentifier=locale.Identifier; table.AddEntry("TIER","Tier"); table.AddEntry("RUNS","Runs"); return Addressables.ResourceManager.CreateCompletedOperation(table as T,null);
        }
    }
    static void Check(bool b,string m) { if(!b) throw new Exception("MenuProgressInfoPortCheck: "+m); }
    static void Call(object o,string m) => o.GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static TextMeshProUGUI Label(Transform parent) { var g=new GameObject("label",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI)); g.transform.SetParent(parent); return g.GetComponent<TextMeshProUGUI>(); }
    static RawImage Image(Transform parent) { var g=new GameObject("image",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)); g.transform.SetParent(parent); return g.GetComponent<RawImage>(); }
    public static void Run()
    {
        var oldSave=SaveManager.Instance; var oldData=DataManager.Instance; var instance=typeof(SaveManager).GetProperty("Instance",BindingFlags.Public|BindingFlags.Static); var root=new GameObject("menu progress fixture"); root.SetActive(false); var manager=root.AddComponent<SaveManager>(); manager.progression=new ProgressionSaveFile(); instance.SetValue(null,manager);
        var data=root.AddComponent<DataManager>(); DataManager.Instance=data;
        var registry=(Dictionary<ECharacter, CharacterData>)typeof(DataManager).GetField("characterData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data);
        var fox=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/Fox.asset"); Check(fox,"Fox asset"); registry.Add(ECharacter.Fox,fox);
        var loc=ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.Instance=loc; LocalizationSettings.SelectedLocale=Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider=new Provider();
        var source=AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset"); Check(source,"Forest map asset"); var map=UnityEngine.Object.Instantiate(source); map.localizedName=new LocalizedString(); var config=new RunConfig{mapData=map,mapTierIndex=0};
        var ui=root.AddComponent<MapStatsInfoUi>(); ui.t_mapName=Label(root.transform); ui.t_mapRuns=Label(root.transform); ui.t_tier=Label(root.transform); ui.t_highscore=Label(root.transform); ui.t_fastestTime=Label(root.transform); ui.characterIconPrefab=Image(root.transform); ui.mapIcon=Image(root.transform);
        var selection=root.AddComponent<MapSelectionUi>(); selection.runConfig=config; var window=root.AddComponent<MapStatsWindow>(); window.mapSelectionUi=selection;
        var counter=root.AddComponent<GoldCounter>(); counter.t_silver=Label(root.transform);
        try {
            Call(counter,"Start"); Check(counter.t_silver.text.Contains("silver") && counter.t_silver.text.EndsWith("0"),"initial silver"); manager.progression.silver=1234; ProgressionSaveFile.A_SilverChanged?.Invoke(1234); Check(counter.t_silver.text.Contains("1,234"),"silver event uses actual balance"); Check(Vector3.Distance(counter.t_silver.transform.localScale,Vector3.one*1.6f)<.001f,"silver change pulse");
            Call(ui,"Awake"); Call(ui,"OnEnable"); Check(ui.t_mapRuns.text=="0 Runs","empty tier run count after late panel activation"); Check(ui.t_highscore.text=="-" && ui.t_fastestTime.text=="-","empty records"); Check(ui.t_tier.text=="Tier 1","tier label"); Check(!ui.characterIconPrefab.gameObject.activeSelf,"no completion icons");
            var progress=manager.progression.menuMeta.GetMapProgress(map.eMap); progress.numRunsByTier[0]=5; progress.tierHighscores[0]=1234; progress.tierFastestTimes[0]=83.5f; MapSelectionUi.A_RunConfigChanged?.Invoke(config);
            Check(ui.t_mapRuns.text=="5 Runs","saved run count"); Check(ui.t_highscore.text.Contains("1,234") && ui.t_highscore.text.Contains("skull"),"saved highscore"); Check(ui.t_fastestTime.text.Contains("01:23") && ui.t_fastestTime.text.Contains(".5"),"saved fastest time: "+ui.t_fastestTime.text);
            var missing=(ECharacter)OpenBonkContentIds.Character("pikachu","pikachu"); progress.tierCompletionsWithCharacters[0]=new HashSet<ECharacter>{missing,ECharacter.Fox};
            ui.SetConfig(config); Check(ui.characterIconPrefab.gameObject.activeSelf && ui.characterIconPrefab.texture==fox.GetIcon(),"missing mod leaves built-in completion visible");
            Check(progress.tierCompletionsWithCharacters[0].Contains(missing),"missing mod history preserved");
            registry.Add(missing,fox); ui.SetConfig(config);
            var icons=(List<RawImage>)typeof(MapStatsInfoUi).GetField("characterIcons",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
            Check(icons.Count==2 && icons[0].gameObject.activeSelf && icons[1].gameObject.activeSelf,"restored mod history visible");
            registry.Remove(missing); ui.SetConfig(config); Check(icons[0].gameObject.activeSelf && !icons[1].gameObject.activeSelf,"disabled mod hides extra icon");
            Call(counter,"OnDestroy"); Call(ui,"OnDestroy"); Debug.Log("MenuProgressInfoPortCheck passed: saved silver, map records and missing/restored mod character history");
        } finally { instance.SetValue(null,oldSave); DataManager.Instance=oldData; LocalizationSettings.Instance=null; UnityEngine.Object.DestroyImmediate(loc); UnityEngine.Object.DestroyImmediate(map); UnityEngine.Object.DestroyImmediate(root); }
    }
}
