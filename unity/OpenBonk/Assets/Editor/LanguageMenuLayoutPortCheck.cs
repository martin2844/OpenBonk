using System;
using System.Reflection;
using Assets.Scripts.UI.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class LanguageMenuLayoutPortCheck
{
    public static void Run()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
        LanguageButtonMenu menu = null;
        foreach (var item in Resources.FindObjectsOfTypeAll<LanguageButtonMenu>()) if (item.gameObject.scene.IsValid()) { menu = item; break; }
        if (menu == null || menu.languageText == null || menu.buttonTextWrapper == null || menu.textSizer == null) throw new Exception("Missing scene menu layout references");
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var spanish = locales.Find(item => item.Identifier.Code == "es");
        if (spanish == null) throw new Exception("Spanish locale missing");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(LanguageButtonMenu).GetMethod("OnLocaleChanged", flags).Invoke(menu, new object[] { spanish });
        typeof(LanguageButtonMenu).GetMethod("DelayedRefresh", flags).Invoke(menu, null);
        if (menu.languageText.text != LocalizationUtility.GetLanguageName(spanish)) throw new Exception("Language button did not reflect chosen locale");
        var wrapper = menu.buttonTextWrapper;
        var size = wrapper.t_text.rectTransform.sizeDelta;
        var expected = new Vector2(size.x + wrapper.paddingX * 2f, size.y + wrapper.paddingY * 2f);
        if ((wrapper.rect.sizeDelta - expected).sqrMagnitude > .001f) throw new Exception("Button wrapper did not follow text size");
        var pos = wrapper.t_text.rectTransform.anchoredPosition;
        if ((pos - new Vector2(wrapper.offsetLeftX, size.y * wrapper.yOffsetRatio)).sqrMagnitude > .001f) throw new Exception("Button text offset is wrong");
        Debug.Log("LanguageMenuLayoutPortCheck passed");
    }
}
