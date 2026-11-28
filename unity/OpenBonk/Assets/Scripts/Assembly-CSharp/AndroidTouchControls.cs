#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
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
    private bool ownsMouseSimulation;
    private bool previousMouseSimulation;
    private string locale;
    private AndroidTouchButton[] buttons;
    private AndroidTouchStick[] sticks;
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
        bool inRun = GameManager.Instance != null && GameManager.Instance.IsPlaying() && MyPlayer.Instance != null &&
                     MyPlayer.Instance.hasStarted && !MyPlayer.Instance.IsDead();
        bool canPlay = appFocused && !appPaused && inRun && !GameManager.Instance.cutscene && (!MyTime.paused || AndroidTouchLayoutEditor.Editing) &&
                       UiManager.Instance != null && UiManager.Instance.encounterWindows != null &&
                       !UiManager.Instance.encounterWindows.HasEncounter();
        bool shouldShow = enabled && canPlay;
        if (shouldShow && overlay == null) CreateOverlay();
        if (visible != shouldShow)
        {
            visible = shouldShow;
            SetMouseSimulationForGameplay(visible);
            if (!visible) AndroidTouchInput.ReleaseAll();
            if (overlay != null) overlay.SetActive(visible);
        }
        // A held stick survives menus inside a run, but not the end of the run.
        if (!visible && sticks != null) foreach (var stick in sticks) { if (enabled && inRun) stick.TrackWhileHidden(); else stick.ResetPointer(); }
        if (overlay != null)
        {
            string currentLocale = LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
            if (locale != currentLocale)
            {
                locale = currentLocale;
                foreach (var button in buttons) button.RefreshLabel();
                layoutEditor?.RefreshToggleLabel();
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

    private void SetMouseSimulationForGameplay(bool active)
    {
        if (active && !ownsMouseSimulation) { previousMouseSimulation = Input.simulateMouseWithTouches; Input.simulateMouseWithTouches = false; ownsMouseSimulation = true; }
        else if (!active && ownsMouseSimulation) { Input.simulateMouseWithTouches = previousMouseSimulation; ownsMouseSimulation = false; }
    }

    private void SuspendOverlay()
    {
        SetMouseSimulationForGameplay(false);
        AndroidTouchInput.ReleaseAll();
        if (sticks != null) foreach (var stick in sticks) stick.ResetPointer();
        // Disabling invokes each control's OnDisable, clearing its pointer ID.
        // Android may never send pointer-up after an interruption.
        visible = false;
        if (overlay != null) overlay.SetActive(false);
    }

    private void OnDestroy()
    {
        SetMouseSimulationForGameplay(false);
        AndroidTouchInput.ReleaseAll();
        if (circle != null) { Destroy(circle.texture); Destroy(circle); }
    }

    private void CreateOverlay()
    {
        AndroidTouchSettingsBridge.ApplyLauncherChanges();
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

        // The first sibling receives only touches that did not start on a
        // control. Jump, Aim and Slide also share their gestures with free look.
        var lookArea = new GameObject("Free look area", typeof(RectTransform), typeof(Image), typeof(AndroidTouchFreeLook));
        var lookRect = (RectTransform)lookArea.transform;
        lookRect.SetParent(safe, false); lookRect.anchorMin = Vector2.zero; lookRect.anchorMax = Vector2.one;
        lookRect.offsetMin = lookRect.offsetMax = Vector2.zero;
        lookArea.GetComponent<Image>().color = Color.clear;

        // Above free look and below every control: used only by the floating
        // movement stick, and forwards nothing while that option is off.
        var floatingArea = new GameObject("Floating stick area", typeof(RectTransform), typeof(Image), typeof(AndroidTouchFloatingZone));
        var floatingRect = (RectTransform)floatingArea.transform;
        floatingRect.SetParent(safe, false); floatingRect.anchorMin = Vector2.zero; floatingRect.anchorMax = new Vector2(.5f, .8f);
        floatingRect.offsetMin = floatingRect.offsetMax = Vector2.zero;
        floatingArea.GetComponent<Image>().color = Color.clear;

        var move = CreateStick(safe, "Move", new Vector2(190, 175), false);
        var lookStick = CreateStick(safe, "Look", new Vector2(-390, 175), true);
        sticks = new[] { move, lookStick };
        floatingArea.GetComponent<AndroidTouchFloatingZone>().stick = move;
        buttons = new[] {
            CreateButton(safe, "Jump", new Vector2(-105, 155), 150),
            CreateButton(safe, "Slide", new Vector2(-105, 325), 135),
            CreateButton(safe, "Interact", new Vector2(-260, 400), 135),
            CreateButton(safe, "Aim", new Vector2(-105, 475), 125),
            // Invisible touch target over the in-game minimap. It is still
            // editable in the layout editor, where its label and outline show.
            CreateButton(safe, "MapOverlay", new Vector2(-175, -195), 230, true),
            CreateButton(safe, "Pause", new Vector2(-700, -75), 90, true)
        };
        Canvas.ForceUpdateCanvases();
        layoutEditor = overlay.AddComponent<AndroidTouchLayoutEditor>();
        layoutEditor.Initialize(safe, buttons);
        locale = null;
        overlay.SetActive(false);
    }

    private AndroidTouchStick CreateStick(RectTransform parent, string name, Vector2 position, bool rightAnchor)
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
        return stick;
    }

    private AndroidTouchButton CreateButton(RectTransform parent, string action, Vector2 position, float size, bool top = false)
    {
        var rect = CreateImage(parent, action, position, size, true, top);
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
        button.action = action; button.label = label; button.ghost = action == "MapOverlay";
        button.RefreshLabel();
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

internal sealed class AndroidTouchFreeLook : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData e)
    {
        if (!AndroidTouchLayoutEditor.Editing) AndroidTouchInput.LookGestures.Capture("", e.pointerId);
    }
    public void OnPointerUp(PointerEventData e) { AndroidTouchInput.LookGestures.Release("", e.pointerId); }
    private void OnDisable() { AndroidTouchInput.LookGestures.Clear(); }

    internal static Vector2 Delta()
    {
        if (AndroidTouchLayoutEditor.Editing) return Vector2.zero;
        var gestures = AndroidTouchInput.LookGestures;
        gestures.BeginSample();
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            gestures.Observe(touch.fingerId, touch.phase, touch.deltaPosition);
        }
        return ScaleDelta(gestures.EndSample());
    }

    private static Vector2 ScaleDelta(Vector2 delta) => delta * (1080f / Mathf.Max(1, Screen.height)) * .04f;
}

internal sealed class AndroidTouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    internal const string FloatingKey = "openbonk.touch.layout.floating";
    internal bool look;
    internal RectTransform knob;
    private int pointer = int.MinValue;
    private bool autoRun, autoRunArmed;
    // Set when the finger was taken over from before a menu hid the overlay;
    // the EventSystem keeps sending that finger's events elsewhere, so it is
    // followed through Input.touches instead.
    private bool polling;
    private bool floatingHome;
    private Vector2 home;
    private RectTransform Rect => (RectTransform)transform;
    private static int floatingCache = -1;
    internal static bool FloatingEnabled
    {
        get { if (floatingCache < 0) floatingCache = PlayerPrefs.GetInt(FloatingKey, 0) != 0 ? 1 : 0; return floatingCache == 1; }
    }
    internal static void RefreshFloating() { floatingCache = -1; }
    private bool Floating => !look && FloatingEnabled;
    internal bool Usable
    {
        get { var group = GetComponent<CanvasGroup>(); return group == null || group.blocksRaycasts; }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (AndroidTouchLayoutEditor.Editing) return;
        if (pointer != int.MinValue) return;
        if (autoRun) { Cancel(); return; }
        pointer = eventData.pointerId;
        polling = false;
        if (Floating) MoveBaseTo(eventData.position, eventData.pressEventCamera);
        UpdatePosition(eventData.position, eventData.pressEventCamera);
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!AndroidTouchLayoutEditor.Editing && !polling && eventData.pointerId == pointer) UpdatePosition(eventData.position, eventData.pressEventCamera);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointer && !polling) Release();
    }

    private void Release()
    {
        if (!look && autoRunArmed && PlayerPrefs.GetInt("openbonk.touch.layout.autorun", 0) != 0)
        {
            pointer = int.MinValue; polling = false; autoRun = true; autoRunArmed = false; AndroidTouchInput.Move = Vector2.up;
            ReturnHome();
            if (knob != null) knob.anchoredPosition = Vector2.up * Mathf.Max(1f, Rect.rect.width * .35f);
        }
        else Cancel();
    }

    // Menus such as level-up and chests hide the overlay while the thumb
    // usually stays on the stick. The finger and auto run are kept so movement
    // resumes as soon as the menu closes; AndroidTouchControls calls
    // TrackWhileHidden every frame to drop the finger if it is lifted meanwhile.
    private void OnDisable()
    {
        if (look) Cancel();
        else if (pointer != int.MinValue) { polling = true; if (knob != null) knob.anchoredPosition = Vector2.zero; }
    }
    private void OnEnable()
    {
        if (look || AndroidTouchLayoutEditor.Editing) return;
        if (pointer != int.MinValue)
        {
            if (FindTouch(pointer, out var touch)) UpdatePosition(touch.position, null);
            else Cancel();
        }
        else if (autoRun)
        {
            AndroidTouchInput.Move = Vector2.up;
            if (knob != null) knob.anchoredPosition = Vector2.up * Mathf.Max(1f, Rect.rect.width * .35f);
        }
    }
    internal void TrackWhileHidden()
    {
        if (pointer != int.MinValue && !FindTouch(pointer, out _)) Cancel();
    }
    private void Update()
    {
        if (!polling || pointer == int.MinValue) return;
        if (AndroidTouchLayoutEditor.Editing) { Cancel(); return; }
        if (FindTouch(pointer, out var touch)) UpdatePosition(touch.position, null);
        else Release();
    }
    private static bool FindTouch(int finger, out Touch found)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != finger) continue;
            found = touch;
            return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
        }
        found = default;
        return false;
    }

    internal void ResetPointer() { Cancel(); }

    private void MoveBaseTo(Vector2 screen, Camera camera)
    {
        var parent = Rect.parent as RectTransform;
        if (parent == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screen, camera, out var world)) return;
        if (!floatingHome) { home = Rect.anchoredPosition; floatingHome = true; }
        Rect.position = world;
    }
    private void ReturnHome()
    {
        if (!floatingHome) return;
        Rect.anchoredPosition = home;
        floatingHome = false;
    }

    private void UpdatePosition(Vector2 screen, Camera camera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screen, camera, out var local)) return;
        float radius = Mathf.Max(1f, Rect.rect.width * .35f);
        Vector2 value = Vector2.ClampMagnitude(local / radius, 1f);
        if (!look) autoRunArmed = PlayerPrefs.GetInt("openbonk.touch.layout.autorun", 0) != 0 && local.y > radius * 1.35f && Mathf.Abs(local.x) < radius;
        if (look) AndroidTouchInput.Look = value; else AndroidTouchInput.Move = value;
        if (knob != null) knob.anchoredPosition = value * radius;
    }
    private void Cancel()
    {
        pointer = int.MinValue; polling = false; autoRun = autoRunArmed = false;
        if (look) AndroidTouchInput.Look = Vector2.zero; else AndroidTouchInput.Move = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        ReturnHome();
    }
}

// With the floating stick enabled, a touch anywhere on the left part of the
// screen that does not start on another control moves the stick under it.
internal sealed class AndroidTouchFloatingZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    internal AndroidTouchStick stick;
    private Image image;
    private void Awake() { image = GetComponent<Image>(); }
    private void Update() { if (image != null) image.raycastTarget = AndroidTouchStick.FloatingEnabled && !AndroidTouchLayoutEditor.Editing; }
    private bool Active => stick != null && stick.isActiveAndEnabled && stick.Usable && AndroidTouchStick.FloatingEnabled && !AndroidTouchLayoutEditor.Editing;
    public void OnPointerDown(PointerEventData e) { if (Active) stick.OnPointerDown(e); }
    public void OnDrag(PointerEventData e) { if (stick != null) stick.OnDrag(e); }
    public void OnPointerUp(PointerEventData e) { if (stick != null) stick.OnPointerUp(e); }
}

internal sealed class AndroidTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    internal string action;
    internal TextMeshProUGUI label;
    internal bool ghost;
    private int pointer = int.MinValue;

    internal void RefreshLabel()
    {
        bool editing = AndroidTouchLayoutEditor.Editing;
        if (ghost && !editing)
        {
            label.text = string.Empty;
            var hidden = GetComponent<Image>();
            // Keep a tiny non-zero alpha: a fully transparent Graphic can be
            // culled by the canvas and stop receiving raycasts.
            if (hidden != null) hidden.color = new Color(0f, 0f, 0f, .01f);
            return;
        }
        bool xbox = AndroidControlText.XboxLabels;
        string glyph = action == "Pause" ? "II" : xbox ? AndroidControlText.TouchGlyph(action) : AndroidControlText.TouchAction(action);
        label.text = glyph ?? string.Empty;
        label.color = xbox && action == "Aim" ? new Color(.12f, .10f, .02f, 1f) : new Color(.96f, .97f, .90f, 1f);
        var background = GetComponent<Image>();
        if (background != null) background.color = ButtonColor(action, xbox);
    }

    private static Color ButtonColor(string action, bool xbox)
    {
        if (!xbox) return action == "Interact" ? new Color(.20f, .41f, .45f, .72f) : new Color(.10f, .15f, .17f, .64f);
        if (string.Equals(action, "Jump", StringComparison.OrdinalIgnoreCase) || string.Equals(action, "Jump Bhop", StringComparison.OrdinalIgnoreCase)) return new Color(.28f, .60f, .24f, .72f);
        if (string.Equals(action, "Slide", StringComparison.OrdinalIgnoreCase)) return new Color(.70f, .18f, .20f, .72f);
        if (string.Equals(action, "Interact", StringComparison.OrdinalIgnoreCase)) return new Color(.16f, .42f, .70f, .72f);
        if (string.Equals(action, "Aim", StringComparison.OrdinalIgnoreCase)) return new Color(.86f, .68f, .12f, .78f);
        if (string.Equals(action, "MapOverlay", StringComparison.OrdinalIgnoreCase)) return new Color(.18f, .22f, .26f, .72f);
        return new Color(.10f, .15f, .17f, .64f);
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
        else
        {
            AndroidTouchInput.SetButton(action, true);
            AndroidTouchInput.LookGestures.Capture(action, pointer);
        }
    }
    public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == pointer) Release(); }
    private void OnDisable() { Release(); }
    internal void ResetPointer() { Release(); }
    private void Release()
    {
        AndroidTouchInput.LookGestures.Release(action, pointer);
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
    private TextMeshProUGUI toggleLabel;
    private TextMeshProUGUI labelsLabel;
    private Image toggleBackground;
    private int profileSlot = 1;
    private bool pausedForEditing;
    internal bool SnapToGrid => PlayerPrefs.GetInt(Prefix + "grid", 0) != 0;
    internal bool IsSelected(AndroidTouchLayoutTarget target) => selected == target;
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
        // Keep the editor affordance out of the centre of the screen, where it
        // covered the hotbar and pickup prompts during a run.
        var toggle = MakeButton(safe, "Customize", new Vector2(-30, 30), new Vector2(124, 56), ToggleEditing, false);
        var toggleRect = (RectTransform)toggle.transform;
        toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1, 0);
        toggleRect.pivot = new Vector2(1, 0);
        toggleRect.anchoredPosition = new Vector2(-30, 30);
        toggleBackground = toggle.GetComponent<Image>();
        toggleLabel = toggle.GetComponentInChildren<TextMeshProUGUI>();
        toolbar = new GameObject("Touch layout toolbar", typeof(RectTransform));
        var bar = (RectTransform)toolbar.transform; bar.SetParent(safe, false);
        bar.anchorMin = bar.anchorMax = new Vector2(.5f, 1); bar.pivot = new Vector2(.5f, 1);
        bar.anchoredPosition = new Vector2(0, -12); bar.sizeDelta = new Vector2(1000, 160);
        MakeButton(bar, "Done", new Vector2(-370, -45), new Vector2(155, 76), FinishEditing, true);
        MakeButton(bar, "Reset", new Vector2(-195, -45), new Vector2(155, 76), ResetLayout, true);
        MakeButton(bar, "-", new Vector2(-20, -45), new Vector2(110, 76), () => Resize(.9f), true);
        MakeButton(bar, "+", new Vector2(110, -45), new Vector2(110, 76), () => Resize(1.1f), true);
        MakeButton(bar, "Swap", new Vector2(260, -45), new Vector2(150, 76), StartSwap, true);
        MakeButton(bar, "Show", new Vector2(-390, -125), new Vector2(165, 64), ToggleSelected, true);
        MakeButton(bar, "All", new Vector2(-210, -125), new Vector2(165, 64), ToggleAll, true);
        MakeButton(bar, "Opacity -", new Vector2(-30, -125), new Vector2(165, 64), () => ChangeOpacity(-.1f), true);
        MakeButton(bar, "Opacity +", new Vector2(150, -125), new Vector2(165, 64), () => ChangeOpacity(.1f), true);
        MakeButton(bar, "Grid", new Vector2(330, -125), new Vector2(165, 64), ToggleGrid, true);
        MakeButton(bar, "Floating", new Vector2(-440, -195), new Vector2(165, 64), ToggleFloating, true);
        MakeButton(bar, "Slot", new Vector2(-260, -195), new Vector2(165, 64), () => { profileSlot = profileSlot % 3 + 1; Refresh(); }, true);
        MakeButton(bar, "Save", new Vector2(-80, -195), new Vector2(165, 64), SaveProfile, true);
        MakeButton(bar, "Load", new Vector2(100, -195), new Vector2(165, 64), LoadProfile, true);
        MakeButton(bar, "Auto run", new Vector2(280, -195), new Vector2(165, 64), ToggleAutoRun, true);
        var labels = MakeButton(bar, "Labels", new Vector2(470, -195), new Vector2(180, 64), ToggleLabels, true);
        labelsLabel = labels.GetComponentInChildren<TextMeshProUGUI>();
        var text = new GameObject("Selection", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)text.transform; rect.SetParent(bar, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -255); rect.sizeDelta = new Vector2(1400, 110);
        selectedLabel = text.GetComponent<TextMeshProUGUI>(); selectedLabel.font = TMP_Settings.defaultFontAsset;
        selectedLabel.fontSize = 25; selectedLabel.alignment = TextAlignmentOptions.Center; selectedLabel.raycastTarget = false;
        toolbar.SetActive(false);
        toggle.transform.SetAsLastSibling();
        RefreshLabelsButton();
        RefreshToggleLabel();
    }

    private void AddTarget(System.Collections.Generic.List<AndroidTouchLayoutTarget> list, RectTransform rect, string id, AndroidTouchButton button)
    {
        var target = rect.gameObject.AddComponent<AndroidTouchLayoutTarget>(); target.Setup(this, safe, id, button, Prefix); list.Add(target);
    }
    internal static bool ValidAction(string id, string action) => id == action || (IsRemappable(id) && IsRemappable(action));
    private static bool IsRemappable(string action) => action == "Jump" || action == "Slide" || action == "Interact" || action == "Aim" || action == "MapOverlay";
    private void ToggleEditing() { if (Editing) FinishEditing(); else StartEditing(); }
    private void StartEditing()
    {
        if (Editing) return;
        foreach (var stick in safe.GetComponentsInChildren<AndroidTouchStick>()) stick.ResetPointer();
        foreach (var button in buttons) button.ResetPointer();
        AndroidTouchInput.ReleaseAll();
        pausedForEditing = !MyTime.paused; if (pausedForEditing) MyTime.Pause();
        Editing = true; selected = null; swapFrom = null;
        foreach (var target in targets) target.UpdateAppearance(); toolbar.SetActive(true); RefreshToggleLabel(); Refresh();
    }
    private void FinishEditing() {
        Editing = false; swapFrom = null; toolbar.SetActive(false); foreach (var target in targets) target.UpdateAppearance(); RefreshToggleLabel();
        AndroidTouchInput.ReleaseAll(); if (pausedForEditing) { pausedForEditing = false; if (MyTime.paused) MyTime.Unpause(); }
        PlayerPrefs.Save(); AndroidTouchSettingsBridge.Publish();
    }
    private void OnDisable() { if (Editing) FinishEditing(); }
    private void OnDestroy() { if (Editing) FinishEditing(); Editing = false; }
    internal void Select(AndroidTouchLayoutTarget target)
    {
        selected = target;
        foreach (var item in targets) item.UpdateAppearance();
        if (swapFrom != null && target.Button != null && target.Button != swapFrom && target.Id != "MapOverlay" && IsRemappable(target.Button.action))
        {
            string action = swapFrom.action; swapFrom.action = target.Button.action; target.Button.action = action;
            foreach (var t in targets) if (t.Button != null && IsRemappable(t.Id)) PlayerPrefs.SetString(Prefix + t.Id + ".action", t.Button.action);
            foreach (var b in buttons) b.RefreshLabel();
            swapFrom = null;
        }
        Refresh();
    }
    private void StartSwap() { swapFrom = selected?.Button; if (swapFrom != null && (selected.Id == "MapOverlay" || !IsRemappable(swapFrom.action))) swapFrom = null; Refresh(); }
    private void Resize(float factor) { if (selected != null) selected.Resize(factor); Refresh(); }
    private void ToggleSelected() { if (selected != null) selected.SetVisible(!selected.Visible); Refresh(); }
    private void ToggleAll()
    {
        bool show = false; foreach (var target in targets) if (target.Button != null && !target.Visible) show = true;
        foreach (var target in targets) if (target.Button != null) target.SetVisible(show); Refresh();
    }
    private void ChangeOpacity(float delta) { if (selected != null) selected.SetOpacity(selected.Opacity + delta); Refresh(); }
    private void ToggleGrid() { PlayerPrefs.SetInt(Prefix + "grid", SnapToGrid ? 0 : 1); Refresh(); }
    private void ToggleAutoRun() { PlayerPrefs.SetInt(Prefix + "autorun", PlayerPrefs.GetInt(Prefix + "autorun", 0) == 0 ? 1 : 0); Refresh(); }
    private void ToggleFloating() { PlayerPrefs.SetInt(Prefix + "floating", AndroidTouchStick.FloatingEnabled ? 0 : 1); AndroidTouchStick.RefreshFloating(); Refresh(); }
    private void ToggleLabels()
    {
        PlayerPrefs.SetInt(Prefix + "labels", AndroidControlText.XboxLabels ? 0 : 1);
        PlayerPrefs.Save();
        AndroidControlText.RefreshLabels();
        foreach (var button in buttons) button.RefreshLabel();
        RefreshLabelsButton(); Refresh();
    }
    private void RefreshLabelsButton()
    {
        if (labelsLabel == null) return;
        labelsLabel.text = (Spanish ? "Etiquetas: " : "Labels: ") + (AndroidControlText.XboxLabels ? "Xbox" : (Spanish ? "Texto" : "Text"));
    }
    private string SlotKey => Prefix + "profile." + profileSlot + ".";
    private void SaveProfile()
    {
        foreach (var target in targets) target.CopyToProfile(SlotKey);
        PlayerPrefs.SetInt(SlotKey + "grid", SnapToGrid ? 1 : 0); PlayerPrefs.SetInt(SlotKey + "autorun", PlayerPrefs.GetInt(Prefix + "autorun", 0)); PlayerPrefs.SetInt(SlotKey + "floating", PlayerPrefs.GetInt(Prefix + "floating", 0)); PlayerPrefs.SetInt(SlotKey + "exists", 1); PlayerPrefs.Save(); Refresh();
    }
    private void LoadProfile()
    {
        if (PlayerPrefs.GetInt(SlotKey + "exists", 0) == 0) { selectedLabel.text = Spanish ? "Perfil vacío" : "Empty profile"; return; }
        foreach (var target in targets) target.CopyFromProfile(SlotKey);
        PlayerPrefs.SetInt(Prefix + "grid", PlayerPrefs.GetInt(SlotKey + "grid", 0)); PlayerPrefs.SetInt(Prefix + "autorun", PlayerPrefs.GetInt(SlotKey + "autorun", 0));
        PlayerPrefs.SetInt(Prefix + "floating", PlayerPrefs.GetInt(SlotKey + "floating", 0)); AndroidTouchStick.RefreshFloating();
        foreach (var button in buttons) button.RefreshLabel(); selected = null; swapFrom = null;
        foreach (var target in targets) target.UpdateAppearance(); PlayerPrefs.Save(); Refresh();
    }
    private void ResetLayout()
    {
        swapFrom = null; foreach (var target in targets) target.ResetLayout();
        PlayerPrefs.DeleteKey(Prefix + "grid"); PlayerPrefs.DeleteKey(Prefix + "autorun"); PlayerPrefs.DeleteKey(Prefix + "floating"); AndroidTouchStick.RefreshFloating();
        foreach (var button in buttons) button.RefreshLabel(); PlayerPrefs.Save(); Refresh();
    }
    internal void RefreshToggleLabel()
    {
        if (toggleLabel == null || toggleBackground == null) return;
        toggleLabel.text = Editing ? (Spanish ? "Listo" : "Done") : (Spanish ? "Editar" : "Edit");
        toggleLabel.color = new Color(.96f, .97f, .90f, Editing ? .95f : .72f);
        toggleBackground.color = new Color(.06f, .1f, .12f, Editing ? .78f : .36f);
    }
    private void Refresh()
    {
        if (selectedLabel == null) return;
        selectedLabel.text = swapFrom != null ? (Spanish ? "Toca otro botón para intercambiar" : "Tap another button to swap") :
            (selected == null ? (Spanish ? "Arrastra un control para moverlo" : "Drag a control to move it") :
            (AndroidControlText.TouchAction(selected.Button?.action) ?? selected.Id) + "  ·  " + (Spanish ? "Tamaño" : "Size") + " " + Mathf.RoundToInt(selected.Scale * 100) + "%  ·  " + (Spanish ? "Opacidad" : "Opacity") + " " + Mathf.RoundToInt(selected.Opacity * 100) + "%" + (selected.Visible ? "" : (Spanish ? "  ·  Oculto" : "  ·  Hidden"))) +
            "   |   " + (Spanish ? "Perfil " : "Profile ") + profileSlot + "   |   " + (Spanish ? "Cuadrícula: " : "Grid: ") + (SnapToGrid ? (Spanish ? "sí" : "on") : (Spanish ? "no" : "off")) + "   |   " + (Spanish ? "Autoavance: " : "Auto run: ") + (PlayerPrefs.GetInt(Prefix + "autorun", 0) != 0 ? (Spanish ? "sí" : "on") : (Spanish ? "no" : "off")) +
            "   |   " + (Spanish ? "Stick flotante: " : "Floating stick: ") + (AndroidTouchStick.FloatingEnabled ? (Spanish ? "sí" : "on") : (Spanish ? "no" : "off"));
        if (selected != null && selected.Id == "Move" && PlayerPrefs.GetInt(Prefix + "autorun", 0) != 0)
            selectedLabel.text += Spanish ? "\nArrastrá hacia arriba y soltá; tocá para cancelar." : "\nDrag upward and release; tap to cancel.";
    }
    private GameObject MakeButton(RectTransform parent, string id, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click, bool center)
    {
        var obj = new GameObject(id, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)obj.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = center ? new Vector2(.5f, 1) : new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(.06f, .1f, .12f, .92f); obj.GetComponent<Button>().onClick.AddListener(click);
        var child = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); var tr = (RectTransform)child.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        var label = child.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.fontSize = 24; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.text = Spanish ? (id == "Customize" ? "Editar" : id == "Done" ? "Listo" : id == "Reset" ? "Reiniciar" : id == "Swap" ? "Cambiar" : id == "Show" ? "Visible" : id == "All" ? "Todos" : id == "Opacity -" ? "Opacidad -" : id == "Opacity +" ? "Opacidad +" : id == "Grid" ? "Cuadrícula" : id == "Slot" ? "Perfil" : id == "Save" ? "Guardar" : id == "Load" ? "Cargar" : id == "Auto run" ? "Autoavance" : id == "Floating" ? "Flotante" : id) : (id == "Customize" ? "Edit" : id);
        return obj;
    }
}

internal sealed class AndroidTouchLayoutTarget : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    internal string Id { get; private set; }
    internal AndroidTouchButton Button { get; private set; }
    internal float Scale => ((RectTransform)transform).localScale.x;
    internal bool Visible => PlayerPrefs.GetInt(prefix + "visible", 1) != 0;
    internal float Opacity => Mathf.Clamp(PlayerPrefs.GetFloat(prefix + "opacity", 1), .1f, 1f);
    private CanvasGroup canvasGroup;
    private UnityEngine.UI.Outline selectionOutline;
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
        canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();
        selectionOutline = rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
        selectionOutline.effectColor = new Color(.83f, .96f, .37f, 1f);
        selectionOutline.effectDistance = new Vector2(5, -5);
        LoadSettings();
    }
    private void LoadSettings()
    {
        rect.anchorMin = rect.anchorMax = originalAnchor; rect.anchoredPosition = originalPosition;
        if (Button != null) {
            if (Id == "MapOverlay") Button.action = "MapOverlay";
            else {
                string mapped = PlayerPrefs.GetString(prefix + "action", Id);
                Button.action = AndroidTouchLayoutEditor.ValidAction(Id, mapped) ? mapped : Id;
            }
        }
        float scale = PlayerPrefs.GetFloat(prefix + "scale", 1); if (float.IsNaN(scale) || float.IsInfinity(scale)) scale = 1;
        rect.localScale = Vector3.one * Mathf.Clamp(scale, .5f, 1.8f);
        if (PlayerPrefs.HasKey(prefix + "x") && PlayerPrefs.HasKey(prefix + "y"))
        {
            float x = PlayerPrefs.GetFloat(prefix + "x"), y = PlayerPrefs.GetFloat(prefix + "y");
            if (!float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y)) SetNormalized(new Vector2(x, y));
        }
        UpdateAppearance();
    }
    internal void UpdateAppearance()
    {
        if (canvasGroup == null) return;
        if (Button != null) Button.RefreshLabel();
        canvasGroup.alpha = Visible ? Opacity : (AndroidTouchLayoutEditor.Editing ? .25f : 0f);
        canvasGroup.blocksRaycasts = Visible || AndroidTouchLayoutEditor.Editing;
        if (selectionOutline != null) selectionOutline.enabled = AndroidTouchLayoutEditor.Editing && editor.IsSelected(this);
    }
    internal void SetVisible(bool value) { PlayerPrefs.SetInt(prefix + "visible", value ? 1 : 0); UpdateAppearance(); }
    internal void SetOpacity(float value) { PlayerPrefs.SetFloat(prefix + "opacity", Mathf.Clamp(value, .1f, 1f)); UpdateAppearance(); }
    internal void CopyToProfile(string slot)
    {
        string dst = slot + Id + ".";
        foreach (string key in new[] { "x", "y", "scale", "opacity" }) {
            PlayerPrefs.SetInt(dst + key + ".exists", PlayerPrefs.HasKey(prefix + key) ? 1 : 0);
            if (PlayerPrefs.HasKey(prefix + key)) PlayerPrefs.SetFloat(dst + key, PlayerPrefs.GetFloat(prefix + key));
        }
        PlayerPrefs.SetInt(dst + "visible", Visible ? 1 : 0); PlayerPrefs.SetString(dst + "action", Button == null ? Id : Button.action);
    }
    internal void CopyFromProfile(string slot)
    {
        string src = slot + Id + ".";
        foreach (string key in new[] { "x", "y", "scale", "opacity" }) {
            PlayerPrefs.DeleteKey(prefix + key);
            if (PlayerPrefs.GetInt(src + key + ".exists", 0) != 0) PlayerPrefs.SetFloat(prefix + key, PlayerPrefs.GetFloat(src + key));
        }
        PlayerPrefs.SetInt(prefix + "visible", PlayerPrefs.GetInt(src + "visible", 1));
        PlayerPrefs.SetString(prefix + "action", PlayerPrefs.GetString(src + "action", Id)); LoadSettings();
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
        Vector2 normalized = new Vector2((pos.x - bounds.xMin) / bounds.width, (pos.y - bounds.yMin) / bounds.height);
        if (editor.SnapToGrid) normalized = new Vector2(Mathf.Round(normalized.x * 20f) / 20f, Mathf.Round(normalized.y * 20f) / 20f);
        SetNormalized(normalized); SavePosition();
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
        PlayerPrefs.DeleteKey(prefix + "x"); PlayerPrefs.DeleteKey(prefix + "y"); PlayerPrefs.DeleteKey(prefix + "scale"); PlayerPrefs.DeleteKey(prefix + "action"); PlayerPrefs.DeleteKey(prefix + "visible"); PlayerPrefs.DeleteKey(prefix + "opacity");
        if (Button != null) Button.action = Id; UpdateAppearance();
    }
}
// Launcher edits are queued separately from Unity PlayerPrefs. They are applied
// together at overlay creation; finishing the in-game editor publishes a new
// snapshot so both interfaces show the same layout and saved profiles.
internal static class AndroidTouchSettingsBridge
{
    private const string Prefix = "openbonk.touch.layout.";
    private static readonly string[] Ids = { "Move", "Look", "Jump", "Slide", "Interact", "Aim", "MapOverlay", "Pause" };
    private static void ForEachKey(Action<string, char> visit)
    {
        visit(Prefix + "grid", 'i'); visit(Prefix + "autorun", 'i'); visit(Prefix + "floating", 'i'); visit(Prefix + "labels", 'i');
        for (int slot = 0; slot <= 3; slot++)
        {
            string root = slot == 0 ? Prefix : Prefix + "profile." + slot + ".";
            if (slot != 0) { visit(root + "exists", 'i'); visit(root + "grid", 'i'); visit(root + "autorun", 'i'); visit(root + "floating", 'i'); }
            foreach (string id in Ids)
            {
                foreach (string name in new[] { "x", "y", "scale", "opacity" })
                {
                    visit(root + id + "." + name, 'f');
                    if (slot != 0) visit(root + id + "." + name + ".exists", 'i');
                }
                visit(root + id + ".visible", 'i'); visit(root + id + ".action", 's');
            }
        }
    }
    private static AndroidJavaObject Preferences()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        return activity.Call<AndroidJavaObject>("getSharedPreferences", "openbonk_touch_layout", 0);
    }
    internal static void ApplyLauncherChanges()
    {
        try
        {
            using var prefs = Preferences();
            string text = prefs.Call<string>("getString", "pending", "{}");
            using var pending = new AndroidJavaObject("org.json.JSONObject", text);
            ForEachKey((key, type) => {
                if (!pending.Call<bool>("has", key)) return;
                if (pending.Call<bool>("isNull", key)) { PlayerPrefs.DeleteKey(key); return; }
                if (type == 'i') PlayerPrefs.SetInt(key, pending.Call<int>("getInt", key));
                else if (type == 's') PlayerPrefs.SetString(key, pending.Call<string>("getString", key));
                else { double value = pending.Call<double>("getDouble", key); if (!double.IsNaN(value) && !double.IsInfinity(value)) PlayerPrefs.SetFloat(key, (float)value); }
            });
            PlayerPrefs.Save(); AndroidTouchStick.RefreshFloating(); AndroidControlText.RefreshLabels(); Publish(prefs, true);
        }
        catch (Exception e) { Debug.LogWarning("Could not apply launcher touch settings: " + e.Message); }
    }
    internal static void Publish()
    {
        try { using var prefs = Preferences(); Publish(prefs, false); }
        catch (Exception e) { Debug.LogWarning("Could not publish touch settings: " + e.Message); }
    }
    private static void Publish(AndroidJavaObject prefs, bool clearPending)
    {
        using var json = new AndroidJavaObject("org.json.JSONObject");
        ForEachKey((key, type) => {
            if (!PlayerPrefs.HasKey(key)) return;
            if (type == 'i') json.Call<AndroidJavaObject>("put", key, PlayerPrefs.GetInt(key)).Dispose();
            else if (type == 's') json.Call<AndroidJavaObject>("put", key, PlayerPrefs.GetString(key)).Dispose();
            else json.Call<AndroidJavaObject>("put", key, (double)PlayerPrefs.GetFloat(key)).Dispose();
        });
        using var edit = prefs.Call<AndroidJavaObject>("edit");
        edit.Call<AndroidJavaObject>("putString", "snapshot", json.Call<string>("toString")).Dispose();
        if (clearPending) edit.Call<AndroidJavaObject>("remove", "pending").Dispose();
        if (!edit.Call<bool>("commit")) throw new IOException("cannot persist touch settings");
    }
}
#endif
