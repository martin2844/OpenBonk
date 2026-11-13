using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class ChallengeNavigationPortCheck
{
    public static void Run()
    {
        var root = new GameObject("challenge navigation fixture"); root.SetActive(false);
        var menu = root.AddComponent<ChallengesUi>();
        var confirmObject = new GameObject("confirm"); confirmObject.transform.SetParent(root.transform); var confirmButton = confirmObject.AddComponent<Button>(); menu.btn_confirm = confirmObject.AddComponent<MyButtonNormal>();
        var firstObject = new GameObject("first"); firstObject.transform.SetParent(root.transform); var firstButton = firstObject.AddComponent<Button>(); var first = firstObject.AddComponent<SelectionGroupToggleSingleButtonChallenge>();
        var secondObject = new GameObject("second"); secondObject.transform.SetParent(root.transform); var secondButton = secondObject.AddComponent<Button>(); var second = secondObject.AddComponent<SelectionGroupToggleSingleButtonChallenge>();
        var navigation = confirmButton.navigation; navigation.mode = Navigation.Mode.Explicit; navigation.selectOnUp = firstButton; confirmButton.navigation = navigation;
        var select = typeof(ChallengesUi).GetMethod("OnChallengeSelected", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            select.Invoke(menu, new object[] { first });
            if (menu.startBtn != first || confirmButton.navigation.selectOnLeft != firstButton || confirmButton.navigation.selectOnUp != firstButton) throw new Exception("first selection or preserved navigation");
            select.Invoke(menu, new object[] { second });
            if (menu.startBtn != second || confirmButton.navigation.selectOnLeft != secondButton || confirmButton.navigation.selectOnUp != firstButton) throw new Exception("second selection or preserved navigation");
            Debug.Log("ChallengeNavigationPortCheck passed: confirm returns to last selected row; window focus tracks row");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
