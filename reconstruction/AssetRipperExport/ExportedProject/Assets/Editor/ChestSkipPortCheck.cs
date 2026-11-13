using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ChestSkipPortCheck
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string message) { if (!value) throw new Exception("ChestSkipPortCheck: " + message); }
    public static void Run()
    {
        var obj = new GameObject("chest skip check"); obj.SetActive(false);
        try
        {
            var chest = obj.AddComponent<ChestOpening>();
            var type = typeof(ChestOpening);
            var init = type.GetMethod("InitializeSkipState", PrivateInstance);
            var canRead = type.GetMethod("CanReadSkipInput", BindingFlags.Static | BindingFlags.NonPublic);
            var wait = type.GetMethod("WaitForChestEffect", PrivateInstance);
            var canSkip = type.GetField("canSkip", PrivateInstance);
            var skipped = type.GetField("skipped", PrivateInstance);
            init.Invoke(chest, new object[] { true, 100 });
            Check((bool)canSkip.GetValue(chest) && !(bool)skipped.GetValue(chest), "Android must allow the first chest and reset previous skip state");
            Check(!(bool)canRead.Invoke(null, new object[] { true, true, 100, 100 }), "the input that opens the chest must not also skip it");
            Check((bool)canRead.Invoke(null, new object[] { true, true, 100, 101 }), "a later confirmation must be accepted");
            Check(!(bool)canRead.Invoke(null, new object[] { false, true, 100, 101 }), "input must be ignored after the skip window closes");
            var androidWait = (IEnumerator)wait.Invoke(chest, new object[] { .4f });
            Check(androidWait.MoveNext() && androidWait.Current == null, "Android intro must wait when not skipped");
            skipped.SetValue(chest, true);
            Check(!androidWait.MoveNext(), "Android intro must end immediately after a skip");
            var nextTier = (IEnumerator)wait.Invoke(chest, new object[] { .32f });
            Check(!nextTier.MoveNext(), "remaining rarity waits must also end after a skip");
            init.Invoke(chest, new object[] { false, 200 });
            Check(!(bool)canSkip.GetValue(chest) && !(bool)skipped.GetValue(chest), "non-Android path must retain the original initial gate");
            var desktopWait = (IEnumerator)wait.Invoke(chest, new object[] { .4f });
            Check(desktopWait.MoveNext() && desktopWait.Current is WaitForSeconds, "non-Android intro must retain its timed wait");

            init.Invoke(chest, new object[] { true, 300 }); skipped.SetValue(chest, true);
            chest.particlesCoinsParent = new GameObject("coins"); chest.particlesCoinsParent.transform.SetParent(obj.transform);
            chest.fxFinal = new GameObject("final"); chest.fxFinal.transform.SetParent(obj.transform);
            chest.fxCommon = new GameObject("common"); chest.fxCommon.transform.SetParent(obj.transform);
            chest.itemShine = new GameObject("shine").AddComponent<ParticleSystem>(); chest.itemShine.transform.SetParent(obj.transform);
            chest.itemIcon = new GameObject("icon", typeof(RectTransform)).AddComponent<RawImage>(); chest.itemIcon.transform.SetParent(obj.transform);
            chest.sfxBuildup = obj.AddComponent<AudioSource>(); chest.psChestEmission = new ParticleSystem[0];
            var item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/MonoBehaviour/Clover.asset");
            Check(item != null, "original item asset must load");
            int finished = 0; Action<ItemData> listener = _ => finished++;
            ChestOpening.A_ChestFinished += listener;
            try
            {
                var opening = (IEnumerator)type.GetMethod("AnimateOpening", PrivateInstance).Invoke(chest, new object[] { item });
                Check(opening.MoveNext() && opening.Current is IEnumerator, "reveal must begin with effect sequence");
                Check(opening.MoveNext() && opening.Current is WaitForSeconds, "skipped reveal must yield before enabling reward");
                Check(finished == 0, "the skip frame must not enable the take button");
            }
            finally { ChestOpening.A_ChestFinished -= listener; }
            Debug.Log("ChestSkipPortCheck passed");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
    }
}
