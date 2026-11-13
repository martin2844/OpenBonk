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
        if (pointer != int.MinValue) return;
        pointer = eventData.pointerId;
        UpdatePosition(eventData);
    }
    public void OnDrag(PointerEventData eventData) { if (eventData.pointerId == pointer) UpdatePosition(eventData); }
    public void OnPointerUp(PointerEventData eventData) { if (eventData.pointerId == pointer) Release(); }
    private void OnDisable() { Release(); }

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
    private void Release()
    {
        pointer = int.MinValue;
        if (action != "Pause") AndroidTouchInput.SetButton(action, false);
    }
}
#endif
