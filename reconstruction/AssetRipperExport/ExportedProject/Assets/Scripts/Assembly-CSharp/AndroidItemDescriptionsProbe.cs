#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Actors.Player;

// Disposable opt-in check against the built Android localization and scene card.
internal sealed class AndroidItemDescriptionsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_item_descriptions_probe", false)) return;
        var root = new GameObject("Item descriptions probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidItemDescriptionsProbe>();
    }

    private IEnumerator Start()
    {
        bool spanish;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            spanish = intent.Call<bool>("getBooleanExtra", "megabonk_item_descriptions_spanish", false);
        float deadline = Time.realtimeSinceStartup + 180f;
        UpgradeButton source = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && DataManager.Instance != null &&
                MyPlayer.Instance != null && UiManager.Instance != null && UiManager.Instance.encounterWindows != null && !UiManager.Instance.encounterWindows.HasEncounter())
            {
                foreach (var candidate in Resources.FindObjectsOfTypeAll<UpgradeButton>())
                    if (candidate.gameObject.scene.IsValid() && candidate.t_description != null) { source = candidate; break; }
                if (source != null) break;
            }
            yield return null;
        }
        if (source == null) { Debug.LogError("Item descriptions probe: scene card unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        if (spanish)
        {
			var locale = LocalizationSettings.AvailableLocales.GetLocale(new UnityEngine.Localization.LocaleIdentifier("es"));
            if (locale == null) { Debug.LogError("Item descriptions probe: Spanish locale unavailable"); yield break; }
            LocalizationSettings.SelectedLocale = locale;
            yield return null;
        }
        var clone = Instantiate(source.gameObject);
        clone.SetActive(false);
        var card = clone.GetComponent<UpgradeButton>();
        int count = 0, failures = 0;
        foreach (var item in DataManager.Instance.unsortedItems)
        {
            if (item == null || !item.isEnabled) continue;
            try
            {
                card.SetItem(item);
                string description = card.t_description.text;
                if (string.IsNullOrWhiteSpace(description) || description.Contains("{") || description.Contains("Missing localization") || description.Contains("No translation found"))
                {
                    failures++;
                    Debug.LogError($"Item descriptions probe: {item.name} invalid: {description}");
                }
                if (item.name == "Battery" || item.name == "Borgar" || item.name == "Oats" || item.name == "Pumpkin")
                    Debug.Log($"Item descriptions probe: {item.name}: {description}");
                count++;
            }
            catch (Exception ex)
            {
                failures++;
                Debug.LogError($"Item descriptions probe: {item.name} threw {ex}");
            }
        }
        Debug.Log($"Item descriptions probe: completed locale={LocalizationSettings.SelectedLocale?.Identifier.Code} checked={count} failures={failures}");
        Destroy(clone);
        UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Moai);
        yield return new WaitForSecondsRealtime(1f);
        var screen = UiManager.Instance.encounterWindows.levelupScreen as LevelupScreen;
        if (screen == null || screen.window == null || !screen.window.activeInHierarchy)
        {
            Debug.LogError("Item descriptions probe: offer window did not open");
            yield break;
        }
        foreach (var button in screen.upgradePicker.buttons)
            if (button != null && button.gameObject.activeInHierarchy)
                Debug.Log($"Item descriptions probe: visible offer name={button.t_name.text} description={button.t_description.text}");
        Debug.Log("Item descriptions probe: capture visible offer window now");
    }
}
#endif
