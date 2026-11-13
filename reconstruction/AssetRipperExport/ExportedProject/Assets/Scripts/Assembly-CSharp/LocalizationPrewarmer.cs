using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationPrewarmer : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
	private static TMP_FontAsset uppercaseEnyeFallback;
#endif
	[Header("Font Assets per script group")]
	public TMP_FontAsset mainFont;

	public TMP_FontAsset notoDefault;

	public TMP_FontAsset notoJp;

	public TMP_FontAsset notoKo;

	public TMP_FontAsset notoSc;

	public TMP_FontAsset notoTc;

	public TMP_FontAsset notoTh;

	public List<string> tableNamesToPreload;

	private void Start()
	{
		OnLocaleChanged(LocalizationSettings.SelectedLocale);
		LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
	}

	private void OnDestroy()
	{
		LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
	}

	private void OnLocaleChanged(Locale locale)
	{
		if (locale == null) return;
		TMP_FontAsset font = GetFontAsset(locale);
		if (font != null) PrewarmFontAsset(font, locale);
	}

	public void PrewarmFontAsset(TMP_FontAsset fontAsset, Locale locale)
	{
		if (fontAsset == null || locale == null || tableNamesToPreload == null) return;
		var characters = new HashSet<char>();
		foreach (string name in tableNamesToPreload)
		{
			var table = LocalizationSettings.StringDatabase.GetTable(name, locale);
			if (table == null) continue;
			foreach (var entry in table.Values)
			{
				string value = entry?.LocalizedValue;
				if (string.IsNullOrEmpty(value)) continue;
				foreach (char character in value) characters.Add(character);
			}
		}
		if (characters.Count > 0) fontAsset.TryAddCharacters(new string(new List<char>(characters).ToArray()), out _);
		SortFallbacks(fontAsset);
	}

	private void SortFallbacks(TMP_FontAsset fontAsset)
	{
		if (mainFont == null || fontAsset == null) return;
		var fonts = new List<TMP_FontAsset> { notoDefault, notoJp, notoKo, notoSc, notoTc, notoTh };
		fonts.RemoveAll(font => font == null || font == fontAsset);
		fonts.Insert(0, fontAsset);
#if UNITY_ANDROID && !UNITY_EDITOR
		// The exported Noto atlas cannot accept newly generated glyphs on Android.
		// Keep the missing uppercase Spanish character in a small writable atlas.
		if (uppercaseEnyeFallback == null && notoDefault != null && notoDefault.sourceFontFile != null)
		{
			var replacement = TMP_FontAsset.CreateFontAsset(notoDefault.sourceFontFile, 90, 9,
				UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic, false);
			if (replacement != null && replacement.TryAddCharacters("Ñ", out _))
			{
				replacement.atlasPopulationMode = AtlasPopulationMode.Static;
				uppercaseEnyeFallback = replacement;
			}
		}
		if (uppercaseEnyeFallback != null)
		{
			fonts.Insert(0, uppercaseEnyeFallback);
			if (mainFont.characterLookupTable.TryGetValue(0x00D1, out var cached) && cached.textAsset != mainFont)
				mainFont.characterLookupTable.Remove(0x00D1);
		}
#endif
		mainFont.fallbackFontAssetTable = new List<TMP_FontAsset> { fonts[0] };
		for (int i = 0; i < fonts.Count; i++)
			fonts[i].fallbackFontAssetTable = i + 1 < fonts.Count ? new List<TMP_FontAsset> { fonts[i + 1] } : new List<TMP_FontAsset>();
	}

	private TMP_FontAsset GetFontAsset(Locale locale)
	{
		if (locale == null) return null;
		string code = locale.Identifier.Code.ToLowerInvariant();
		if (code.StartsWith("ja")) return notoJp;
		if (code.StartsWith("ko")) return notoKo;
		if (code.StartsWith("zh-hans") || code == "zh-cn") return notoSc;
		if (code.StartsWith("zh-hant") || code == "zh-hk" || code == "zh-tw") return notoTc;
		if (code.StartsWith("th")) return notoTh;
		return notoDefault;
	}
}
