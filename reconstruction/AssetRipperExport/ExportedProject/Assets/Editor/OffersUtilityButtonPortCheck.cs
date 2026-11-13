using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public static class OffersUtilityButtonPortCheck
{
    sealed class Provider : ITableProvider
    {
        public AsyncOperationHandle<T> ProvideTableAsync<T>(string name, Locale locale) where T : LocalizationTable
        {
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/String Tables/PopupText_en.asset");
            return Addressables.ResourceManager.CreateCompletedOperation(table as T, null);
        }
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("Offers utility: " + message); }
    public static void Run()
    {
        foreach (string sceneName in new[] { "GeneratedMap", "FinalBossMap" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + sceneName + ".unity", OpenSceneMode.Single);
            var buttons = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MyButtonOffersUtility>(true)).ToArray();
            foreach (var b in buttons) Debug.Log(sceneName + " button=" + b.name + " amount=" + (b.t_amount != null) + " price=" + (b.t_price != null) + " overlay=" + (b.disbaledOverlay != null) + " background=" + (b.background != null));
            Check(buttons.Length > 0, sceneName + " buttons");
        }
        var root = new GameObject("offers fixture"); root.SetActive(false);
        LocalizationSettings.Instance = ScriptableObject.CreateInstance<LocalizationSettings>(); LocalizationSettings.SelectedLocale = Locale.CreateLocale("en"); LocalizationSettings.StringDatabase.TableProvider = new Provider();
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var ui = root.AddComponent<AlwaysUi>(); AlwaysUi.Instance = ui; var popup = root.AddComponent<UiTextPopup>(); ui.UiTextPopup = popup;
        popup.parent = new GameObject("popup", typeof(RectTransform)); popup.parent.SetActive(false); popup.t_text = new GameObject("popup text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); popup.sfx = root.AddComponent<RandomSfx>();
        var obj = new GameObject("utility", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); obj.SetActive(false); var button = obj.AddComponent<MyButtonOffersUtility>();
        button.background = obj.GetComponent<Image>(); button.background.color = new Color(.6f,.4f,.2f,1f); button.disbaledOverlay = new GameObject("disabled");
        button.t_amount = new GameObject("amount", typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); button.t_price = new GameObject("price", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        button.StartHover(); Check(button.background.color == Color.Lerp(new Color(.6f,.4f,.2f,1f), Color.black, .2f), "hover color");
        button.StopHover(); Check(button.background.color == new Color(.6f,.4f,.2f,1f), "normal color");
        button.Disable(); Check(button.disbaledOverlay.activeSelf && button.GetButton().interactable, "disabled overlay keeps clickable");
        button.Enable(); Check(!button.disbaledOverlay.activeSelf && button.GetButton().interactable, "enabled overlay");
        button.SetAmount(1, 5); Check(button.disbaledOverlay.activeSelf && button.t_amount.text == "1" && button.t_price.text == "5", "unaffordable price");
        typeof(MyButtonOffersUtility).GetField("refreshedAtTime", Private).SetValue(button, Time.time - 1f);
        typeof(MyButtonOffersUtility).GetMethod("OnClick", Private).Invoke(button, null);
        Check(popup.parent.activeSelf && popup.t_text.color == MyColorUtility.interactDisabledOutlineColor && !string.IsNullOrEmpty(popup.t_text.text), "unaffordable popup");
        popup.parent.SetActive(false); button.SetAmount(0, 5); typeof(MyButtonOffersUtility).GetField("refreshedAtTime", Private).SetValue(button, Time.time - 1f);
        typeof(MyButtonOffersUtility).GetMethod("OnClick", Private).Invoke(button, null); Check(!popup.parent.activeSelf, "no affordability warning when no charges");
        button.t_price = null; button.SetAmount(1, 5); Check(button.disbaledOverlay.activeSelf, "missing price field remains safe and shows disabled state");
        Debug.Log("OffersUtilityButtonPortCheck passed: scene bindings, hover, disabled state and localized affordability popup");
    }
}
