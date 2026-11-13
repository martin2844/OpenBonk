using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using UnityEngine;

public static class ZapBarPortCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    public static void Run()
    {
        var root = new GameObject("zap fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory;
        var hud = root.AddComponent<ZapBar>(); var icon = new GameObject("zap icon"); var bar = new GameObject("zap bar");
        icon.SetActive(false); bar.SetActive(false); hud.zapIcon = icon; hud.zapBar = bar; hud.barFill = bar.transform;
        try
        {
            inventory.passiveAbility = new PassiveAbilityWallClimb(); Call(hud, "Start");
            Check(!icon.activeSelf && !bar.activeSelf, "other character keeps HUD hidden");
            var passive = new PassiveAbilityZooma(); inventory.passiveAbility = passive;
            typeof(PassiveAbilityZooma).GetField("accumulatedCharge", Private).SetValue(passive, .35f);
            Call(hud, "Start"); Check(icon.activeSelf && bar.activeSelf, "Zap HUD enabled");
            Call(hud, "FixedUpdate"); Call(hud, "FixedUpdate");
            Check(Vector3.Distance(bar.transform.localScale, new Vector3(.35f, 1f, 1f)) < .0001f, "charge displayed");
            typeof(PassiveAbilityZooma).GetField("accumulatedCharge", Private).SetValue(passive, 1f); Call(hud, "FixedUpdate");
            Check(Vector3.Distance(bar.transform.localScale, Vector3.one) < .0001f, "full charge displayed");
            typeof(PassiveAbilityZooma).GetField("accumulatedCharge", Private).SetValue(passive, 0f); Call(hud, "FixedUpdate");
            Check(Vector3.Distance(bar.transform.localScale, new Vector3(0f, 1f, 1f)) < .0001f, "charge reset displayed");
            Debug.Log("ZapBarPortCheck passed: passive visibility, charge progression and reset");
        }
        finally { MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(icon); UnityEngine.Object.DestroyImmediate(bar); UnityEngine.Object.DestroyImmediate(root); }
    }
}
