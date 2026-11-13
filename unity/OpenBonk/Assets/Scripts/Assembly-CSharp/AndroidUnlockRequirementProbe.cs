#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

internal sealed class AndroidUnlockRequirementProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_unlock_requirement_probe", false)) return;
        var root = new GameObject("unlock requirement probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidUnlockRequirementProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        MainMenu menu = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            menu = FindObjectOfType<MainMenu>();
            if (menu != null && menu.tabUnlocks != null && SaveManager.loaded && DataManager.Instance != null && EventSystem.current != null) break;
            yield return null;
        }
        if (menu == null || !SaveManager.loaded || EventSystem.current == null) { Debug.LogError("Unlock requirement probe: menu unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("es"));
        if (locale == null) { Debug.LogError("Unlock requirement probe: Spanish unavailable"); yield break; }
        LocalizationSettings.SelectedLocale = locale;
        yield return new WaitForSecondsRealtime(20f);
        menu.GoToUnlocks();
        yield return null;
        var ui = menu.tabUnlocks.GetComponentInChildren<UnlocksUi>(true);
        var footer = menu.tabUnlocks.GetComponentInChildren<UnlocksFooter>(true);
        if (ui == null || footer == null || ui.tabButtons == null) { Debug.LogError("Unlock requirement probe: scene bindings missing"); yield break; }
        ui.tabButtons.ButtonPressed(1, true);
        yield return null;
        UnlockContainer target = null;
        foreach (var card in ui.contentParent.GetComponentsInChildren<UnlockContainer>(true))
            if (card.gameObject.activeInHierarchy && card.unlockable != null && card.unlockable.name == "WeaponShotgun") { target = card; break; }
        if (target == null) { Debug.LogError("Unlock requirement probe: Shotgun card not found"); yield break; }
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target.gameObject);
        yield return null;
        var row = footer.requirementsContainer != null && footer.requirementsContainer.reqContainers.Length > 0 ? footer.requirementsContainer.reqContainers[0] : null;
        string description = target.unlockable.GetUnlockRequirement()?.GetUnlockRequirement();
        bool ok = !target.isUnlocked && row != null && row.gameObject.activeInHierarchy && row.t_requirement != null && row.t_requirement.text == description && !string.IsNullOrWhiteSpace(description);
        Debug.Log($"Unlock requirement probe {(ok ? "PASS" : "FAIL")}: locale={LocalizationSettings.SelectedLocale.Identifier.Code}, locked={!target.isUnlocked}, row={row?.gameObject.activeInHierarchy}, text={row?.t_requirement?.text}");
    }
}
#endif
