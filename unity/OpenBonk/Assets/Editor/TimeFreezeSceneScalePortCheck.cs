using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TimeFreezeSceneScalePortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("TimeFreezeSceneScalePortCheck: " + message); }
    static void Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    public static void Run()
    {
        foreach (var path in new[] { "Assets/ScenesAndroid/GeneratedMap.unity", "Assets/ScenesAndroid/FinalBossMap.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            TimeFreezeUi ui = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<TimeFreezeUi>(true)) { Check(ui == null, "multiple freeze UIs"); ui = candidate; }
            Check(ui != null && ui.swirl != null && ui.vignette != null, path + " original freeze UI wiring");
            var scale = ui.swirl.GetComponent<ScaleSquareToMaxScreenSize>();
            Check(scale != null && scale.canvasScaler != null, path + " original swirl and canvas scaler wiring");
            var transform = ui.swirl.GetComponent<RectTransform>();
            var playerObject = new GameObject("controlled test player"); playerObject.SetActive(false);
            var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var status = new PlayerStatusEffects(); player.inventory.statusEffects = status;
            try
            {
                ui.swirl.gameObject.SetActive(false); ui.vignette.gameObject.SetActive(false);
                transform.sizeDelta = new Vector2(123, 456);
                Call(ui, "Awake");
                status.AddNewEffect(new StatusEffect(EStatusEffect.TimeFreeze, 100f, Array.Empty<StatModifier>()), 100f);
                Check(ui.swirl.gameObject.activeSelf && ui.vignette.gameObject.activeSelf, path + " status event enables original visuals");
                Call(scale, "OnEnable");
                var resolution = scale.canvasScaler.referenceResolution;
                float expected = (float)Screen.width / Screen.height > 1f ? resolution.x : resolution.y;
                Check(transform.sizeDelta == new Vector2(expected, expected), path + " actual swirl uses its canvas resolution");
                status.RemoveAllStatusEffects();
                Check(!ui.swirl.gameObject.activeSelf && !ui.vignette.gameObject.activeSelf, path + " removing freeze disables visuals");
                Debug.Log($"TimeFreezeSceneScalePortCheck: {path} reference={resolution} swirl={transform.sizeDelta}");
            }
            finally { Call(ui, "OnDestroy"); status.OnDestroy(); MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(playerObject); }
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("TimeFreezeSceneScalePortCheck passed: original Android scenes, status events, swirl scale and removal");
    }
}
