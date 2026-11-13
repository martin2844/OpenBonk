using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class InventorySlotPortCheck
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Run()
    {
        var root = new GameObject("InventorySlotCheck");
        try
        {
            var slot = root.AddComponent<InventoryItemPrefabUI>();
            slot.icon = new GameObject("Icon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            slot.icon.transform.SetParent(root.transform);
            slot.t_level = new GameObject("Level", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            slot.t_level.transform.SetParent(root.transform);
            slot.lockedOverlay = new GameObject("Locked");
            slot.lockedOverlay.transform.SetParent(root.transform);
            slot.banishedIcon = new GameObject("Banished");
            slot.banishedIcon.transform.SetParent(root.transform);
            slot.toolTipObject = new GameObject("Tooltip").AddComponent<ToolTipObject>();
            slot.toolTipObject.transform.SetParent(root.transform);
            slot.SetUnavailable();
            Require(slot.lockedOverlay.activeSelf && !slot.icon.enabled && slot.t_level.text == "", "locked state");
            slot.SetItem((Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase)null);
            Require(!slot.lockedOverlay.activeSelf && !slot.banishedIcon.activeSelf && !slot.icon.enabled, "empty state");
            slot.icon.enabled = true;
            slot.t_level.text = "2";
            slot.SetBanished();
            Require(slot.banishedIcon.activeSelf && slot.t_level.text == "" && slot.icon.color == new Color(.5f, .5f, .5f, 1f), "banished state");
            slot.SetItem((Assets.Scripts.Saves___Serialization.Progression.Achievements.UnlockableBase)null);
            Require(!slot.banishedIcon.activeSelf && slot.icon.color == Color.white, "reused state");
            var weaponData = ScriptableObject.CreateInstance<WeaponData>();
            try
            {
                typeof(InventoryItemPrefabUI).GetField("item", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(slot, weaponData);
                var refresh = typeof(InventoryItemPrefabUI).GetMethod("RefreshEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                refresh.Invoke(slot, new object[] { false });
                Require(slot.icon.color == Color.black && slot.toolTipObject.text.EndsWith(" (Disabled)"), "disabled weapon state");
                refresh.Invoke(slot, new object[] { true });
                Require(slot.icon.color == Color.white && !slot.toolTipObject.text.EndsWith(" (Disabled)"), "enabled weapon state");
            }
            finally { UnityEngine.Object.DestroyImmediate(weaponData); }
            Debug.Log("InventorySlotPortCheck passed: locked, empty, banished, reused, and weapon enabled states");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
