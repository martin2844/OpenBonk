using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

[InitializeOnLoad]
public static class ServerFeedPlayCheck
{
    const string Pending = "ServerFeedPlayCheck.pending";
    static ServerFeedPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("feed test").AddComponent<ServerFeedPlayRunner>().Begin(); } }
}
public class ServerFeedPlayRunner : MonoBehaviour
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ServerFeedPlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        ServerFeed feed = null; ServerFeedPrefab first = null; Texture2D icon = null; SaveManager save = null;
        try
        {
            LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
            save = new GameObject("save").AddComponent<SaveManager>(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.show_item_feed = 1;
            var data = new GameObject("data").AddComponent<DataManager>(); DataManager.Instance = data;
            var item = Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/UnstableTransfusion.asset"));
            item.localizedName = new LocalizedString("ItemsBlue", "UnstableTransfusion_NAME");
            ((Dictionary<EItem, ItemData>)typeof(DataManager).GetField("itemData", P).GetValue(data)).Add(item.eItem, item);
            var host = new GameObject("feed"); host.SetActive(false); feed = host.AddComponent<ServerFeed>();
            feed.content = new GameObject("content", typeof(RectTransform)).transform;
            feed.serverFeedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ServerFeedPrefab.prefab");
            Require(feed.serverFeedPrefab != null, "exported feed prefab"); host.SetActive(true);
            icon = new Texture2D(2, 2); feed.SetFeed("first", 0f, icon);
            var entries = (List<ServerFeedPrefab>)typeof(ServerFeed).GetField("activePrefabs", P).GetValue(feed); first = entries[0];
            Require(entries.Count == 1 && first.t_info.text == "first" && first.i_icon.enabled && first.i_icon.texture == icon, "first feed text and icon");
            for (int i = 1; i < 14; i++) feed.SetFeed("entry " + i, 0f);
            Require(entries.Count == 13 && entries[12] == first && first.t_info.text == "entry 13" && !first.i_icon.enabled && first.transform.GetSiblingIndex() == 12, "oldest entry reused at capacity");
            ItemInventory.A_ItemAdded?.Invoke(item.eItem);
            Require(entries.Count == 13 && entries[12].t_info.text.StartsWith("+1 ") && entries[12].i_icon.texture == item.icon, "item event creates feed entry");
            var previous = entries[12]; save.config.cfGameSettings.show_item_feed = 0; ItemInventory.A_ItemAdded?.Invoke(item.eItem);
            Require(entries[12] == previous, "disabled item feed suppresses entry");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        yield return new WaitForSecondsRealtime(1.5f);
        try
        {
            Require(first.canvasGroup.alpha == 0f, "reused entry completes fade in and out");
            save.config.cfGameSettings.show_item_feed = 1;
            Destroy(feed.gameObject);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        yield return null;
        ItemInventory.A_ItemAdded?.Invoke(EItem.UnstableTransfusion);
        Debug.Log("ServerFeedPlayCheck passed: original prefab, icon and text, capacity reuse, item event, setting and fades"); EditorApplication.Exit(0);
    }
}
