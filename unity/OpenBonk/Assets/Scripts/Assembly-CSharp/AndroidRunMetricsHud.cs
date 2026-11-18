#if UNITY_ANDROID && !UNITY_EDITOR
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Informational overlay; never participates in input or navigation.
internal sealed class AndroidRunMetricsHud : MonoBehaviour
{
    private GameObject canvasObject;
    private TextMeshProUGUI label;
    private float nextRefresh;
    private bool wasPlaying;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var root = new GameObject("OpenBonk run metrics"); DontDestroyOnLoad(root);
        root.AddComponent<AndroidRunMetricsHud>();
        PlayerXp.A_XpAdded -= OnXp; PlayerXp.A_XpAdded += OnXp;
        PlayerInventory.A_GoldChange -= OnGold; PlayerInventory.A_GoldChange += OnGold;
        GameManager.A_RunStarted -= OnRunStarted; GameManager.A_RunStarted += OnRunStarted;
    }
    private static bool Active() => OpenBonkRunMetrics.Enabled && !MyTime.paused && GameManager.Instance != null && GameManager.Instance.IsPlaying();
    private static void OnRunStarted() => OpenBonkRunMetrics.Reset();
    private static void OnXp(PlayerXp xp, int amount)
    {
        if (Active() && MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.inventory.playerXp == xp)
            OpenBonkRunMetrics.AddXp(MyTime.time, amount);
    }
    private static void OnGold(PlayerInventory inventory, int amount)
    {
        if (Active() && MyPlayer.Instance != null && MyPlayer.Instance.inventory == inventory)
            OpenBonkRunMetrics.AddGold(MyTime.time, amount);
    }
    internal static void OnDamage(float amount)
    {
        if (Active()) OpenBonkRunMetrics.AddDamage(MyTime.time, amount);
    }
    private void Update()
    {
        bool activeRun = OpenBonkRunMetrics.Enabled && GameManager.Instance != null && GameManager.Instance.IsPlaying() &&
            MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.inventory.playerXp != null && !MyPlayer.Instance.IsDead();
        if (wasPlaying && !activeRun) OpenBonkRunMetrics.Reset();
        wasPlaying = activeRun;
        bool playing = activeRun && !MyTime.paused;
        if (playing && canvasObject == null) Build();
        if (canvasObject == null) return;
        if (canvasObject.activeSelf != playing) canvasObject.SetActive(playing);
        if (!playing || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .2f;
        var inventory = MyPlayer.Instance.inventory;
        var xp = inventory.playerXp;
        var rates = OpenBonkRunMetrics.Get(MyTime.time);
        label.text = $"XP {xp.GetXpInt()} / {XpUtility.XpTotalNextLevel(xp.xp)}  (+{rates.XpPerSecond:F0}/s)\n" +
            $"Gold {inventory.gold:F0}  (+{rates.GoldPerSecond:F0}/s)\nDPS {rates.DamagePerSecond:F1}";
    }
    private void Build()
    {
        canvasObject = new GameObject("Run metrics overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
        var holder = new GameObject("Metrics", typeof(RectTransform), typeof(Image)); holder.transform.SetParent(canvasObject.transform, false);
        var rect = holder.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-425, -350); rect.sizeDelta = new Vector2(375, 100);
        var background = holder.GetComponent<Image>(); background.color = new Color(0, 0, 0, .65f); background.raycastTarget = false;
        var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(holder.transform, false);
        var bounds = text.GetComponent<RectTransform>(); bounds.anchorMin = Vector2.zero; bounds.anchorMax = Vector2.one;
        bounds.offsetMin = new Vector2(12, 2); bounds.offsetMax = new Vector2(-4, -2);
        label = text.GetComponent<TextMeshProUGUI>(); label.fontSize = 23; label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white; label.raycastTarget = false;
    }
}
#endif
