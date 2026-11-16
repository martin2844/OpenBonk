#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Optional Android-only overlay. It uses the same gameplay actions as the
// controller and disappears while the game's own menus receive touch input.
internal sealed class AndroidTouchControls : MonoBehaviour
{
    private GameObject overlay;
    private Sprite circle;
    private bool visible;
    private bool forceTest;
    private bool appFocused = true;
    private bool appPaused;
    private string locale;
    private AndroidTouchButton[] buttons;
    private AndroidTouchLayoutEditor layoutEditor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var root = new GameObject("Android touch controls");
        DontDestroyOnLoad(root);
        var controls = root.AddComponent<AndroidTouchControls>();
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        controls.forceTest = intent.Call<bool>("getBooleanExtra", "megabonk_touch_controls_probe", false);
    }

    private void Update()
    {
        bool enabled = forceTest || (SaveManager.Instance?.config?.cfControlSettings != null &&
                       SaveManager.Instance.config.cfControlSettings.touch_controls != 0);
        bool canPlay = appFocused && !appPaused && GameManager.Instance != null && GameManager.Instance.IsPlaying() &&
                       !GameManager.Instance.cutscene && MyPlayer.Instance != null &&
                       MyPlayer.Instance.hasStarted && !MyPlayer.Instance.IsDead() && !MyTime.paused &&
                       UiManager.Instance != null && UiManager.Instance.encounterWindows != null &&
                       !UiManager.Instance.encounterWindows.HasEncounter();
        bool shouldShow = enabled && canPlay;
        if (shouldShow && overlay == null) CreateOverlay();
        if (visible != shouldShow)
        {
            visible = shouldShow;
            if (!visible) AndroidTouchInput.ReleaseAll();
            if (overlay != null) overlay.SetActive(visible);
        }
        if (overlay != null)
        {
            string currentLocale = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
            if (locale != currentLocale)
            {
                locale = currentLocale;
                foreach (var button in buttons) button.RefreshLabel();
            }
        }
    }

    private void OnApplicationPause(bool paused)
    {
        appPaused = paused;
        if (paused) SuspendOverlay();
    }

    private void OnApplicationFocus(bool focused)
    {
        appFocused = focused;
        if (!focused) SuspendOverlay();
    }

    private void SuspendOverlay()
    {
        AndroidTouchInput.ReleaseAll();
        // Disabling invokes each control's OnDisable, clearing its pointer ID.
        // Android may never send pointer-up after an interruption.
        visible = false;
        if (overlay != null) overlay.SetActive(false);
    }

    private void OnDestroy()
    {
        AndroidTouchInput.ReleaseAll();
        if (circle != null) { Destroy(circle.texture); Destroy(circle); }
    }

    private void CreateOverlay()
    {
        circle = MakeCircle();
        overlay = new GameObject("Touch overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var safe = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
        safe.SetParent(overlay.transform, false);
        Rect area = Screen.safeArea;
        safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        safe.offsetMin = safe.offsetMax = Vector2.zero;

        CreateStick(safe, "Move", new Vector2(190, 175), false);
        CreateStick(safe, "Look", new Vector2(-390, 175), true);
        buttons = new[] {
            CreateButton(safe, "Jump", new Vector2(-105, 155), 150),
            CreateButton(safe, "Slide", new Vector2(-105, 325), 135),
            CreateButton(safe, "Interact", new Vector2(-260, 400), 135),
            CreateButton(safe, "Aim", new Vector2(-105, 475), 125),
            CreateButton(safe, "MapOverlay", new Vector2(-420, 400), 110),
            CreateButton(safe, "Pause", new Vector2(-700, -75), 90, true)
        };
        Canvas.ForceUpdateCanvases();
        layoutEditor = overlay.AddComponent<AndroidTouchLayoutEditor>();
        layoutEditor.Initialize(safe, buttons);
        locale = null;
        overlay.SetActive(false);
    }

    private void CreateStick(RectTransform parent, string name, Vector2 position, bool rightAnchor)
    {
        var baseRect = CreateImage(parent, name, position, 245, rightAnchor, false);
        baseRect.GetComponent<Image>().color = new Color(.08f, .12f, .14f, .46f);
        var knob = CreateImage(baseRect, "Knob", Vector2.zero, 100, false, false);
        knob.anchorMin = knob.anchorMax = knob.pivot = new Vector2(.5f, .5f);
        knob.GetComponent<Image>().color = new Color(.88f, .94f, .82f, .48f);
        knob.GetComponent<Image>().raycastTarget = false;
        var stick = baseRect.gameObject.AddComponent<AndroidTouchStick>();
        stick.look = name == "Look";
        stick.knob = knob;
    }

    private AndroidTouchButton CreateButton(RectTransform parent, string action, Vector2 position, float size, bool top = false)
    {
        var rect = CreateImage(parent, action, position, size, true, top);
        rect.GetComponent<Image>().color = action == "Interact" ? new Color(.20f, .41f, .45f, .72f) : new Color(.10f, .15f, .17f, .64f);
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = action == "Interact" ? 25 : 29;
        label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = label.fontSize;
        label.alignment = TextAlignmentOptions.Center; label.color = new Color(.96f, .97f, .90f, 1f);
        label.raycastTarget = false;
        var button = rect.gameObject.AddComponent<AndroidTouchButton>();
        button.action = action; button.label = label;
        return button;
    }

    private RectTransform CreateImage(RectTransform parent, string name, Vector2 position, float size, bool right, bool top)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(right ? 1 : 0, top ? 1 : 0);
        rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(size, size);
        go.GetComponent<Image>().sprite = circle;
        return rect;
    }

    private static Sprite MakeCircle()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Touch circle", filterMode = FilterMode.Bilinear };
        var colors = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f));
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(size / 2f - distance) * 255f);
            colors[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(colors); texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
    }
}

internal sealed class AndroidTouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    internal bool look;
    internal RectTransform knob;
    private int pointer = int.MinValue;
    private RectTransform Rect => (RectTransform)transform;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (AndroidTouchLayoutEditor.Editing) return;
        if (pointer != int.MinValue) return;
        pointer = eventData.pointerId;
        UpdatePosition(eventData);
    }
    public void OnDrag(PointerEventData eventData) { if (!AndroidTouchLayoutEditor.Editing && eventData.pointerId == pointer) UpdatePosition(eventData); }
    public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == pointer) Release(); }
    private void OnDisable() { Release(); }
    internal void ResetPointer() { Release(); }

    private void UpdatePosition(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, eventData.position, eventData.pressEventCamera, out var local)) return;
        float radius = Mathf.Max(1f, Rect.rect.width * .35f);
        Vector2 value = Vector2.ClampMagnitude(local / radius, 1f);
        if (look) AndroidTouchInput.Look = value; else AndroidTouchInput.Move = value;
        if (knob != null) knob.anchoredPosition = value * radius;
    }
    private void Release()
    {
        pointer = int.MinValue;
        if (look) AndroidTouchInput.Look = Vector2.zero; else AndroidTouchInput.Move = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }
}

internal sealed class AndroidTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    internal string action;
    internal TextMeshProUGUI label;
    private int pointer = int.MinValue;

    internal void RefreshLabel()
    {
        label.text = action == "Pause" ? "II" : AndroidControlText.TouchAction(action) ?? string.Empty;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (AndroidTouchLayoutEditor.Editing) return;
        if (pointer != int.MinValue) return;
        pointer = eventData.pointerId;
        if (action == "Pause")
        {
            var pause = UnityEngine.Object.FindObjectOfType<PauseUi>(true);
            if (pause != null) pause.Pause();
        }
        else AndroidTouchInput.SetButton(action, true);
    }
    public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == pointer) Release(); }
    private void OnDisable() { Release(); }
    internal void ResetPointer() { Release(); }
    private void Release()
    {
        pointer = int.MinValue;
        if (action != "Pause") AndroidTouchInput.SetButton(action, false);
    }
}

// A small in-game editor is available while the touch overlay is visible.
// Positions are relative to the safe area, so they survive resolution changes.
internal sealed class AndroidTouchLayoutEditor : MonoBehaviour
{
    private const string Prefix = "openbonk.touch.layout.";
    internal static bool Editing { get; private set; }
    private RectTransform safe;
    private AndroidTouchButton[] buttons;
    private AndroidTouchLayoutTarget[] targets;
    private AndroidTouchLayoutTarget selected;
    private AndroidTouchButton swapFrom;
    private GameObject toolbar;
    private TextMeshProUGUI selectedLabel;
    private bool Spanish => (LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en").StartsWith("es", StringComparison.OrdinalIgnoreCase);

    internal void Initialize(RectTransform area, AndroidTouchButton[] actionButtons)
    {
        safe = area; buttons = actionButtons;
        var list = new System.Collections.Generic.List<AndroidTouchLayoutTarget>();
        foreach (var stick in safe.GetComponentsInChildren<AndroidTouchStick>()) AddTarget(list, (RectTransform)stick.transform, stick.name, null);
        foreach (var button in buttons)
        {
            string id = button.action;
            string mapped = PlayerPrefs.GetString(Prefix + id + ".action", id);
            if (IsRemappable(id) && IsRemappable(mapped)) button.action = mapped;
            AddTarget(list, (RectTransform)button.transform, id, button);
        }
        targets = list.ToArray();
        MakeButton(safe, "Customize", new Vector2(92, -58), new Vector2(166, 76), () => StartEditing(), false);
        toolbar = new GameObject("Touch layout toolbar", typeof(RectTransform));
        var bar = (RectTransform)toolbar.transform; bar.SetParent(safe, false);
        bar.anchorMin = bar.anchorMax = new Vector2(.5f, 1); bar.pivot = new Vector2(.5f, 1);
        bar.anchoredPosition = new Vector2(0, -12); bar.sizeDelta = new Vector2(1000, 160);
        MakeButton(bar, "Done", new Vector2(-370, -45), new Vector2(155, 76), FinishEditing, true);
        MakeButton(bar, "Reset", new Vector2(-195, -45), new Vector2(155, 76), ResetLayout, true);
        MakeButton(bar, "−", new Vector2(-20, -45), new Vector2(110, 76), () => Resize(.9f), true);
        MakeButton(bar, "+", new Vector2(110, -45), new Vector2(110, 76), () => Resize(1.1f), true);
        MakeButton(bar, "Swap", new Vector2(260, -45), new Vector2(150, 76), StartSwap, true);
        var text = new GameObject("Selection", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)text.transform; rect.SetParent(bar, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -120); rect.sizeDelta = new Vector2(900, 60);
        selectedLabel = text.GetComponent<TextMeshProUGUI>(); selectedLabel.font = TMP_Settings.defaultFontAsset;
        selectedLabel.fontSize = 25; selectedLabel.alignment = TextAlignmentOptions.Center; selectedLabel.raycastTarget = false;
        toolbar.SetActive(false);
    }

    private void AddTarget(System.Collections.Generic.List<AndroidTouchLayoutTarget> list, RectTransform rect, string id, AndroidTouchButton button)
    {
        var target = rect.gameObject.AddComponent<AndroidTouchLayoutTarget>(); target.Setup(this, safe, id, button, Prefix); list.Add(target);
    }
    private static bool IsRemappable(string action) => action == "Jump" || action == "Slide" || action == "Interact" || action == "Aim" || action == "MapOverlay";
    private void StartEditing()
    {
        foreach (var stick in safe.GetComponentsInChildren<AndroidTouchStick>()) stick.ResetPointer();
        foreach (var button in buttons) button.ResetPointer();
        AndroidTouchInput.ReleaseAll(); Editing = true; selected = null; swapFrom = null; toolbar.SetActive(true); Refresh();
    }
    private void FinishEditing() { Editing = false; swapFrom = null; toolbar.SetActive(false); AndroidTouchInput.ReleaseAll(); PlayerPrefs.Save(); }
    private void OnDisable() { if (Editing) FinishEditing(); }
    private void OnDestroy() { Editing = false; }
    internal void Select(AndroidTouchLayoutTarget target)
    {
        selected = target;
        if (swapFrom != null && target.Button != null && target.Button != swapFrom && IsRemappable(target.Button.action))
        {
            string action = swapFrom.action; swapFrom.action = target.Button.action; target.Button.action = action;
            foreach (var t in targets) if (t.Button != null && IsRemappable(t.Id)) PlayerPrefs.SetString(Prefix + t.Id + ".action", t.Button.action);
            foreach (var b in buttons) b.RefreshLabel();
            swapFrom = null;
        }
        Refresh();
    }
    private void StartSwap() { swapFrom = selected?.Button; if (swapFrom != null && !IsRemappable(swapFrom.action)) swapFrom = null; Refresh(); }
    private void Resize(float factor) { if (selected != null) selected.Resize(factor); Refresh(); }
    private void ResetLayout()
    {
        swapFrom = null; foreach (var target in targets) target.ResetLayout();
        foreach (var button in buttons) button.RefreshLabel(); PlayerPrefs.Save(); Refresh();
    }
    private void Refresh()
    {
        if (selectedLabel == null) return;
        selectedLabel.text = swapFrom != null ? (Spanish ? "Toca otro botón para intercambiar" : "Tap another button to swap") :
            (selected == null ? (Spanish ? "Arrastra un control para moverlo" : "Drag a control to move it") :
            (AndroidControlText.TouchAction(selected.Button?.action) ?? selected.Id) + "  ·  " + (Spanish ? "Tamaño" : "Size") + " " + Mathf.RoundToInt(selected.Scale * 100) + "%");
    }
    private void MakeButton(RectTransform parent, string id, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click, bool center)
    {
        var obj = new GameObject(id, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = center ? new Vector2(.5f, 1) : new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(.06f, .1f, .12f, .92f); obj.GetComponent<Button>().onClick.AddListener(click);
        var child = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); var tr = (RectTransform)child.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        var label = child.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.fontSize = 24; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.text = Spanish ? (id == "Customize" ? "Editar" : id == "Done" ? "Listo" : id == "Reset" ? "Reiniciar" : id == "Swap" ? "Cambiar" : id) : (id == "Customize" ? "Edit" : id);
    }
}

internal sealed class AndroidTouchLayoutTarget : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    internal string Id { get; private set; }
    internal AndroidTouchButton Button { get; private set; }
    internal float Scale => ((RectTransform)transform).localScale.x;
    private AndroidTouchLayoutEditor editor;
    private RectTransform safe, rect;
    private string prefix;
    private Vector2 originalPosition, originalAnchor;
    private int pointer = int.MinValue;
    private Vector2 dragOffset;
    internal void Setup(AndroidTouchLayoutEditor owner, RectTransform area, string id, AndroidTouchButton button, string key)
    {
        editor = owner; safe = area; rect = (RectTransform)transform; Id = id; Button = button; prefix = key + id + ".";
        originalPosition = rect.anchoredPosition; originalAnchor = rect.anchorMin;
        float scale = PlayerPrefs.GetFloat(prefix + "scale", 1); if (float.IsNaN(scale) || float.IsInfinity(scale)) scale = 1;
        rect.localScale = Vector3.one * Mathf.Clamp(scale, .5f, 1.8f);
        if (PlayerPrefs.HasKey(prefix + "x") && PlayerPrefs.HasKey(prefix + "y"))
        {
            float x = PlayerPrefs.GetFloat(prefix + "x"), y = PlayerPrefs.GetFloat(prefix + "y");
            if (!float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y)) SetNormalized(new Vector2(x, y));
        }
    }
    public void OnPointerDown(PointerEventData e)
    {
        if (!AndroidTouchLayoutEditor.Editing || pointer != int.MinValue) return;
        pointer = e.pointerId; editor.Select(this);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, e.position, e.pressEventCamera, out var local)) dragOffset = (Vector2)safe.InverseTransformPoint(rect.position) - local;
    }
    public void OnDrag(PointerEventData e)
    {
        if (!AndroidTouchLayoutEditor.Editing || pointer != e.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, e.position, e.pressEventCamera, out var local)) return;
        Vector2 pos = local + dragOffset; Rect bounds = safe.rect;
        if (bounds.width <= 0 || bounds.height <= 0) return;
        SetNormalized(new Vector2((pos.x - bounds.xMin) / bounds.width, (pos.y - bounds.yMin) / bounds.height)); SavePosition();
    }
    public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointer) pointer = int.MinValue; }
    private void OnDisable() { pointer = int.MinValue; }
    private void SetNormalized(Vector2 value)
    {
        float width = safe.rect.width, height = safe.rect.height;
        if (width <= 0 || height <= 0) return;
        float halfX = Mathf.Min(.5f, rect.rect.width * Scale / (2f * width));
        float halfY = Mathf.Min(.5f, rect.rect.height * Scale / (2f * height));
        value = new Vector2(Mathf.Clamp(value.x, halfX, 1f - halfX), Mathf.Clamp(value.y, halfY, 1f - halfY)); rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(value.x * safe.rect.width, value.y * safe.rect.height);
    }
    private void SavePosition()
    {
        if (safe.rect.width <= 0 || safe.rect.height <= 0) return;
        PlayerPrefs.SetFloat(prefix + "x", Mathf.Clamp01(rect.anchoredPosition.x / safe.rect.width));
        PlayerPrefs.SetFloat(prefix + "y", Mathf.Clamp01(rect.anchoredPosition.y / safe.rect.height));
    }
    internal void Resize(float factor)
    {
        Vector2 local = safe.InverseTransformPoint(rect.position); Rect bounds = safe.rect;
        rect.localScale = Vector3.one * Mathf.Clamp(Scale * factor, .5f, 1.8f); PlayerPrefs.SetFloat(prefix + "scale", Scale);
        if (bounds.width > 0 && bounds.height > 0)
        {
            SetNormalized(new Vector2((local.x - bounds.xMin) / bounds.width, (local.y - bounds.yMin) / bounds.height)); SavePosition();
        }
    }
    internal void ResetLayout()
    {
        rect.anchorMin = rect.anchorMax = originalAnchor; rect.anchoredPosition = originalPosition; rect.localScale = Vector3.one;
        PlayerPrefs.DeleteKey(prefix + "x"); PlayerPrefs.DeleteKey(prefix + "y"); PlayerPrefs.DeleteKey(prefix + "scale"); PlayerPrefs.DeleteKey(prefix + "action");
        if (Button != null) Button.action = Id;
    }
}
#endif
