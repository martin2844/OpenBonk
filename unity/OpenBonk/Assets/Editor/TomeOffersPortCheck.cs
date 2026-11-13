using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEditor;
using UnityEngine;

public static class TomeOffersPortCheck
{
    static void Require(bool ok, string why) { if (!ok) throw new Exception("TomeOffersPortCheck: " + why); }
    public static void Run()
    {
        var state = new GameObject("tome offer fixture"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.progression = new ProgressionSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var data = state.AddComponent<DataManager>(); DataManager.Instance = data;
        var tome = AssetDatabase.LoadAssetAtPath<TomeData>("Assets/MonoBehaviour/TomeBlood.asset");
        Require(tome != null && tome.GetPrice() > 0 && tome.GetUnlockRequirement() != null, "original purchasable Blood tome");
        var tomes = (Dictionary<ETome, TomeData>)typeof(DataManager).GetField("tomeData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(data); tomes.Add(tome.eTome, tome);
        var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.tomeInventory = new TomeInventory();
        var previousBanished = RunUnlockables.banishedUpgradables; RunUnlockables.banishedUpgradables = new HashSet<UnlockableBase>();
        try
        {
            Require(!UnlockUtility.GetAvailableTomes().Contains(tome), "unlocked purchase required");
            save.progression.achievements.Add(tome.GetUnlockRequirement().internalName); save.progression.purchases.Add(tome.GetInternalName());
            Require(UnlockUtility.GetAvailableTomes().Contains(tome), "purchased and unlocked tome offered");
            save.progression.inactivated.Add(tome.GetInternalName()); Require(!UnlockUtility.GetAvailableTomes().Contains(tome), "inactive unowned tome excluded");
            player.inventory.tomeInventory.tomeLevels.Add(tome.eTome, 1); Require(UnlockUtility.GetAvailableTomes().Contains(tome), "already owned tome remains upgradable");
            RunUnlockables.banishedUpgradables.Add(tome); Require(!UnlockUtility.GetAvailableTomes().Contains(tome), "banish excludes even owned tome");
            RunUnlockables.banishedUpgradables.Remove(tome); Require(UnlockUtility.GetAvailableTomes().Contains(tome), "removing banish restores owned tome");
            Debug.Log("TomeOffersPortCheck passed: original Blood tome, unlock/purchase, activation, ownership and banish");
        }
        finally { RunUnlockables.banishedUpgradables = previousBanished; MyPlayer.Instance = null; DataManager.Instance = null; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null }); UnityEngine.Object.DestroyImmediate(state); }
    }
}
