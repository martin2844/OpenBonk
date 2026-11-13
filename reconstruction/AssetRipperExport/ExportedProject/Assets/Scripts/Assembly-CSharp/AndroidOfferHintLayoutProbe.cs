#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.InGame.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

internal sealed class AndroidOfferHintLayoutProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_offer_hint_layout_probe", false)) return;
        var root = new GameObject("offer hint layout probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidOfferHintLayoutProbe>();
    }
    static Rect GlyphRect(TMP_Text text, TMP_CharacterInfo c)
    {
        var canvas=text.GetComponentInParent<Canvas>(); var cam=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3 a=RectTransformUtility.WorldToScreenPoint(cam,text.transform.TransformPoint(c.bottomLeft));
        Vector3 b=RectTransformUtility.WorldToScreenPoint(cam,text.transform.TransformPoint(c.topRight));
        return Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
    }
    static bool GlyphsOverlap(TMP_Text left, TMP_Text right)
    {
        if(left==null || right==null) return false;
        left.ForceMeshUpdate();right.ForceMeshUpdate();
        for(int i=0;i<left.textInfo.characterCount;i++)
        {
            var a=left.textInfo.characterInfo[i];if(!a.isVisible)continue;var r=GlyphRect(left,a);
            for(int j=0;j<right.textInfo.characterCount;j++) { var b=right.textInfo.characterInfo[j];if(b.isVisible && r.Overlaps(GlyphRect(right,b)))return true; }
        }
        return false;
    }
    static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var canvas = rect.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x,b.x), Mathf.Min(a.y,b.y), Mathf.Max(a.x,b.x), Mathf.Max(a.y,b.y));
    }
    static void AuditDescription(string kind, string name, TMP_Text text, ref int overflowing, ref int outside, ref int masked, ref int outsideCard)
    {
        text.ForceMeshUpdate();
        if (text.isTextOverflowing) overflowing++;
        Rect rect = ScreenRect(text.rectTransform);
        var masks = text.GetComponentsInParent<UnityEngine.UI.RectMask2D>();
        var card = text.GetComponentInParent<UpgradeButton>();
        Rect cardRect = ScreenRect((RectTransform)card.transform);
        bool outsideRect = false, outsideMask = false, outsideCardRect = false;
        for (int i = 0; i < text.textInfo.characterCount; i++) {
            var ch = text.textInfo.characterInfo[i];
            if (!ch.isVisible) continue;
            Rect glyph = GlyphRect(text,ch);
            if (glyph.xMin < rect.xMin-2 || glyph.xMax > rect.xMax+2 || glyph.yMin < rect.yMin-2 || glyph.yMax > rect.yMax+2) outsideRect = true;
            if (glyph.xMin < cardRect.xMin-2 || glyph.xMax > cardRect.xMax+2 || glyph.yMin < cardRect.yMin-2 || glyph.yMax > cardRect.yMax+2) outsideCardRect = true;
            foreach (var mask in masks) if (mask.isActiveAndEnabled) {
                Rect bounds = ScreenRect(mask.rectTransform);
                if (glyph.xMin < bounds.xMin-2 || glyph.xMax > bounds.xMax+2 || glyph.yMin < bounds.yMin-2 || glyph.yMax > bounds.yMax+2) outsideMask = true;
            }
        }
        if (outsideRect) outside++;
        if (outsideMask) masked++;
        if (outsideCardRect) outsideCard++;
        if (text.isTextOverflowing || outsideRect || outsideMask)
            Debug.Log($"Description layout {kind}: {name} textOverflow={text.isTextOverflowing} glyphOutsideRect={outsideRect} glyphOutsideMask={outsideMask} glyphOutsideCard={outsideCardRect} card={cardRect} mode={text.overflowMode} rect={rect} text={text.text}");
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+900f;
        while(Time.realtimeSinceStartup<deadline && (GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance==null || UiManager.Instance==null || UiManager.Instance.encounterWindows==null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        if(Time.realtimeSinceStartup>=deadline) { Debug.LogError("Offer hint layout probe: gameplay unavailable");yield break; }
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("es"));yield return null;
        UiManager.Instance.encounterWindows.AddEncounter(EEncounter.Moai);yield return new WaitForSecondsRealtime(1f);
        var screen=UiManager.Instance.encounterWindows.levelupScreen as LevelupScreen;
        if(screen==null || screen.upgradePicker==null || EventSystem.current==null) { Debug.LogError("Offer hint layout probe: offer unavailable");yield break; }
        UpgradeButton offer=null;foreach(var candidate in screen.upgradePicker.buttons) if(candidate!=null && candidate.gameObject.activeInHierarchy && candidate.t_description!=null) { offer=candidate;break; }
        if(offer==null) { Debug.LogError("Offer hint layout probe: card unavailable");yield break; }
        EventSystem.current.SetSelectedGameObject(null);EventSystem.current.SetSelectedGameObject(offer.gameObject);yield return null;
        int tested=0,linked=0,overlaps=0,errors=0,overflow=0,outside=0,masked=0,cardOutside=0;
        int empty=0, missing=0, badGlyphs=0, nameOverlap=0;
        var weapons = MyPlayer.Instance.inventory.weaponInventory.weapons;
        foreach (var weapon in DataManager.Instance.unsortedWeapons)
        {
            if (weapon == null || !weapon.isEnabled) continue;
            weapons.TryGetValue(weapon.eWeapon, out var owned);
            int oldLevel = owned != null ? owned.level : 0;
            if (owned != null) owned.level = 0;
            try { offer.SetUpgrade(weapon); }
            catch (System.Exception e) { errors++; Debug.LogError("Weapon offer probe exception: " + weapon.name + " " + e); }
            finally { if (owned != null) owned.level = oldLevel; }
            yield return null; Canvas.ForceUpdateCanvases();
            var description = offer.t_description;
            string value = description != null ? description.text : "";
            if (string.IsNullOrWhiteSpace(value)) { empty++; Debug.LogWarning("Weapon offer probe EMPTY: " + weapon.name); }
            if (value.Contains("Missing localization") || value.Contains("No translation found") || value.Contains("{") || value.Contains("}")) { missing++; Debug.LogWarning("Weapon offer probe PLACEHOLDER: " + weapon.name + " text=" + value); }
            if (description != null)
            {
                AuditDescription("weapon", weapon.name, description, ref overflow, ref outside, ref masked, ref cardOutside);
                description.ForceMeshUpdate();
                bool bad = false;
                for (int i=0; i<description.textInfo.characterCount; i++) if (description.textInfo.characterInfo[i].character == '\ufffd') bad = true;
                if (bad) { badGlyphs++; Debug.LogWarning("Weapon offer probe BAD GLYPH: " + weapon.name); }
                if (GlyphsOverlap(offer.t_name, description)) { nameOverlap++; Debug.LogWarning("Weapon offer probe NAME OVERLAP: " + weapon.name); }
            }
            tested++;
            Debug.Log($"Weapon offer probe RESULT: weapon={weapon.eWeapon} name={offer.t_name.text} description={value}");
        }
        Debug.Log($"Weapon offer probe COMPLETE: locale=es tested={tested} empty={empty} placeholders={missing} badGlyphs={badGlyphs} nameOverlap={nameOverlap} errors={errors} overflow={overflow} glyphOutside={outside} masked={masked} cardOutside={cardOutside}");
        tested=errors=overflow=outside=masked=cardOutside=empty=missing=badGlyphs=nameOverlap=0;
        foreach (var weapon in DataManager.Instance.unsortedWeapons)
        {
            if (weapon == null || !weapon.isEnabled) continue;
            weapons.TryGetValue(weapon.eWeapon, out var owned);
            bool created = owned == null;
            bool success = false;
            int oldLevel = owned != null ? owned.level : 0;
            try {
                if (created) { owned = new Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponBase(weapon); weapons.Add(weapon.eWeapon, owned); }
                owned.level = 1;
                offer.SetUpgrade(weapon); success = true;
            }
            catch (System.Exception e) { errors++; Debug.LogError("Weapon upgrade probe exception: " + weapon.name + " " + e); }
            finally {
                if (created && owned != null) { weapons.Remove(weapon.eWeapon); owned.Cleanup(); }
                else if (owned != null) owned.level = oldLevel;
            }
            if (!success) continue;
            yield return null; Canvas.ForceUpdateCanvases();
            var description = offer.t_description;
            string value = description != null ? description.text : "";
            if (string.IsNullOrWhiteSpace(value)) { empty++; Debug.LogWarning("Weapon upgrade probe EMPTY: " + weapon.name); }
            if (value.Contains("Missing localization") || value.Contains("No translation found") || value.Contains("{") || value.Contains("}")) { missing++; Debug.LogWarning("Weapon upgrade probe PLACEHOLDER: " + weapon.name + " text=" + value); }
            if (description != null) {
                AuditDescription("weapon upgrade", weapon.name, description, ref overflow, ref outside, ref masked, ref cardOutside);
                description.ForceMeshUpdate();
                bool bad = false;
                for (int i=0; i<description.textInfo.characterCount; i++) if (description.textInfo.characterInfo[i].character == '\ufffd') bad = true;
                if (bad) { badGlyphs++; Debug.LogWarning("Weapon upgrade probe BAD GLYPH: " + weapon.name); }
                if (GlyphsOverlap(offer.t_name, description)) { nameOverlap++; Debug.LogWarning("Weapon upgrade probe NAME OVERLAP: " + weapon.name); }
            }
            tested++;
            Debug.Log($"Weapon upgrade probe RESULT: weapon={weapon.eWeapon} name={offer.t_name.text} description={value}");
        }
        Debug.Log($"Weapon upgrade probe COMPLETE: locale=es tested={tested} empty={empty} placeholders={missing} badGlyphs={badGlyphs} nameOverlap={nameOverlap} errors={errors} overflow={overflow} glyphOutside={outside} masked={masked} cardOutside={cardOutside}");
        tested=linked=overlaps=errors=overflow=outside=masked=cardOutside=0;
        foreach(var tome in DataManager.Instance.unsortedTomes)
        {
            if(tome==null || !tome.isEnabled)continue;
            try { offer.SetUpgrade(tome); }
            catch(System.Exception e) { errors++;Debug.LogError("Offer hint layout probe: "+tome.name+" "+e);continue; }
            yield return null;Canvas.ForceUpdateCanvases();
            AuditDescription("tome",tome.name,offer.t_description,ref overflow,ref outside,ref masked,ref cardOutside);
            var hint=offer.transform.Find("Controller tooltip hint");tested++;
            if(hint==null)continue;
            linked++;var label=hint.GetComponent<TextMeshProUGUI>();
            if(GlyphsOverlap(offer.t_name,label) || GlyphsOverlap(offer.t_description,label)) { overlaps++;Debug.LogWarning("Offer hint layout overlap: "+tome.name+" text="+offer.t_description.text); }
        }
        Debug.Log($"Offer hint layout probe: locale=es tested={tested} linked={linked} overlaps={overlaps} errors={errors} overflow={overflow} glyphOutside={outside} masked={masked} cardOutside={cardOutside}");
        tested=linked=overlaps=errors=overflow=outside=masked=cardOutside=0;
        foreach(var item in DataManager.Instance.unsortedItems)
        {
            if(item==null || !item.isEnabled)continue;
            try { offer.SetItem(item); }
            catch(System.Exception e) { errors++;Debug.LogError("Item hint layout probe: "+item.name+" "+e);continue; }
            yield return null;Canvas.ForceUpdateCanvases();
            AuditDescription("item",item.name,offer.t_description,ref overflow,ref outside,ref masked,ref cardOutside);
            var hint=offer.transform.Find("Controller tooltip hint");tested++;
            if(hint==null)continue;
            linked++;var label=hint.GetComponent<TextMeshProUGUI>();
            if(GlyphsOverlap(offer.t_name,label) || GlyphsOverlap(offer.t_description,label)) { overlaps++;Debug.LogWarning("Item hint layout overlap: "+item.name+" text="+offer.t_description.text); }
        }
        Debug.Log($"Item hint layout probe: locale=es tested={tested} linked={linked} overlaps={overlaps} errors={errors} overflow={overflow} glyphOutside={outside} masked={masked} cardOutside={cardOutside}");
        foreach(var item in DataManager.Instance.unsortedItems) if(item != null && item.name == "Giant Fork") {
            offer.SetItem(item); yield return null; Canvas.ForceUpdateCanvases();
            Debug.Log("Description layout probe: CAPTURE Giant Fork");
            yield return new WaitForSecondsRealtime(20f); break;
        }
    }
}
#endif
