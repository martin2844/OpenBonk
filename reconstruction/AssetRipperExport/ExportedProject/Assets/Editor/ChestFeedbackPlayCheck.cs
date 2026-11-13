using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

[InitializeOnLoad]
public static class ChestFeedbackPlayCheck
{
    const string Pending = "ChestFeedbackPlayCheck.pending";
    static ChestFeedbackPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("chest feedback test").AddComponent<ChestFeedbackPlayRunner>().Begin(); } }
}
public class ChestFeedbackPlayRunner : MonoBehaviour
{
    sealed class Provider : ITableProvider { public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable { var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset"); return Addressables.ResourceManager.CreateCompletedOperation(table as T, null); } }
    static void Check(bool condition, string message) { if (!condition) throw new Exception("ChestFeedbackPlayCheck: " + message); }
    public void Begin() { StartCoroutine(Guard()); }
    IEnumerator Guard() { var test = Run(); while (true) { bool next; try { next = test.MoveNext(); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; } if (!next) yield break; yield return test.Current; } }
    IEnumerator Run()
    {
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var state = new GameObject("controlled state"); state.SetActive(false);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = state.AddComponent<GameManager>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float> { [EStat.ChestPriceMultiplier] = 1f }; player.inventory.playerStats = stats;
        var ui = state.AddComponent<AlwaysUi>(); AlwaysUi.Instance = ui; var popup = state.AddComponent<UiTextPopup>(); ui.UiTextPopup = popup;
        popup.parent = new GameObject("popup parent"); popup.parent.SetActive(false); popup.t_text = new GameObject("text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); popup.sfx = state.AddComponent<RandomSfx>();
        var chestObject = new GameObject("chest"); chestObject.SetActive(false); chestObject.transform.position = new Vector3(2, 3, 4); var chest = chestObject.AddComponent<InteractableChest>(); chest.chestType = EChest.Normal;
        Check(!chest.Interact(), "unaffordable chest must refuse interaction");
        Check(popup.parent.activeSelf && popup.t_text.text == Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("PopupText", "CANT_AFFORD"), "original localized notice");
        Check(popup.t_text.color == Assets.Scripts.Utility.MyColorUtility.interactDisabledOutlineColor, "disabled color");
        Check(Vector3.Distance(popup.parent.transform.position, new Vector3(Screen.width * .55f, Screen.height * .55f, 0)) < .01f, "popup screen position");
        var effects = state.AddComponent<EffectManager>(); EffectManager.Instance = effects; effects.wuiFreeChest = new GameObject("free chest effect"); effects.wuiFreeChest.SetActive(false);
        var key = new ItemKey(player.inventory.itemInventory); typeof(ItemKey).GetField("currentChance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(key, 1f); player.inventory.itemInventory.items[EItem.Key] = key;
        typeof(InteractableChest).GetField("opening", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(chest, true);
        typeof(InteractableChest).GetMethod("OnChestWindowClose", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(chest, null);
        var clone = GameObject.Find("free chest effect(Clone)");
        if (clone == null) foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) if (t.name == "free chest effect(Clone)") { clone = t.gameObject; break; }
        Check(clone != null && Vector3.Distance(clone.transform.position, new Vector3(2, 4.5f, 4)) < .001f, "key effect above chest");
        Check(player.inventory.gold == 0f, "successful key does not spend gold");
        yield return null; Debug.Log("ChestFeedbackPlayCheck passed: unaffordable popup and free-key effect"); EditorApplication.Exit(0);
    }
}
