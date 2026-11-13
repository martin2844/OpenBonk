#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

internal sealed class AndroidScytheUnlockProbe : MonoBehaviour
{
    bool buy;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_scythe_unlock_probe", false)) return;
        var root = new GameObject("scythe unlock probe"); DontDestroyOnLoad(root);
        root.AddComponent<AndroidScytheUnlockProbe>().buy = intent.Call<bool>("getBooleanExtra", "megabonk_scythe_unlock_buy", false);
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        MainMenu menu = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            menu = FindObjectOfType<MainMenu>();
            if (menu != null && menu.tabUnlocks != null && SaveManager.loaded && SaveManager.Instance?.progression != null && DataManager.Instance != null && EventSystem.current != null) break;
            yield return null;
        }
        if (menu == null || !SaveManager.loaded || SaveManager.Instance?.progression == null || DataManager.Instance == null || EventSystem.current == null)
        { Debug.LogError("Scythe unlock probe INCONCLUSIVE: startup unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var item = DataManager.Instance.GetWeapon(EWeapon.Scythe);
        if (item == null) { Debug.LogError("Scythe unlock probe INCONCLUSIVE: weapon unavailable"); yield break; }
        var save = SaveManager.Instance.progression;
        Debug.Log($"Scythe unlock probe LOADED: unlocked={MyAchievements.IsUnlocked(item, out _)} purchased={MyAchievements.IsPurchased(item)} activated={MyAchievements.IsActivated(item)} silver={save.silver}");
        if (buy)
        {
            if (!MyAchievements.IsUnlocked(item, out _) || MyAchievements.IsPurchased(item))
            { Debug.LogError("Scythe unlock probe INCONCLUSIVE: expected earned achievement and unpurchased weapon"); yield break; }
            save.AddSilver(100);
            yield return new WaitForSecondsRealtime(20f);
            menu.GoToUnlocks(); yield return null;
            var ui = menu.tabUnlocks.GetComponentInChildren<UnlocksUi>(true);
            var footer = menu.tabUnlocks.GetComponentInChildren<UnlocksFooter>(true);
            if (ui == null || footer == null || ui.tabButtons == null) { Debug.LogError("Scythe unlock probe INCONCLUSIVE: UI missing"); yield break; }
            ui.tabButtons.ButtonPressed(1, true); yield return null;
            UnlockContainer target = null;
            foreach (var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true))
                if (card.gameObject.activeInHierarchy && card.unlockable == item) { target = card; break; }
            if (target == null) { Debug.LogError("Scythe unlock probe INCONCLUSIVE: card missing"); yield break; }
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject); yield return null;
            int baseline = save.silver, price = item.GetPrice();
            bool ready = target.isUnlocked && !target.isPurchased && footer.buyContainer.activeInHierarchy;
            if (ready) footer.TryBuyUnlockable();
            bool pass = ready && MyAchievements.IsPurchased(item) && save.silver == baseline - price;
            Debug.Log($"Scythe unlock probe {(pass ? "BUY PASS" : "BUY FAIL")}: ready={ready} price={price} before={baseline} after={save.silver} purchased={MyAchievements.IsPurchased(item)}");
            yield break;
        }
        deadline = Time.realtimeSinceStartup + 300f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null)) yield return null;
        if (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null)
        { Debug.LogError("Scythe unlock probe INCONCLUSIVE: no started run"); yield break; }
        bool available = UnlockUtility.GetAvailableWeapons().Contains(item);
        Debug.Log($"Scythe unlock probe {(available ? "RUN PASS" : "RUN FAIL")}: available={available} unlocked={MyAchievements.IsUnlocked(item, out _)} purchased={MyAchievements.IsPurchased(item)} activated={MyAchievements.IsActivated(item)}");
    }
}
#endif
