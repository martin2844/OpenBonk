#if UNITY_ANDROID && !UNITY_EDITOR
using Assets.Scripts.Actors.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Passive HUD: never captures touch, focus, or controller navigation.
internal sealed class AndroidChestTrackerHud : MonoBehaviour
{
    private GameObject canvasObject;
    private TextMeshProUGUI label;
    private int revision = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var root = new GameObject("OpenBonk chest tracker");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidChestTrackerHud>();
    }
    private void Update()
    {
        bool visible = OpenBonkChestTracker.Enabled && OpenBonkChestTracker.Total > 0 &&
            GameManager.Instance != null && GameManager.Instance.IsPlaying() &&
            MyPlayer.Instance != null && MyPlayer.Instance.inventory != null &&
            MyPlayer.Instance.inventory.playerHealth != null && !MyPlayer.Instance.IsDead();
        if (visible && canvasObject == null) Build();
        if (canvasObject == null) return;
        if (canvasObject.activeSelf != visible) canvasObject.SetActive(visible);
        if (!visible || revision == OpenBonkChestTracker.Revision) return;
        revision = OpenBonkChestTracker.Revision;
        label.text = "Chests: " + OpenBonkChestTracker.Unopened + " / " + OpenBonkChestTracker.Total;
    }
    private void Build()
    {
        canvasObject = new GameObject("Chest tracker overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1;
        var holder = new GameObject("Chest count", typeof(RectTransform), typeof(Image));
        holder.transform.SetParent(canvasObject.transform, false);
        var rect = holder.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -100); rect.sizeDelta = new Vector2(290, 54);
        var background = holder.GetComponent<Image>(); background.color = new Color(0, 0, 0, .65f); background.raycastTarget = false;
        var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(holder.transform, false);
        var t = text.GetComponent<RectTransform>(); t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
        t.offsetMin = Vector2.zero; t.offsetMax = Vector2.zero;
        label = text.GetComponent<TextMeshProUGUI>(); label.fontSize = 28; label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white; label.raycastTarget = false;
    }
}
#endif
