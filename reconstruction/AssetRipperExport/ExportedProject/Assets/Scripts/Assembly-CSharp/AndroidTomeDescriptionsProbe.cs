#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Disposable, opt-in audit of real level-up cards and a controlled tome inventory.
internal sealed class AndroidTomeDescriptionsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_tome_descriptions_probe", false)) return;
        var root = new GameObject("Tome descriptions probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidTomeDescriptionsProbe>();
    }

    IEnumerator Start()
    {
        string code;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent")) code = intent.Call<string>("getStringExtra", "megabonk_tome_locale") ?? "en";
        float deadline = Time.realtimeSinceStartup + 180f; UpgradeButton source = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && DataManager.Instance != null && MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && UiManager.Instance != null && UiManager.Instance.encounterWindows != null && !UiManager.Instance.encounterWindows.HasEncounter())
            {
                foreach (var candidate in Resources.FindObjectsOfTypeAll<UpgradeButton>())
                    if (candidate.gameObject.scene.IsValid() && candidate.t_description != null) { source = candidate; break; }
                if (source != null) break;
            }
            yield return null;
        }
        if (source == null) { Debug.LogError("Tome descriptions probe: card unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        if (locale == null) { Debug.LogError("Tome descriptions probe: locale missing " + code); yield break; }
        LocalizationSettings.SelectedLocale = locale; yield return null;
        var clone = Instantiate(source.gameObject); clone.SetActive(false); var card = clone.GetComponent<UpgradeButton>();
        int fresh = 0, owned = 0, failures = 0;
        foreach (var tome in DataManager.Instance.unsortedTomes)
        {
            if (tome == null || !tome.isEnabled) continue;
            try
            {
                card.SetUpgrade(tome); Check(tome.name, "new", card.t_description.text); fresh++;
                if (tome.eTome == ETome.Balance || tome.eTome == ETome.Chaos || tome.eTome == ETome.Gambler || tome.eTome == ETome.Hoarder) continue;
                var inventory = MyPlayer.Instance.inventory.tomeInventory;
                inventory.AddTome(tome, tome.GetUpgradeOffer(ERarity.Common), ERarity.Common);
                card.SetUpgrade(tome); Check(tome.name, "owned", card.t_description.text); owned++;
            }
            catch (Exception e) { failures++; Debug.LogError($"Tome descriptions probe: {tome.name} threw {e}"); }
        }
        Debug.Log($"Tome descriptions probe: locale={code}, new={fresh}, owned={owned}, failures={failures}");
        Destroy(clone);
        void Check(string name, string state, string description)
        {
            if (string.IsNullOrWhiteSpace(description) || description.Contains("Missing localization") || description.Contains("No translation found") || description.Contains("{"))
            { failures++; Debug.LogError($"Tome descriptions probe: {name} {state} invalid: {description}"); }
        }
    }
}
#endif
