#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;

// Opt-in diagnostic. Uses real encounter UI and waits for external button input.
internal sealed class AndroidOffersUtilityProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_offers_utility_probe", false)) return;
        var root = new GameObject("offers utility probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidOffersUtilityProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || UiManager.Instance == null || UiManager.Instance.encounterWindows == null)) yield return null;
        if (Time.realtimeSinceStartup >= deadline) { Debug.LogError("Offers utility probe: no active game"); yield break; }
        var inventory = MyPlayer.Instance.inventory;
        inventory.refreshes = 2; inventory.refreshesUsed = 1; inventory.ChangeGold(-inventory.goldInt);
        UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Moai);
        yield return new WaitForSecondsRealtime(1f);
        var screen = UiManager.Instance.encounterWindows.levelupScreen as LevelupScreen;
        if (screen == null || screen.b_refresh == null || !screen.window.activeInHierarchy) { Debug.LogError("Offers utility probe: no active offers"); yield break; }
        var popup = AlwaysUi.Instance != null ? AlwaysUi.Instance.UiTextPopup : null;
        if (popup == null || popup.parent == null) { Debug.LogError("Offers utility probe: no popup"); yield break; }
        Debug.Log($"Offers utility probe: ready refreshes={inventory.refreshes} used={inventory.refreshesUsed} gold={inventory.gold} price={screen.b_refresh.t_price.text} disabled={screen.b_refresh.disbaledOverlay.activeSelf}");
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline && !popup.parent.activeSelf) yield return null;
        if (!popup.parent.activeSelf) { Debug.LogError("Offers utility probe: no affordability popup"); yield break; }
        Debug.Log($"Offers utility probe: rejected refreshes={inventory.refreshes} used={inventory.refreshesUsed} gold={inventory.gold} popup={popup.t_text.text}");
        inventory.ChangeGold(10); screen.b_refresh.SetAmount(inventory.refreshes, screen.GetShopToolPrice(inventory.refreshesUsed));
        Debug.Log($"Offers utility probe: funded gold={inventory.gold} disabled={screen.b_refresh.disbaledOverlay.activeSelf}");
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline && inventory.refreshesUsed == 1) yield return null;
        Debug.Log($"Offers utility probe: final refreshes={inventory.refreshes} used={inventory.refreshesUsed} gold={inventory.gold}");
    }
}
#endif
