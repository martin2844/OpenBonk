#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.InGame.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Opt-in audit of the real chest's localized text presentation, with forced item order.
internal sealed class AndroidChestDescriptionsProbe : MonoBehaviour
{
    static string requestedLocale = "es";
    static bool pauseForCapture = true;
    static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var canvas = rect.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x,b.x), Mathf.Min(a.y,b.y), Mathf.Max(a.x,b.x), Mathf.Max(a.y,b.y));
    }
    static Rect GlyphRect(TMP_Text text, TMP_CharacterInfo ch)
    {
        var canvas = text.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(ch.bottomLeft));
        Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(ch.topRight));
        return Rect.MinMaxRect(Mathf.Min(a.x,b.x), Mathf.Min(a.y,b.y), Mathf.Max(a.x,b.x), Mathf.Max(a.y,b.y));
    }
    static bool OverlapsText(TMP_Text a, TMP_Text b)
    {
        b.ForceMeshUpdate();
        for (int i=0;i<a.textInfo.characterCount;i++) { var x=a.textInfo.characterInfo[i]; if(!x.isVisible) continue; var r=GlyphRect(a,x);
            for(int j=0;j<b.textInfo.characterCount;j++) { var y=b.textInfo.characterInfo[j]; if(y.isVisible && r.Overlaps(GlyphRect(b,y))) return true; }
        }
        return false;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chest_descriptions_probe", false)) return;
        string locale = intent.Call<string>("getStringExtra", "megabonk_chest_descriptions_locale");
        if (!string.IsNullOrEmpty(locale)) requestedLocale = locale;
        pauseForCapture = intent.Call<bool>("getBooleanExtra", "megabonk_chest_descriptions_capture", true);
        var root = new GameObject("chest descriptions probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidChestDescriptionsProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || DataManager.Instance == null || UiManager.Instance == null || UiManager.Instance.encounterWindows == null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        var encounters = UiManager.Instance != null ? UiManager.Instance.encounterWindows : null;
        var chest = encounters != null ? encounters.chestWindow as ChestWindowUi : null;
        if (chest == null) { Debug.LogError("Chest descriptions probe FAIL: chest unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(requestedLocale));
        if (locale == null || locale.Identifier.Code != requestedLocale) { Debug.LogError("Chest descriptions probe FAIL: exact locale unavailable: " + requestedLocale); yield break; }
        LocalizationSettings.SelectedLocale = locale; yield return null;
        if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
        encounters.AddEncounter(EEncounter.ChestNormal); yield return new WaitForSecondsRealtime(1f);
        if (!chest.gameObject.activeInHierarchy || chest.t_itemDesc == null) { Debug.LogError("Chest descriptions probe FAIL: chest not shown"); yield break; }
        var animate = typeof(ChestWindowUi).GetMethod("AnimateText", BindingFlags.Instance | BindingFlags.NonPublic);
        if (animate == null) { Debug.LogError("Chest descriptions probe FAIL: animation missing"); yield break; }
        int tested=0, bad=0, missingGlyphs=0, invisible=0, overflow=0, masked=0, overlaps=0, titleOverlaps=0;
        float oldScale = Time.timeScale; Time.timeScale = 4f;
        foreach (var item in DataManager.Instance.unsortedItems)
        {
            if (item == null || !item.isEnabled) continue;
            chest.itemData = item;
            IEnumerator sequence;
            try { sequence = (IEnumerator)animate.Invoke(chest, null); }
            catch (Exception e) { bad++; Debug.LogError("Chest descriptions probe exception " + item.name + ": " + e); continue; }
            yield return chest.StartCoroutine(sequence);
            // The production fade uses unscaled time, while this audit accelerates game time.
            yield return new WaitForSecondsRealtime(.4f);
            var text = chest.t_itemDesc; text.ForceMeshUpdate(); tested++;
            bool invalid = string.IsNullOrWhiteSpace(text.text) || text.text.Contains("Missing localization") || text.text.Contains("{");
            bool hidden = !text.gameObject.activeInHierarchy || text.canvasRenderer.GetAlpha() < .95f;
            int missing = 0;
            bool maskClips = false, buttonOverlap = false;
            var masks = text.GetComponentsInParent<RectMask2D>();
            var buttonRect = ScreenRect((RectTransform)chest.b_take.transform);
            for (int i=0; i<text.textInfo.characterCount; i++) {
                var ch = text.textInfo.characterInfo[i];
                if (ch.isVisible && (ch.character == '\u25a1' || ch.character == '\ufffd')) missing++;
                if (!ch.isVisible) continue;
                var glyph = GlyphRect(text,ch);
                if (glyph.Overlaps(buttonRect)) buttonOverlap = true;
                foreach(var mask in masks) if(mask.isActiveAndEnabled) {
                    var rect = ScreenRect(mask.rectTransform);
                    if(glyph.xMin < rect.xMin-2 || glyph.xMax > rect.xMax+2 || glyph.yMin < rect.yMin-2 || glyph.yMax > rect.yMax+2) maskClips=true;
                }
            }
            bool nameOverlap = OverlapsText(text,chest.t_itemName), rarityOverlap = OverlapsText(text,chest.t_itemRarity);
            bool collision = buttonOverlap || nameOverlap || rarityOverlap;
            bool titleOverlap = OverlapsText(chest.t_itemName,chest.t_itemRarity);
            if (invalid) bad++;
            if (hidden) invisible++;
            if (text.isTextOverflowing) overflow++;
            if (maskClips) masked++;
            if (collision) overlaps++;
            if (titleOverlap) titleOverlaps++;
            missingGlyphs += missing;
            if (invalid || hidden || missing > 0 || text.isTextOverflowing || maskClips || collision || titleOverlap)
                Debug.LogWarning($"Chest descriptions probe issue: item={item.name} invalid={invalid} hidden={hidden} alpha={text.canvasRenderer.GetAlpha():F2} missingGlyphs={missing} overflow={text.isTextOverflowing} masked={maskClips} overlaps={collision} nameOverlap={nameOverlap} rarityOverlap={rarityOverlap} buttonOverlap={buttonOverlap} titleOverlap={titleOverlap} text={text.text}");
            if (item.name == "Beefy Ring" || item.name == "Boss Buster" || item.name == "Moldy Cheese" || item.name == "Tactical Glasses" || item.name == "Turbo Socks")
                Debug.Log($"Chest percent description probe: item={item.name} text={text.text}");
            if (pauseForCapture && item.name == "Moldy Cheese")
            {
                Debug.Log($"Chest descriptions probe CAPTURE: item={item.name} overflowMode={text.overflowMode} fontSize={text.fontSize} autoSize={text.enableAutoSizing} bounds={text.rectTransform.rect}");
                yield return new WaitForSecondsRealtime(20f);
            }
        }
        Time.timeScale = oldScale;
        Debug.Log($"Chest descriptions probe completed: locale={locale.Identifier.Code} screen={Screen.width}x{Screen.height} tested={tested} invalid={bad} hidden={invisible} missingGlyphs={missingGlyphs} overflow={overflow} masked={masked} overlaps={overlaps} titleOverlaps={titleOverlaps}");
    }
}
#endif
