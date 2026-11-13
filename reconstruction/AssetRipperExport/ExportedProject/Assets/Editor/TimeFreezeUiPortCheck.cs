using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public static class TimeFreezeUiPortCheck
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static bool Frozen(TimeFreezeUi ui) => (bool)typeof(TimeFreezeUi).GetField("isTimeFrozen", Private).GetValue(ui);
    public static void Run()
    {
        var root = new GameObject("freeze fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        var status = new PlayerStatusEffects(); player.inventory.statusEffects = status;
        var ui = root.AddComponent<TimeFreezeUi>();
        var swirl = new GameObject("swirl", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var vignette = new GameObject("vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        swirl.SetActive(false); vignette.SetActive(false); ui.swirl = swirl.GetComponent<RawImage>(); ui.vignette = vignette.GetComponent<RawImage>();
        try
        {
            Call(ui, "Awake");
            PlayerStatusEffects.A_StatusEffectAdded?.Invoke(EStatusEffect.Rage, true);
            Check(!Frozen(ui) && !swirl.activeSelf && !vignette.activeSelf, "unrelated effects");
            status.AddNewEffect(new StatusEffect(EStatusEffect.TimeFreeze, 100f, Array.Empty<StatModifier>()), 100f);
            Check(Frozen(ui) && swirl.activeSelf && vignette.activeSelf, "powerup starts freeze visuals");
            MyTime.SetTimeScale(.5f, 3f);
            status.RemoveAllStatusEffects();
            Check(Frozen(ui) && !swirl.activeSelf && vignette.activeSelf, "slow time continues vignette after pickup expires");
            MyTime.SetTimeScale(1f, 0f);
            Check(!Frozen(ui) && !vignette.activeSelf, "normal time ends freeze visuals");
            MyTime.SetTimeScale(.5f, 3f);
            Check(Frozen(ui) && vignette.activeSelf && !swirl.activeSelf, "slow time alone");
            MyTime.SetTimeScale(1f, 0f);
            Call(ui, "OnDestroy"); MyTime.SetTimeScale(.5f, 3f);
            Check(!Frozen(ui) && !vignette.activeSelf, "time event cleanup");
            status.AddNewEffect(new StatusEffect(EStatusEffect.TimeFreeze, 100f, Array.Empty<StatModifier>()), 100f);
            Check(!swirl.activeSelf, "status event cleanup");
            Debug.Log("TimeFreezeUiPortCheck passed: status and time transitions, overlap, visuals and cleanup");
        }
        finally
        {
            Call(ui, "OnDestroy"); status.OnDestroy(); MyPlayer.Instance = null; MyTime.SetTimeScale(1f, 0f);
            UnityEngine.Object.DestroyImmediate(swirl); UnityEngine.Object.DestroyImmediate(vignette); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
