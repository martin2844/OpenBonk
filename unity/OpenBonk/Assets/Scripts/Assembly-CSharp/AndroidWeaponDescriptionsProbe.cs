#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Opt-in audit; equips and disables weapons in a disposable emulator session.
internal sealed class AndroidWeaponDescriptionsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_weapon_descriptions_probe", false)) return;
        var root = new GameObject("Weapon descriptions probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidWeaponDescriptionsProbe>();
    }

    IEnumerator Start()
    {
        string code;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent")) code = intent.Call<string>("getStringExtra", "megabonk_weapon_locale") ?? "es";
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
        if (source == null) { Debug.LogError("Weapon descriptions probe: card unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        if (locale == null) { Debug.LogError("Weapon descriptions probe: locale missing " + code); yield break; }
        LocalizationSettings.SelectedLocale = locale; yield return null;
        var clone = Instantiate(source.gameObject); clone.SetActive(false); var card = clone.GetComponent<UpgradeButton>();
        var inventory = MyPlayer.Instance.inventory.weaponInventory;
        foreach (var current in inventory.weapons.Values) current.Disable();
        int fresh = 0, owned = 0, failures = 0;
        foreach (var data in DataManager.Instance.unsortedWeapons)
        {
            if (data == null || !data.isEnabled) continue;
            try
            {
                if (!inventory.weapons.ContainsKey(data.eWeapon))
                {
                    card.SetUpgrade(data); Check(data.name, "new", card.t_description.text); fresh++;
                    inventory.AddWeapon(data, null);
                    inventory.weapons[data.eWeapon].Disable();
                }
                card.SetUpgrade(data); Check(data.name, "owned", card.t_description.text); owned++;
            }
            catch (Exception e) { failures++; Debug.LogError($"Weapon descriptions probe: {data.name} threw {e}"); }
        }
        Debug.Log($"Weapon descriptions probe: locale={code}, new={fresh}, owned={owned}, failures={failures}");
        Destroy(clone);
        void Check(string name, string state, string description)
        {
            if (string.IsNullOrWhiteSpace(description) || description.Contains("Missing localization") || description.Contains("No translation found") || description.Contains("{"))
            { failures++; Debug.LogError($"Weapon descriptions probe: {name} {state} invalid: {description}"); }
        }
    }
}
#endif
