using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.UI.HUD;
using Assets.Scripts.UI.InGame.Levelup;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Menu.Windows;
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
using UnityEngine.UI;

[InitializeOnLoad]
public static class BalanceShrinePlayCheck
{
    const string Pending = "BalanceShrinePlayCheck.pending";
    static BalanceShrinePlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("balance test runner").AddComponent<BalanceShrinePlayRunner>().Begin(); } }
}
public class BalanceShrinePlayRunner : MonoBehaviour
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/" + name + "_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    static void Require(bool value, string message) { if (!value) throw new Exception("BalanceShrinePlayCheck: " + message); }
    public void Begin() => StartCoroutine(Run());
    IEnumerator Run()
    {
        InteractableShrineBalance shrine = null; EncounterUi window = null; EncounterWindows queue = null; PlayerInventory inventory = null; GameObject minimap = null, alert = null;
        try
        {
            LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
            var root = new GameObject("synthetic runtime state"); root.SetActive(false);
            GameManager.Instance = root.AddComponent<GameManager>(); MyPlayer.Instance = root.AddComponent<MyPlayer>();
            inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); MyPlayer.Instance.inventory = inventory;
            var health = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); health.hp = 100; health.maxHp = 100; inventory.playerHealth = health;
            inventory.statInventory = new StatInventory(); inventory.itemInventory = new ItemInventory();
            var dataManager = root.AddComponent<DataManager>(); DataManager.Instance = dataManager;
            var data = Instantiate(AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/MonoBehaviour/ShrineOfBalance.asset"));
            data.localizedName = new LocalizedString("Game_Ui", "SHRINE_CHARGE_NAME"); data.localizedDescription = new LocalizedString("Game_Ui", "SHRINE_BALANCE_DESC");
            var dictionary = (Dictionary<EEncounter, EncounterData>)typeof(DataManager).GetField("encounterData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dataManager); dictionary[EEncounter.BalanceShrine] = data;
            var manager = root.AddComponent<UiManager>(); UiManager.Instance = manager; manager.shrineLogs = root.AddComponent<ShrineLogs>(); manager.scoreUi = root.AddComponent<ScoreUi>(); queue = root.AddComponent<EncounterWindows>(); manager.encounterWindows = queue;
            var ui = new GameObject("synthetic encounter window", typeof(RectTransform)); ui.SetActive(false); window = ui.AddComponent<EncounterUi>();
            window.t_name = new GameObject("name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); window.t_description = new GameObject("description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            window.particles = new GameObject("particles"); window.particles.transform.SetParent(ui.transform); window.tabsExplicitNavigation = ui.AddComponent<TabsExplicitNavigation>(); window.tabsExplicitNavigation.content = ui.transform;
            var button = new GameObject("button template", typeof(RectTransform), typeof(Button)); button.transform.SetParent(ui.transform); window.b_generic = button;
            var template = button.AddComponent<EncounterButton>(); var nav = template.GetComponent<Button>().navigation; nav.mode = Navigation.Mode.Explicit; template.GetComponent<Button>().navigation = nav;
            template.rarityContainer = new GameObject("rarity"); template.rarityContainer.transform.SetParent(button.transform);
            template.background = button.AddComponent<Image>(); template.selectedOverlay = new GameObject("selection", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); template.selectedOverlay.transform.SetParent(button.transform);
            template.iconBorder = new GameObject("border", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); template.iconBorder.transform.SetParent(button.transform);
            template.iconBackground = new GameObject("background", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); template.iconBackground.transform.SetParent(button.transform);
            template.t_description = new GameObject("text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); template.t_description.transform.SetParent(button.transform);
            template.t_rarity = new GameObject("rarity text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); template.t_rarity.transform.SetParent(button.transform);
            queue.genericEncounterWindow = window;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShrineBalance.prefab"); var instance = Instantiate(prefab, root.transform); instance.transform.SetParent(null); shrine = instance.GetComponent<InteractableShrineBalance>();
            Require(shrine != null && shrine.minimapIcon != null && shrine.alertIcon != null, "original shrine prefab and markers"); minimap = shrine.minimapIcon; alert = shrine.alertIcon;
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/Game_Interactables_en.asset"); Require(shrine.GetInteractString() == table.GetEntry("SHRINE_BALANCE").GetLocalizedString(), "original localized prompt");
            var detector = new GameObject("synthetic player detector").AddComponent<DetectInteractables>(); detector.whatIsInteractable = 1 << instance.layer; detector.transform.position = instance.transform.position;
            Physics.SyncTransforms(); typeof(DetectInteractables).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(detector, null);
            Require((BaseInteractable)typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(detector) == shrine, "proximity detection finds original shrine collider");
            detector.TryInteract(); Require(shrine.done && !shrine.Interact(), "detected shrine queues reward once");
            Require(queue.encounterInProgress && MyTime.paused && window.gameObject.activeSelf, "queue pauses game and opens encounter");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        float deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline)
        {
            var currentButtons = window.GetComponentsInChildren<EncounterButton>(true);
            if (currentButtons.Length == 3 && currentButtons[0].GetComponent<Button>().interactable) break;
            yield return null;
        }
        try
        {
            Require(shrine == null && minimap == null && alert == null, "used shrine component and markers are destroyed");
            var buttons = window.GetComponentsInChildren<EncounterButton>(true);
            Require(buttons.Length == 3 && buttons[0].GetComponent<Button>().interactable, "two offers and decline button become selectable; count=" + buttons.Length + " first=" + (buttons.Length > 0 && buttons[0].GetComponent<Button>().interactable) + " scale=" + Time.timeScale + " time=" + Time.time + " realtime=" + Time.realtimeSinceStartup);
            var offers = (EncounterOffer[])typeof(EncounterUi).GetField("offers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(window);
            var modifier = offers[0].effects[0].statModifier;
            buttons[0].GetComponent<Button>().onClick.Invoke();
            Require(inventory.statInventory.permanentChanges[modifier.stat].Contains(modifier), "selected shrine effect applied");
            Require(!window.gameObject.activeSelf && !queue.encounterInProgress && !MyTime.paused, "selection closes window and resumes game");
            Debug.Log("BalanceShrinePlayCheck passed: original shrine prefab, reward queue, offer selection and game resume"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
