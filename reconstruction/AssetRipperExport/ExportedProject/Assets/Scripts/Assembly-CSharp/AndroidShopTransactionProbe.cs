#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Managers;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class AndroidShopTransactionProbe : MonoBehaviour
{
    private bool seed;
    private bool exerciseRefresh;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_shop_transaction_probe", false)) return;
        var root = new GameObject("shop transaction probe"); DontDestroyOnLoad(root);
        var probe = root.AddComponent<AndroidShopTransactionProbe>();
        probe.seed = intent.Call<bool>("getBooleanExtra", "megabonk_shop_transaction_seed", false);
        probe.exerciseRefresh = intent.Call<bool>("getBooleanExtra", "megabonk_shop_refresh_use_probe", false);
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f; MainMenu menu = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            menu = FindObjectOfType<MainMenu>();
            if (menu != null && SaveManager.loaded && SaveManager.Instance?.progression != null && DataManager.Instance != null && EventSystem.current != null) break;
            yield return null;
        }
        if (menu == null || !SaveManager.loaded || SaveManager.Instance?.progression == null || DataManager.Instance == null || EventSystem.current == null)
        { Debug.LogError("Shop transaction probe INCONCLUSIVE: startup unavailable"); yield break; }
        var save = SaveManager.Instance.progression;
        var item = DataManager.Instance.GetShopItemData(EShopItem.Refresh);
        if (item == null || item.unlockRequirement == null)
        { Debug.LogError("Shop transaction probe INCONCLUSIVE: refresh data unavailable"); yield break; }
        if (!seed)
        {
            Debug.Log($"Shop transaction probe RELOAD: silver={save.silver} level={item.GetLevel()} unlocked={MyAchievements.IsUnlocked(item, out _)}");
            deadline = Time.realtimeSinceStartup + 240f;
            Assets.Scripts.Actors.Player.MyPlayer player = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                player = Assets.Scripts.Actors.Player.MyPlayer.Instance;
                if (player != null && player.hasStarted && player.inventory != null) break;
                yield return null;
            }
            if (player == null || !player.hasStarted || player.inventory == null)
            { Debug.LogError("Shop transaction probe INCONCLUSIVE: no started player"); yield break; }
            int runs = (int)MyStats.GetStat(EMyStat.runs.ToString());
            int expected = Mathf.Min(item.GetLevel(), item.maxLevel) + (runs > 5 ? 1 : 0);
            int actual = player.inventory.refreshes;
            Debug.Log($"Shop transaction probe {(actual == expected ? "RUN PASS" : "RUN FAIL")}: refreshes={actual} expected={expected} level={item.GetLevel()} runs={runs}");
            if (!exerciseRefresh) yield break;
            if (actual <= 0 || item.GetLevel() <= 0 || GameManager.Instance == null || UiManager.Instance == null)
            { Debug.LogError("Shop refresh use probe INCONCLUSIVE: no purchased refresh or game UI"); yield break; }
            player.inventory.AddLevel();
            deadline = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < deadline && !LevelupScreen.isLevelingUp) yield return null;
            var screen = FindObjectOfType<LevelupScreen>();
            if (!LevelupScreen.isLevelingUp || screen == null || screen.b_refresh == null || !screen.b_refresh.gameObject.activeInHierarchy)
            { Debug.LogError("Shop refresh use probe INCONCLUSIVE: level-up refresh unavailable"); yield break; }
            yield return new WaitForSecondsRealtime(1f);
            int before = player.inventory.refreshes, used = player.inventory.refreshesUsed;
            int gold = player.inventory.goldInt, refreshPrice = screen.GetShopToolPrice(used);
            var rect = screen.b_refresh.GetComponent<RectTransform>();
            var canvas = screen.b_refresh.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var pos = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            Debug.Log($"Shop refresh use probe READY: charges={before} used={used} gold={gold} price={refreshPrice} x={Mathf.RoundToInt(pos.x)} y={Mathf.RoundToInt(Screen.height - pos.y)}");
            deadline = Time.realtimeSinceStartup + 60f;
            while (Time.realtimeSinceStartup < deadline && player.inventory.refreshesUsed == used) yield return null;
            bool pass = player.inventory.refreshes == before - 1 && player.inventory.refreshesUsed == used + 1 &&
                player.inventory.goldInt == gold - refreshPrice && LevelupScreen.isLevelingUp && screen.window.activeInHierarchy;
            Debug.Log($"Shop refresh use probe {(pass ? "PASS" : "FAIL")}: charges={player.inventory.refreshes} used={player.inventory.refreshesUsed} gold={player.inventory.goldInt} window={screen.window.activeInHierarchy}");
            yield break;
        }
        MyStats.SetValueForce(EMyStat.runs.ToString(), 6f);
        bool unlocked = MyAchievements.TryUnlock(item.unlockRequirement.internalName);
        save.AddSilver(100);
        yield return new WaitForSecondsRealtime(18f);
        menu.GoToShop();
        yield return new WaitForSecondsRealtime(2f);
        ShopContainer target = null;
        foreach (var candidate in FindObjectsOfType<ShopContainer>())
            if (candidate.data == item && candidate.GetComponent<MyButtonShop>() != null) { target = candidate; break; }
        var shop = FindObjectOfType<ShopWindow>();
        if (target == null || shop == null) { Debug.LogError("Shop transaction probe INCONCLUSIVE: item card unavailable"); yield break; }
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target.gameObject);
        yield return null;
        int baseline = save.silver, price = item.GetPrice(), level = item.GetLevel();
        Debug.Log($"Shop transaction probe READY: unlockedNow={unlocked} card={shop.currentContainer == target} level={level} silver={baseline} price={price} refund={item.canRefund}");
        deadline = Time.realtimeSinceStartup + 180f; int stage = 0;
        while (Time.realtimeSinceStartup < deadline && stage < 3)
        {
            if (stage == 0 && item.GetLevel() == level + 1) { stage = 1; Debug.Log($"Shop transaction probe BUY: level={item.GetLevel()} silver={save.silver}"); }
            else if (stage == 1 && item.GetLevel() == level) { stage = 2; Debug.Log($"Shop transaction probe REFUND: level={item.GetLevel()} silver={save.silver}"); }
            else if (stage == 2 && item.GetLevel() == level + 1) { stage = 3; Debug.Log($"Shop transaction probe REPURCHASE: level={item.GetLevel()} silver={save.silver}"); }
            yield return null;
        }
        Debug.Log($"Shop transaction probe {(stage == 3 && save.silver == baseline - price ? "PASS" : "INCONCLUSIVE")}: stage={stage} level={item.GetLevel()} silver={save.silver} expected={baseline - price}");
    }
}
#endif
