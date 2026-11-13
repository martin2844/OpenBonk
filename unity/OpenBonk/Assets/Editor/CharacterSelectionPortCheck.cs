using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterSelectionPortCheck
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    public static void Run()
    {
        var root = new GameObject("CharacterSelectionCheck");
        var manager = root.AddComponent<SaveManager>();
        var previousSave = SaveManager.Instance;
        var oldData = DataManager.Instance;
        var previousSelected = CharacterMenu.selectedCharacter;
        var previousXpTable = Inventory__Items__Pickups.XpUtility.xpForLevelsTable;
        var dataManager = root.AddComponent<DataManager>();
        var fox = ScriptableObject.CreateInstance<CharacterData>();
        var calcium = ScriptableObject.CreateInstance<CharacterData>();
        var icon = new Texture2D(2, 2);
        try
        {
            // AlwaysManager initializes this table before the real menu opens.
            Inventory__Items__Pickups.XpUtility.Init();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { manager });
            manager.config = new ConfigSaveFile();
            manager.progression = new ProgressionSaveFile();
            DataManager.Instance = dataManager;
            fox.eCharacter = ECharacter.Fox; fox.isEnabled = true; fox.sortingPriority = 0;
            calcium.eCharacter = ECharacter.Calcium; calcium.isEnabled = true; calcium.sortingPriority = 1; calcium.icon = icon;
            dataManager.unsortedCharacterData = new List<CharacterData> { calcium, fox };
            var lookup = (Dictionary<ECharacter, CharacterData>)typeof(DataManager).GetField("characterData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dataManager);
            lookup.Add(fox.eCharacter, fox); lookup.Add(calcium.eCharacter, calcium);
            var menu = root.AddComponent<CharacterMenu>();
            menu.characterGridParent = new GameObject("Grid", typeof(RectTransform)).transform;
            menu.characterGridParent.SetParent(root.transform);
            menu.characterPrefabUi = new GameObject("Character", typeof(RectTransform), typeof(Button), typeof(MyButtonCharacter));
            menu.characterPrefabUi.transform.SetParent(menu.characterGridParent);
            var confirm = new GameObject("Confirm", typeof(RectTransform), typeof(Button), typeof(MyButtonNormal));
            confirm.transform.SetParent(root.transform);
            menu.b_confirm = confirm.GetComponent<MyButtonNormal>();
            var back = new GameObject("Back", typeof(RectTransform), typeof(Button), typeof(MyButtonNormal));
            back.transform.SetParent(root.transform);
            menu.b_back = back.GetComponent<MyButtonNormal>();
            var infoObject = new GameObject("Info");
            infoObject.transform.SetParent(root.transform);
            var info = infoObject.AddComponent<CharacterInfoUI>();
            info.i_character = new GameObject("Icon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            info.i_character.transform.SetParent(infoObject.transform);
            Invoke(info, "Awake");
            Invoke(menu, "TryInit");
            var buttons = (List<MyButtonCharacter>)typeof(CharacterMenu).GetField("characterButtons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
            Require(buttons.Count == 2 && buttons[0].characterData == fox && buttons[1].characterData == calcium, "character list and ordering");
            Require(buttons[0].GetButton().navigation.mode == Navigation.Mode.Explicit && buttons[0].GetButton().navigation.selectOnRight == buttons[1].GetButton() && buttons[0].GetButton().navigation.selectOnUp == menu.b_back.GetButton(), "grid navigation");
            Require(buttons[1].GetButton().navigation.selectOnLeft == buttons[0].GetButton(), "reverse grid navigation");
            buttons[1].StartHover();
            Require(CharacterMenu.selectedCharacter == ECharacter.Calcium && manager.config.preferences.selectedCharacter == ECharacter.Calcium, "selection and saved preference");
            Require(buttons[1].canUseCharacter && menu.b_confirm.GetButton().interactable, "available character confirmation");
            Require(info.i_character.texture == icon, "selected character information icon");
            Require(menu.b_confirm.GetButton().navigation.selectOnLeft == buttons[1].GetButton(), "confirmation navigation");
            Invoke(menu, "TryInit");
            Require(buttons.Count == 2, "initialization duplicated buttons");
            Invoke(menu, "OnDestroy");
            Invoke(info, "OnDestroy");
            Debug.Log("CharacterSelectionPortCheck passed: population, ordering, selection, and confirmation availability");
        }
        finally
        {
            Inventory__Items__Pickups.XpUtility.xpForLevelsTable = previousXpTable;
            DataManager.Instance = oldData;
            CharacterMenu.selectedCharacter = previousSelected;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousSave });
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(fox);
            UnityEngine.Object.DestroyImmediate(calcium);
            UnityEngine.Object.DestroyImmediate(icon);
        }
    }
}
