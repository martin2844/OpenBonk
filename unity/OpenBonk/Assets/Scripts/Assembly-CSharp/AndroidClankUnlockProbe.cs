#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

internal sealed class AndroidClankUnlockProbe : MonoBehaviour
{
    bool buy;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_clank_unlock_probe", false)) return;
        var root = new GameObject("clank unlock probe"); DontDestroyOnLoad(root);
        root.AddComponent<AndroidClankUnlockProbe>().buy = intent.Call<bool>("getBooleanExtra", "megabonk_clank_unlock_buy", false);
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
        { Debug.LogError("Clank unlock probe INCONCLUSIVE: startup unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var item = DataManager.Instance.GetCharacterData(ECharacter.Cl4nk);
        if (item == null) { Debug.LogError("Clank unlock probe INCONCLUSIVE: character unavailable"); yield break; }
        var save = SaveManager.Instance.progression;
        Debug.Log($"Clank unlock probe LOADED: unlocked={MyAchievements.IsUnlocked(item, out _)} purchased={MyAchievements.IsPurchased(item)} activated={MyAchievements.IsActivated(item)} silver={save.silver}");
        if (buy)
        {
            var revolver=DataManager.Instance.GetWeapon(EWeapon.Revolver);
            bool hasClankAchievement=save.achievements.Contains("a_clank");
            if (!hasClankAchievement || revolver == null || MyAchievements.IsPurchased(item) || MyAchievements.IsPurchased(revolver))
            { Debug.LogError("Clank unlock probe INCONCLUSIVE: expected earned Clank achievement and unpurchased character and weapon"); yield break; }
            bool blockedBefore=!MyAchievements.IsUnlocked(item, out _);
            Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.SetValueForce("kills",7500f);
            deadline=Time.realtimeSinceStartup+20f;
            while (Time.realtimeSinceStartup<deadline && !MyAchievements.IsUnlocked(revolver,out _)) yield return null;
            if (!MyAchievements.IsUnlocked(revolver,out _)) { Debug.LogError("Clank unlock probe INCONCLUSIVE: forced kill statistic did not unlock Revolver"); yield break; }
            save.AddSilver(100);
            yield return new WaitForSecondsRealtime(20f);
            menu.GoToUnlocks(); yield return null;
            var ui = menu.tabUnlocks.GetComponentInChildren<UnlocksUi>(true);
            var footer = menu.tabUnlocks.GetComponentInChildren<UnlocksFooter>(true);
            if (ui == null || footer == null || ui.tabButtons == null) { Debug.LogError("Clank unlock probe INCONCLUSIVE: UI missing"); yield break; }
            ui.tabButtons.ButtonPressed(1, true); yield return null;
            UnlockContainer weaponCard = null;
            foreach (var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true))
                if (card.gameObject.activeInHierarchy && card.unlockable == revolver) { weaponCard = card; break; }
            if (weaponCard == null) { Debug.LogError("Clank unlock probe INCONCLUSIVE: Revolver card missing"); yield break; }
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(weaponCard.gameObject); yield return null;
            int beforeWeapon=save.silver, weaponPrice=revolver.GetPrice();
            bool weaponReady=weaponCard.isUnlocked && !weaponCard.isPurchased && footer.buyContainer.activeInHierarchy;
            if (weaponReady) footer.TryBuyUnlockable();
            bool weaponPurchased=MyAchievements.IsPurchased(revolver) && save.silver==beforeWeapon-weaponPrice;
            ui.tabButtons.ButtonPressed(0, true); yield return null;
            UnlockContainer target = null;
            foreach (var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true))
                if (card.gameObject.activeInHierarchy && card.unlockable == item) { target = card; break; }
            if (target == null) { Debug.LogError("Clank unlock probe INCONCLUSIVE: card missing"); yield break; }
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target.gameObject); yield return null;
            int baseline = save.silver, price = item.GetPrice();
            bool ready = target.isUnlocked && !target.isPurchased && footer.buyContainer.activeInHierarchy;
            if (ready) footer.TryBuyUnlockable();
            bool pass = blockedBefore && weaponReady && weaponPurchased && ready && MyAchievements.IsPurchased(item) && save.silver == baseline - price;
            Debug.Log($"Clank unlock probe {(pass ? "BUY PASS" : "BUY FAIL")}: blockedBefore={blockedBefore} weaponReady={weaponReady} weaponPrice={weaponPrice} weaponPurchased={weaponPurchased} ready={ready} price={price} before={baseline} after={save.silver} purchased={MyAchievements.IsPurchased(item)}");
            yield break;
        }
        deadline = Time.realtimeSinceStartup + 300f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null)) yield return null;
        if (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null)
        { Debug.LogError("Clank unlock probe INCONCLUSIVE: no started run"); yield break; }
        bool available = MyPlayer.Instance.character == ECharacter.Cl4nk;
        Debug.Log($"Clank unlock probe {(available ? "RUN PASS" : "RUN FAIL")}: available={available} unlocked={MyAchievements.IsUnlocked(item, out _)} purchased={MyAchievements.IsPurchased(item)} activated={MyAchievements.IsActivated(item)}");
    }
}
#endif
