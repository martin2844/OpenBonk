#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Optional item-grant UI. It only appears in the pause screen when an enabled
// Lua package explicitly opts in; item effects use the normal game inventory.
internal sealed class AndroidItemLab : MonoBehaviour
{
    private static AndroidItemLab instance;
    private GameObject canvasObject, panel;
    private RectTransform content;
    private Button opener, previous, next, close, bulk;
    private TextMeshProUGUI detail, status, pageLabel;
    private TMP_InputField search;
    private readonly List<GameObject> rows = new List<GameObject>();
    private readonly List<Button> rowButtons = new List<Button>();
    private readonly List<Button> filterButtons = new List<Button>();
    private readonly List<Button> quantityButtons = new List<Button>();
    private readonly List<Selectable> controls = new List<Selectable>();
    private readonly List<ItemData> filtered = new List<ItemData>();
    private PauseUi linkedPause;
    private Button linkedStart;
    private Navigation savedNavigation;
    private ItemData selected;
    private EItemRarity? rarity;
    private int page;
    private bool open;
    private const int PageSize = 6;
    private static readonly Color Background = new Color32(8, 23, 27, 249);
    private static readonly Color Card = new Color32(35, 45, 41, 255);
    private static readonly Color ButtonColor = new Color32(67, 79, 68, 255);
    private static readonly Color Ink = new Color32(245, 239, 219, 255);
    private static readonly Color Accent = new Color32(155, 218, 159, 255);
    private static readonly Color FilterSelected = new Color32(66, 118, 77, 255);
    private bool Spanish => (LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en").StartsWith("es", StringComparison.OrdinalIgnoreCase);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var root = new GameObject("OpenBonk item lab"); DontDestroyOnLoad(root); instance = root.AddComponent<AndroidItemLab>();
    }

    // UiManager owns the pause/cancel input. Consume it while the modal is
    // open so a controller Back press cannot accidentally resume the run.
    internal static bool TryHandleCancel()
    {
        if (instance == null || !instance.open) return false;
        if (!MyInputManager.GetButtonDown(MyInputManager.UICancel) && !MyInputManager.GetButtonDown(MyInputManager.UIAbort)) return false;
        if (!TouchScreenKeyboard.visible) instance.Hide();
        return true;
    }

    private void Update()
    {
        var pause = UiManager.Instance != null ? UiManager.Instance.pause : null;
        bool available = OpenBonkItemGrantPolicy.Enabled && GameManager.Instance != null && GameManager.Instance.IsPlaying() &&
            MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && DataManager.Instance != null &&
            pause != null && pause.IsPaused() && pause.main != null && pause.main.activeInHierarchy;
        if (!available)
        {
            if (open) Hide();
            if (opener != null) opener.gameObject.SetActive(false);
            Unlink();
            return;
        }
        if (canvasObject == null) Build();
        LinkPause(pause);
        opener.gameObject.SetActive(!open);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        Unlink();
        if (canvasObject != null) Destroy(canvasObject);
    }

    private void LinkPause(PauseUi pause)
    {
        var start = pause != null && pause.mainWindow != null && pause.mainWindow.startBtn != null ? pause.mainWindow.startBtn.GetButton() : null;
        if (pause == linkedPause && start == linkedStart) return;
        Unlink(); linkedPause = pause; linkedStart = start;
        if (start == null) return;
        savedNavigation = start.navigation;
        var nav = savedNavigation;
        if (nav.mode == Navigation.Mode.Automatic)
        {
            nav.selectOnUp = start.FindSelectableOnUp(); nav.selectOnDown = start.FindSelectableOnDown(); nav.selectOnRight = start.FindSelectableOnRight();
        }
        nav.mode = Navigation.Mode.Explicit; nav.selectOnLeft = opener; start.navigation = nav;
        var back = opener.navigation; back.mode = Navigation.Mode.Explicit; back.selectOnRight = start; opener.navigation = back;
    }

    private void Unlink()
    {
        if (linkedStart != null) linkedStart.navigation = savedNavigation;
        linkedStart = null; linkedPause = null;
    }

    private void Build()
    {
        canvasObject = new GameObject("Item lab canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 450;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f;
        opener = MakeButton(canvasObject.transform, Spanish ? "Ítems" : "Items", new Vector2(225, 145), new Vector2(300, 82), Show);
        panel = new GameObject("Item lab modal", typeof(RectTransform), typeof(Image)); panel.SetActive(false); panel.transform.SetParent(canvasObject.transform, false);
        var p = (RectTransform)panel.transform; p.anchorMin = Vector2.zero; p.anchorMax = Vector2.one; p.offsetMin = p.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Background;
        panel.AddComponent<Window>();
        var contentObject = new GameObject("Centered content", typeof(RectTransform)); contentObject.transform.SetParent(panel.transform, false);
        content = (RectTransform)contentObject.transform; content.anchorMin = content.anchorMax = new Vector2(.5f, .5f); content.sizeDelta = new Vector2(1920, 1080); content.anchoredPosition = Vector2.zero;
        var card = ImageObject(content, "Card", new Vector2(960, 545), new Vector2(1660, 980), Card); card.raycastTarget = false;
        AddText(content, Spanish ? "Laboratorio de ítems" : "Item lab", new Vector2(610, 985), new Vector2(790, 76), 51, Ink, TextAlignmentOptions.Left);
        AddText(content, Spanish ? "Añadir ítems puede afectar logros y progreso." : "Adding items can affect achievements and progress.", new Vector2(960, 925), new Vector2(1240, 48), 25, Ink, TextAlignmentOptions.Center);
        search = MakeSearch(content, new Vector2(760, 860), new Vector2(960, 64));
        search.onValueChanged.AddListener(_ => { page = 0; RebuildRows(false); });
        var filterValues = new EItemRarity?[] { null, EItemRarity.Common, EItemRarity.Rare, EItemRarity.Epic, EItemRarity.Legendary };
        string[] labels = Spanish ? new[] { "Todos", "Común", "Raro", "Épico", "Legendario" } : new[] { "All", "Common", "Rare", "Epic", "Legendary" };
        controls.Add(search);
        for (int i = 0; i < filterValues.Length; i++)
        {
            var value = filterValues[i];
            var button = MakeButton(content, labels[i], new Vector2(405 + i * 170, 780), new Vector2(155, 62), () => { rarity = value; page = 0; RebuildRows(true); });
            controls.Add(button); filterButtons.Add(button);
        }
        bulk = MakeButton(content, Spanish ? "+1 filtrados" : "+1 filtered", new Vector2(1390, 780), new Vector2(290, 62), GrantFiltered); controls.Add(bulk);
        detail = AddText(content, "", new Vector2(1400, 625), new Vector2(440, 100), 32, Ink, TextAlignmentOptions.Center);
        int[] quantities = { 1, 5, 10, 50 };
        for (int i = 0; i < quantities.Length; i++)
        {
            int amount = quantities[i];
            var button = MakeButton(content, "+" + amount, new Vector2(1290 + (i % 2) * 220, 520 - (i / 2) * 85), new Vector2(200, 68), () => GrantSelected(amount));
            controls.Add(button); quantityButtons.Add(button);
        }
        status = AddText(content, "", new Vector2(1385, 300), new Vector2(480, 110), 24, Accent, TextAlignmentOptions.Center);
        previous = MakeButton(content, Spanish ? "Anterior" : "Previous", new Vector2(500, 185), new Vector2(240, 70), () => { if (page > 0) { page--; RebuildRows(true); } });
        next = MakeButton(content, Spanish ? "Siguiente" : "Next", new Vector2(800, 185), new Vector2(240, 70), () => { if ((page + 1) * PageSize < filtered.Count) { page++; RebuildRows(true); } });
        close = MakeButton(content, Spanish ? "Volver" : "Back", new Vector2(1390, 185), new Vector2(280, 70), Hide);
        pageLabel = AddText(content, "", new Vector2(650, 115), new Vector2(510, 46), 24, Ink, TextAlignmentOptions.Center);
        controls.Add(previous); controls.Add(next); controls.Add(close);
    }

    private void Show()
    {
        if (!OpenBonkItemGrantPolicy.Enabled || MyPlayer.Instance?.inventory?.itemInventory == null) return;
        open = true; rarity = null; page = 0; selected = null; status.text = ""; search.SetTextWithoutNotify("");
        opener.gameObject.SetActive(false); panel.SetActive(true); RebuildRows(true);
    }

    private void Hide()
    {
        open = false;
        if (panel != null) panel.SetActive(false);
        if (opener != null && linkedPause != null && linkedPause.IsPaused())
        { opener.gameObject.SetActive(true); EventSystem.current?.SetSelectedGameObject(opener.gameObject); }
    }

    private static string NameOf(ItemData item)
    {
        if (item == null) return "";
        try { var name = item.GetName(); if (!string.IsNullOrWhiteSpace(name)) return name; } catch (Exception) { }
        return item.eItem.ToString();
    }

    private void RebuildRows(bool selectFirst)
    {
        if (status != null) status.text = "";
        foreach (var row in rows) if (row != null) Destroy(row);
        rows.Clear(); rowButtons.Clear(); filtered.Clear();
        var seen = new HashSet<EItem>();
        string query = search != null ? (search.text ?? "").Trim() : "";
        if (DataManager.Instance != null && DataManager.Instance.unsortedItems != null)
            foreach (var item in DataManager.Instance.unsortedItems)
            {
                if (item == null || !item.isEnabled || !seen.Add(item.eItem) ||
                    !DataManager.Instance.TryGetItem(item.eItem, out var registered) || registered != item ||
                    (rarity.HasValue && item.rarity != rarity.Value)) continue;
                string name = NameOf(item);
                if (query.Length > 0 && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    item.eItem.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                filtered.Add(item);
            }
        filtered.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(NameOf(a), NameOf(b)));
        page = Math.Max(0, Math.Min(page, Math.Max(0, (filtered.Count - 1) / PageSize)));
        for (int i = page * PageSize; i < Math.Min(filtered.Count, (page + 1) * PageSize); i++)
        {
            var item = filtered[i];
            string label = NameOf(item) + "  [" + item.rarity + "]";
            var b = MakeButton(content, label, new Vector2(700, 670 - (i - page * PageSize) * 78), new Vector2(850, 66), () => { selected = item; status.text = ""; UpdateDetail(); });
            var text = b.GetComponentInChildren<TextMeshProUGUI>(); text.alignment = TextAlignmentOptions.MidlineLeft; text.margin = new Vector4(28, 0, 20, 0);
            text.color = item.GetColor(); rows.Add(b.gameObject); rowButtons.Add(b);
        }
        if (filtered.Count == 0)
        {
            var empty = AddText(content, Spanish ? "No hay ítems que coincidan" : "No matching items", new Vector2(700, 515), new Vector2(850, 60), 30, Ink, TextAlignmentOptions.Center);
            rows.Add(empty.gameObject);
        }
        if (selected == null || !filtered.Contains(selected)) selected = filtered.Count > 0 ? filtered[0] : null;
        previous.interactable = page > 0; next.interactable = (page + 1) * PageSize < filtered.Count;
        bulk.interactable = filtered.Count > 0;
        bulk.GetComponentInChildren<TextMeshProUGUI>().text = (Spanish ? "+1 filtrados" : "+1 filtered") + " (" + filtered.Count + ")";
        var filterValues = new EItemRarity?[] { null, EItemRarity.Common, EItemRarity.Rare, EItemRarity.Epic, EItemRarity.Legendary };
        for (int i = 0; i < filterButtons.Count; i++) filterButtons[i].image.color = rarity == filterValues[i] ? FilterSelected : ButtonColor;
        pageLabel.text = (Spanish ? "Página " : "Page ") + (page + 1) + " / " + Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
        UpdateDetail(); search.transform.SetAsLastSibling(); Relink(selectFirst);
    }

    private void Relink(bool selectFirst)
    {
        var all = new List<Selectable>();
        foreach (var control in controls) if (control != null && control.IsInteractable()) all.Add(control);
        // Rows follow the search field so a controller can reach the list and
        // every action without relying on spatial navigation across overlays.
        all.InsertRange(Math.Min(1, all.Count), rowButtons);
        for (int i = 0; i < all.Count; i++)
        {
            var nav = all[i].navigation; nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = nav.selectOnLeft = all[(i + all.Count - 1) % all.Count];
            nav.selectOnDown = nav.selectOnRight = all[(i + 1) % all.Count]; all[i].navigation = nav;
        }
        if (selectFirst && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(rowButtons.Count > 0 ? rowButtons[0].gameObject : close.gameObject);
    }

    private void UpdateDetail()
    {
        if (detail == null) return;
        foreach (var button in quantityButtons) if (button != null) button.interactable = selected != null;
        int amount = selected != null && MyPlayer.Instance?.inventory?.itemInventory != null ? MyPlayer.Instance.inventory.itemInventory.GetAmount(selected.eItem) : 0;
        detail.text = selected == null ? (Spanish ? "Elegí un ítem" : "Choose an item") : NameOf(selected) + "\n" + (Spanish ? "En partida: " : "In run: ") + amount;
    }

    private void GrantSelected(int amount)
    {
        if (selected == null || MyPlayer.Instance?.inventory?.itemInventory == null) return;
        bool ok = OpenBonkItemGrantPolicy.TryGrant(MyPlayer.Instance.inventory.itemInventory, DataManager.Instance, selected.eItem, amount, out int delivered);
        status.text = ok ? (Spanish ? "Añadidos: " : "Added: ") + delivered : (Spanish ? "No se pudo completar. Añadidos: " : "Could not complete. Added: ") + delivered;
        UpdateDetail();
    }

    private void GrantFiltered()
    {
        if (MyPlayer.Instance?.inventory?.itemInventory == null) return;
        int total = 0, failed = 0;
        foreach (var item in filtered)
        {
            bool ok = OpenBonkItemGrantPolicy.TryGrant(MyPlayer.Instance.inventory.itemInventory, DataManager.Instance, item.eItem, 1, out int count);
            total += count; if (!ok) failed++;
        }
        status.text = (Spanish ? "Añadidos: " : "Added: ") + total + (failed == 0 ? "" : (Spanish ? "; omitidos: " : "; skipped: ") + failed);
        UpdateDetail();
    }

    private static Image ImageObject(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = go.GetComponent<Image>(); image.color = color; return image;
    }
    private static TextMeshProUGUI AddText(Transform parent, string text, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.text = text; label.fontSize = fontSize; label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = fontSize; label.alignment = align; label.color = color; label.raycastTarget = false; return label;
    }
    private static Button MakeButton(Transform parent, string text, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var image = ImageObject(parent, text, position, size, ButtonColor); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var border = image.gameObject.AddComponent<UnityEngine.UI.Outline>(); border.effectColor = new Color32(177, 187, 165, 180); border.effectDistance = new Vector2(1.5f, -1.5f);
        var colors = button.colors; colors.highlightedColor = Accent; colors.selectedColor = Accent; colors.pressedColor = Color.white; button.colors = colors; button.onClick.AddListener(action);
        var label = AddText(image.transform, text, Vector2.zero, size - new Vector2(20, 6), 29, Ink, TextAlignmentOptions.Center);
        var rect = (RectTransform)label.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
        return button;
    }
    private TMP_InputField MakeSearch(Transform parent, Vector2 position, Vector2 size)
    {
        var image = ImageObject(parent, "Search", position, size, new Color32(19, 26, 24, 255));
        var field = image.gameObject.AddComponent<TMP_InputField>(); field.targetGraphic = image; field.lineType = TMP_InputField.LineType.SingleLine;
        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewportObject.transform.SetParent(image.transform, false);
        var viewport = (RectTransform)viewportObject.transform; viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.offsetMin = new Vector2(20, 5); viewport.offsetMax = new Vector2(-20, -5);
        var label = AddText(viewport, "", Vector2.zero, Vector2.zero, 28, Ink, TextAlignmentOptions.MidlineLeft);
        var labelRect = (RectTransform)label.transform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var placeholder = AddText(viewport, Spanish ? "Buscar ítem…" : "Search items…", Vector2.zero, Vector2.zero, 28, new Color32(161, 178, 164, 255), TextAlignmentOptions.MidlineLeft);
        var phRect = (RectTransform)placeholder.transform; phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one; phRect.offsetMin = phRect.offsetMax = Vector2.zero;
        field.textViewport = viewport; field.textComponent = label; field.placeholder = placeholder; field.characterLimit = 60; return field;
    }
}
#endif
