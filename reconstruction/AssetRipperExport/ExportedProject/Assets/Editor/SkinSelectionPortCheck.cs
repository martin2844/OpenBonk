using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SkinSelectionPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    static GameObject Child(GameObject parent, string name, params Type[] components)
    {
        var child = new GameObject(name, components); child.transform.SetParent(parent.transform); return child;
    }

    public static void Run()
    {
        var root = new GameObject("SkinSelectionCheck");
        var save = root.AddComponent<SaveManager>();
        var oldSave = SaveManager.Instance;
        var oldData = DataManager.Instance;
        var baseSkin = ScriptableObject.CreateInstance<SkinData>();
        var paid = ScriptableObject.CreateInstance<SkinData>();
        var locked = ScriptableObject.CreateInstance<SkinData>();
        var achievement = ScriptableObject.CreateInstance<MyAchievement>();
        var character = ScriptableObject.CreateInstance<CharacterData>();
        var icon = new Texture2D(2, 2);
        try
        {
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            save.config = new ConfigSaveFile(); save.progression = new ProgressionSaveFile();
            save.config.preferences.characterSkins[ECharacter.Fox] = 1;
            DataManager.Instance = root.AddComponent<DataManager>();
            baseSkin.name = "Default"; baseSkin.character = ECharacter.Fox; baseSkin.skinType = ESkinType.Default; baseSkin.isEnabled = true; baseSkin.icon = icon;
            paid.name = "Paid"; paid.character = ECharacter.Fox; paid.skinType = ESkinType.Kills; paid.isEnabled = true;
            locked.name = "Locked"; locked.character = ECharacter.Fox; locked.skinType = ESkinType.Gold; locked.isEnabled = true;
            achievement.isEnabled = true; achievement.internalName = "test_locked"; locked.unlockRequirement = achievement;
            Require(paid.GetPrice() == 10 && paid.GetInternalName() == "PaidFox", "price and purchase key");
            Require(baseSkin.CompareTo(paid) < 0 && paid.CompareTo(null) > 0 && paid.CompareTo(paid) == 0, "skin sorting");
            var lookup = (Dictionary<ECharacter, List<SkinData>>)typeof(DataManager).GetField("skinData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(DataManager.Instance);
            lookup[ECharacter.Fox] = new List<SkinData> { locked, paid, baseSkin };
            var selection = root.AddComponent<SkinSelection>();
            var prefab = Child(root, "Skin", typeof(RectTransform), typeof(Button), typeof(SkinContainer));
            var container = prefab.GetComponent<SkinContainer>();
            container.skinIcon = Child(prefab, "Icon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            container.locked = Child(prefab, "Locked", typeof(RectTransform));
            container.notSelectedOverlay = Child(prefab, "Unselected", typeof(RectTransform));
            container.purchaseOverlay = Child(prefab, "Purchase", typeof(RectTransform));
            container.t_price = Child(prefab, "Price", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            selection.skinContainerPrefab = prefab;
            selection.b_confirm = Child(root, "Confirm", typeof(RectTransform), typeof(Button), typeof(MyButtonNormal)).GetComponent<MyButtonNormal>();
            var characterButton = Child(root, "Character", typeof(RectTransform), typeof(Button), typeof(MyButtonCharacter)).GetComponent<MyButtonCharacter>();
            character.eCharacter = ECharacter.Fox; characterButton.characterData = character;
            Invoke(selection, "Awake");
            selection.SetCharacter(characterButton);
            var entries = (List<SkinContainer>)typeof(SkinSelection).GetField("containers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(selection);
            Require(entries.Count == 3 && entries[0].skin == baseSkin && entries[1].skin == paid && entries[2].skin == locked, "population and order");
            Require(entries[0].skinIcon.texture == icon && !entries[0].notSelectedOverlay.activeSelf && save.config.preferences.characterSkins[ECharacter.Fox] == 0, "unbought saved selection falls back");
            Require(entries[1].purchaseOverlay.activeSelf && entries[1].t_price.text == "<sprite name=silver> 10", "available paid skin");
            Require(entries[2].locked.activeSelf && !entries[2].skinIcon.gameObject.activeSelf && !entries[2].purchaseOverlay.activeSelf, "locked skin state");
            Require(entries[0].GetButton().navigation.selectOnLeft == characterButton.GetButton() && entries[0].GetButton().navigation.selectOnRight == entries[1].GetButton(), "skin navigation");
            SkinContainer.A_Hover(entries[1]);
            Require(save.config.preferences.characterSkins[ECharacter.Fox] == 0, "preview must not save an unpurchased skin");
            save.progression.silver = 9;
            Require(!save.progression.PurchaseUnlockable(paid) && save.progression.silver == 9 && !save.progression.purchases.Contains(paid.GetInternalName()), "insufficient funds");
            save.progression.silver = 15;
            int purchasedEvents = 0;
            Action<SkinData> purchased = _ => purchasedEvents++;
            SkinContainer.A_Purchased += purchased;
            Invoke(entries[1], "ConfirmPurchase");
            SkinContainer.A_Purchased -= purchased;
            Require(save.progression.silver == 5 && save.progression.purchases.Contains(paid.GetInternalName()) && !entries[1].purchaseOverlay.activeSelf && purchasedEvents == 1, "confirmed purchase");
            Require(!save.progression.PurchaseUnlockable(paid) && save.progression.silver == 5, "duplicate purchase");
            Require(save.config.preferences.characterSkins[ECharacter.Fox] == 1, "purchased selection saved");
            selection.SetNotUnlocked();
            Require(!entries[0].gameObject.activeSelf && !entries[1].gameObject.activeSelf, "hide unavailable character skins");
            Invoke(selection, "OnDestroy");
            Debug.Log("SkinSelectionPortCheck passed: ordering, availability, saved selection, preview, navigation and hiding");
        }
        finally
        {
            DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(baseSkin); UnityEngine.Object.DestroyImmediate(paid); UnityEngine.Object.DestroyImmediate(locked);
            UnityEngine.Object.DestroyImmediate(achievement); UnityEngine.Object.DestroyImmediate(character); UnityEngine.Object.DestroyImmediate(icon);
        }
    }
}
