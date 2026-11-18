using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.InGame.Levelup;
using UnityEngine;

public static class OfferToolsPortCheck
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool value, string reason) { if (!value) throw new Exception("OfferToolsPortCheck: " + reason); }
    private static int Count(LevelupScreen screen, string method) => (int)typeof(LevelupScreen).GetMethod(method, Private).Invoke(screen, null);
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldUi = UiManager.Instance;
        var root = new GameObject("offer tools test");
        try
        {
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            inv.refreshes = 0; inv.skips = 2; inv.banishes = 1; typeof(PlayerInventory).GetProperty("gold").GetSetMethod(true).Invoke(inv, new object[] { 100f });
            var screen = root.AddComponent<LevelupScreen>(); var picker = root.AddComponent<UpgradePicker>();
            picker.buttons = new UpgradeButton[0]; screen.upgradePicker = picker;
            typeof(LevelupScreen).GetField("encounterType", Private).SetValue(screen, (EEncounter)(-1));
            OpenBonkOfferTools.SetEnabled(false);
            Check(Count(screen, "GetRefreshes") == 0 && Count(screen, "GetSkips") == 2 && screen.GetBanishes() == 1, "normal counts");
            screen.Refresh(); Check(inv.refreshesUsed == 0 && inv.gold == 100, "disabled empty refresh rejected");
            OpenBonkOfferTools.SetEnabled(true);
            Check(Count(screen, "GetRefreshes") == 9999 && Count(screen, "GetSkips") == 9999 && screen.GetBanishes() == 9999, "virtual available counts");
            screen.Refresh(); Check(inv.refreshes == 0 && inv.refreshesUsed == 1 && inv.gold == 100, "first refresh preserves count and is free");
            screen.Refresh(); Check(inv.refreshes == 0 && inv.refreshesUsed == 2 && inv.gold == 95, "second refresh charges normal gold price");
            typeof(PlayerInventory).GetProperty("gold").GetSetMethod(true).Invoke(inv, new object[] { 0f }); screen.Refresh(); Check(inv.refreshesUsed == 2 && inv.gold == 0, "insufficient gold rejected");
            var ui = root.AddComponent<UiManager>(); ui.encounterWindows = root.AddComponent<EncounterWindows>(); UiManager.Instance = ui;
            screen.Skip(); Check(inv.skips == 2 && inv.skipsUsed == 1, "skip uses virtual count without consuming real count");
            screen.Skip(); Check(inv.skipsUsed == 1, "unaffordable skip rejected");
            typeof(PlayerInventory).GetProperty("gold").GetSetMethod(true).Invoke(inv, new object[] { 100f });
            screen.StartBanish(); Check(picker.banishMode, "virtual banish enters normal mode");
            screen.Banish(); Check(inv.banishes == 1 && inv.banishesUsed == 1 && !picker.banishMode, "real banish uses normal flow");
            int previous = inv.banishes; screen.DecrementBanishes();
            Check(inv.banishes == previous && inv.banishesUsed == 2 && screen.GetBanishes() == 9999, "chest and offer banish share non-consuming path");
            OpenBonkOfferTools.SetEnabled(false);
            Check(Count(screen, "GetRefreshes") == 0 && Count(screen, "GetSkips") == 2 && screen.GetBanishes() == 1, "disable exposes unchanged inventory");
            screen.DecrementBanishes(); Check(inv.banishes == 0 && inv.banishesUsed == 3, "disabled consumption remains normal");
            Debug.Log("OfferToolsPortCheck passed: counts, real refresh, pricing, banish and disable");
        }
        finally { OpenBonkOfferTools.SetEnabled(false); MyPlayer.Instance = oldPlayer; UiManager.Instance = oldUi; UnityEngine.Object.DestroyImmediate(root); }
    }
}
