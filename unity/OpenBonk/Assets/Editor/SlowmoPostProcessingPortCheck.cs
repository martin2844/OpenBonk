using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class SlowmoPostProcessingPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Invoke(SlowmoPP effect, string method) => typeof(SlowmoPP).GetMethod(method, Private).Invoke(effect, null);
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance;
        var oldDelta = MyTime.deltaTime;
        var oldScaleEvent = MyTime.A_TimeScaleChange;
        var oldAdded = PlayerStatusEffects.A_StatusEffectAdded;
        var oldRemoved = PlayerStatusEffects.A_StatusEffectRemoved;
        GameObject root = null; MyPlayer player = null;
        try
        {
            foreach (var name in new[] { "GeneratedMap", "FinalBossMap" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + name + ".unity", OpenSceneMode.Single);
                root = new GameObject("slowmo test player"); root.SetActive(false);
                player = root.AddComponent<MyPlayer>();
                player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
                player.inventory.statusEffects = new PlayerStatusEffects(); MyPlayer.Instance = player;
                var effect = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SlowmoPP>(true)).Single();
                Check(effect.volume != null && effect.volume.profile != null, name + " profile");
                var cg = effect.volume.profile.GetSetting<ColorGrading>(); var lens = effect.volume.profile.GetSetting<LensDistortion>();
                Check(cg != null && lens != null, name + " effects");
                Invoke(effect, "Awake");
                MyTime.deltaTime = .25f;
                var status = new StatusEffect(EStatusEffect.TimeFreeze, 100f, Array.Empty<StatModifier>());
                player.inventory.statusEffects.AddNewEffect(status, 100f);
                Check(cg.active && cg.enabled.value && lens.active && lens.enabled.value, name + " freeze enables effects");
                for (int i = 0; i < 4; i++) Invoke(effect, "Update");
                Check(Mathf.Abs(cg.postExposure.value - .6f) < .001f && Mathf.Abs(cg.saturation.value + 35f) < .001f && Mathf.Abs(lens.intensity.value - 20f) < .001f && cg.colorFilter.value == new Color(.5f, 1, 1, 1), name + " freeze targets");
                MyTime.SetTimeScale(.5f, 10f);
                player.inventory.statusEffects.RemoveAllStatusEffects();
                for (int i = 0; i < 4; i++) Invoke(effect, "Update");
                Check(cg.active && lens.active && Mathf.Abs(lens.intensity.value - 20f) < .001f, name + " overlapping slowdown retains effect");
                MyTime.SetTimeScale(1f, 0f);
                for (int i = 0; i < 4; i++) Invoke(effect, "Update");
                Check(!cg.active && !cg.enabled.value && !lens.active && !lens.enabled.value && Mathf.Abs(lens.intensity.value) < .001f && cg.colorFilter.value == Color.white, name + " recovery");
                Invoke(effect, "OnDestroy");
                player.inventory.statusEffects.OnDestroy(); UnityEngine.Object.DestroyImmediate(root); root = null; player = null;
            }
            Debug.Log("SlowmoPostProcessingPortCheck passed: real profiles, status, overlapping timescale, transitions and recovery");
        }
        finally
        {
            if (player != null) player.inventory.statusEffects.OnDestroy(); MyPlayer.Instance = oldPlayer; MyTime.deltaTime = oldDelta;
            MyTime.A_TimeScaleChange = oldScaleEvent; PlayerStatusEffects.A_StatusEffectAdded = oldAdded; PlayerStatusEffects.A_StatusEffectRemoved = oldRemoved;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
