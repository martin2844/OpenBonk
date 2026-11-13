#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Small pre-run manager, attached only to the main-menu scene.
internal sealed class AndroidModsMenu : MonoBehaviour
{
    private MainMenu menu;
    private GameObject canvasObject, panel;
    private Button opener, play;
    private Navigation originalPlayNavigation;
    private readonly List<GameObject> rows = new List<GameObject>();
    private List<AndroidLuaMods.ModInfo> mods;
    private int page;
    private bool open;
    private string status;
    private bool Spanish => (LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en").StartsWith("es", StringComparison.OrdinalIgnoreCase);

    private void Start()
    {
        menu = GetComponent<MainMenu>();
        if (menu == null || menu.btnPlay == null) { enabled = false; return; }
        play = menu.btnPlay.GetButton();
        if (play == null) { enabled = false; return; }
        canvasObject = new GameObject("Mods menu canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 400;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        opener = MakeButton(canvasObject.transform, "Mods", new Vector2(1200, 100), new Vector2(330, 80), Show);
        originalPlayNavigation = play.navigation;
        var nav = originalPlayNavigation;
        if (nav.mode == Navigation.Mode.Automatic)
        {
            nav.selectOnUp = play.FindSelectableOnUp(); nav.selectOnDown = play.FindSelectableOnDown();
            nav.selectOnLeft = play.FindSelectableOnLeft();
        }
        nav.mode = Navigation.Mode.Explicit; nav.selectOnRight = opener; play.navigation = nav;
        var openerNav = opener.navigation; openerNav.mode = Navigation.Mode.Explicit; openerNav.selectOnLeft = play; opener.navigation = openerNav;
        panel = new GameObject("Mods panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.02f, .07f, .08f, .95f);
        var card = new GameObject("Mods background", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(panel.transform, false);
        var cardRect = (RectTransform)card.transform; cardRect.anchorMin = cardRect.anchorMax = Vector2.zero;
        cardRect.anchoredPosition = new Vector2(960, 540); cardRect.sizeDelta = new Vector2(1160, 1000);
        card.GetComponent<Image>().color = new Color(.07f, .14f, .13f, 1f);
        card.GetComponent<Image>().raycastTarget = false;
        panel.SetActive(false);
        Debug.Log($"Mods menu initialized: tab={(menu.tabMenu != null && menu.tabMenu.activeInHierarchy)} settings={(menu.settings != null && menu.settings.activeInHierarchy)} credits={(menu.credits != null && menu.credits.activeInHierarchy)}");
    }

    private void Update()
    {
        if (canvasObject == null) return;
        bool warningAccepted = SaveManager.loaded && SaveManager.Instance?.config?.otherSettings != null &&
                               SaveManager.Instance.config.otherSettings.hasAcceptedPhotoSensitivity;
        bool atMain = warningAccepted && menu != null && menu.tabMenu != null && menu.tabMenu.activeInHierarchy &&
                      (menu.settings == null || !menu.settings.activeInHierarchy) &&
                      (menu.credits == null || !menu.credits.activeInHierarchy);
        if (!atMain && open) Hide();
        opener.gameObject.SetActive(atMain && !open);
        if (open && MyInputManager.GetButtonDown(MyInputManager.UIAbort)) Hide();
    }

    private void OnDestroy()
    {
        if (play != null) play.navigation = originalPlayNavigation;
        if (canvasObject != null) Destroy(canvasObject);
    }

    private void Show()
    {
        mods = AndroidLuaMods.ListMods(); page = 0; open = true; status = null;
        opener.gameObject.SetActive(false); panel.SetActive(true); Draw();
    }

    private void Hide()
    {
        open = false;
        if (panel != null) panel.SetActive(false);
        if (opener != null) { opener.gameObject.SetActive(true); EventSystem.current?.SetSelectedGameObject(opener.gameObject); }
    }

    private void Draw(int selectedRow = 0)
    {
        foreach (var row in rows) Destroy(row);
        rows.Clear();
        AddText(Spanish ? "MODS" : "MODS", new Vector2(960, 940), new Vector2(900, 90), 55, new Color(.95f, .94f, .80f));
        AddText(Spanish ? "Los cambios se aplican al iniciar una partida" : "Changes apply when starting a run", new Vector2(960, 860), new Vector2(1100, 55), 27, new Color(.66f, .82f, .78f));
        if (mods.Count == 0) AddText(Spanish ? "No hay mods instalados" : "No mods installed", new Vector2(960, 600), new Vector2(1000, 75), 35, Color.white);
        if (!string.IsNullOrEmpty(status)) AddText(status, new Vector2(960, 235), new Vector2(1050, 50), 22, new Color(.83f, .88f, .72f));
        var modButtons = new List<Button>();
        for (int n = page * 6; n < Math.Min(mods.Count, page * 6 + 6); n++)
        {
            var mod = mods[n];
            int rowIndex = n - page * 6;
            string label = mod.id + "    " + (mod.enabled ? (Spanish ? "Activado" : "On") : (Spanish ? "Desactivado" : "Off"));
            Button b = AddButton(label, new Vector2(960, 760 - (n - page * 6) * 87), new Vector2(950, 70), () => {
                mod.enabled = !mod.enabled; AndroidLuaMods.SetEnabled(mod.id, mod.enabled); Draw(rowIndex);
            });
            modButtons.Add(b);
        }
        Button previous = page > 0 ? AddButton(Spanish ? "Anterior" : "Previous", new Vector2(620, 155), new Vector2(260, 70), () => { page--; Draw(); }) : null;
        Button next = (page + 1) * 6 < mods.Count ? AddButton(Spanish ? "Siguiente" : "Next", new Vector2(1300, 155), new Vector2(260, 70), () => { page++; Draw(); }) : null;
        Button close = AddButton(Spanish ? "Volver" : "Back", new Vector2(960, 155), new Vector2(260, 70), Hide);
        Button import = AddButton(Spanish ? "Importar ZIP" : "Import ZIP", new Vector2(960, 70), new Vector2(310, 60), Import);
        for (int i = 0; i < modButtons.Count; i++)
            Link(modButtons[i], i > 0 ? modButtons[i - 1] : null, i + 1 < modButtons.Count ? modButtons[i + 1] : close, null, null);
        Button last = modButtons.Count > 0 ? modButtons[modButtons.Count - 1] : null;
        Link(previous, last, import, null, close);
        Link(close, last, import, previous, next);
        Link(next, last, import, close, null);
        Link(import, close, null, null, null);
        Button selected = modButtons.Count == 0 ? close : modButtons[Mathf.Clamp(selectedRow, 0, modButtons.Count - 1)];
        EventSystem.current?.SetSelectedGameObject(selected.gameObject);
    }

    private void Import()
    {
        if (GetComponent<AndroidModImportReceiver>() == null) gameObject.AddComponent<AndroidModImportReceiver>().menu = this;
        var receiver = GetComponent<AndroidModImportReceiver>();
        receiver.EnsureObject();
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
                try
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var current = player.GetStatic<AndroidJavaObject>("currentActivity");
                    using var picker = new AndroidJavaClass("com.megabonk.modpicker.ModPickerActivity");
                    using var intent = new AndroidJavaObject("android.content.Intent", current, picker);
                    current.Call("startActivity", intent);
                }
                catch (Exception e) { Debug.LogError("Mod picker failed: " + e.Message); }
            }));
        }
    }

    internal void OnImportResult(string result)
    {
        if (result == "cancel") return;
        string staged = null;
        try
        {
            if (result != "ok") throw new IOException(result);
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var cache = activity.Call<AndroidJavaObject>("getCacheDir"))
                staged = Path.Combine(cache.Call<string>("getAbsolutePath"), "megabonk-mod-import.zip");
            string id;
            using (var file = File.OpenRead(staged)) id = AndroidModPackageImporter.Install(file, Path.Combine(Application.persistentDataPath, "Mods"));
            AndroidLuaMods.SetEnabled(id, false);
            status = Spanish ? "Importado y desactivado: " + id : "Imported and disabled: " + id;
            mods = AndroidLuaMods.ListMods();
            int index = mods.FindIndex(mod => mod.id == id);
            if (index >= 0) page = index / 6;
        }
        catch (Exception e) { status = (Spanish ? "No se pudo importar: " : "Import failed: ") + e.Message; }
        finally { if (staged != null && File.Exists(staged)) File.Delete(staged); }
        if (open) Draw();
    }

    private static void Link(Button button, Selectable up, Selectable down, Selectable left, Selectable right)
    {
        if (button == null) return;
        var nav = button.navigation; nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = up; nav.selectOnDown = down; nav.selectOnLeft = left; nav.selectOnRight = right;
        button.navigation = nav;
    }

    private void AddText(string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(panel.transform, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.text = text; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.color = color; label.raycastTarget = false;
        rows.Add(go);
    }

    private Button AddButton(string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var button = MakeButton(panel.transform, text, position, size, action); rows.Add(button.gameObject); return button;
    }

    private static Button MakeButton(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = go.GetComponent<Image>(); image.color = new Color(.18f, .28f, .27f, 1f);
        var button = go.GetComponent<Button>(); button.targetGraphic = image; var colors = button.colors; colors.highlightedColor = new Color(.73f, 1f, .85f); colors.selectedColor = colors.highlightedColor; button.colors = colors; button.onClick.AddListener(action);
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); labelGo.transform.SetParent(go.transform, false);
        var labelRect = (RectTransform)labelGo.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelGo.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.text = text; label.fontSize = 32; label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 32; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return button;
    }
}
#endif
