using System;
using System.Reflection;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class QuestObjectAnimationPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception("QuestObjectAnimationPortCheck: " + message); }
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, Private).Invoke(obj, null);
    public static void Run()
    {
        var keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/KeyPickupMonke.prefab");
        var shrinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChallengeShrine.prefab");
        Require(keyPrefab && shrinePrefab, "original prefabs");
        var key = UnityEngine.Object.Instantiate(keyPrefab); var shrine = UnityEngine.Object.Instantiate(shrinePrefab);
        try
        {
            var floating = key.GetComponentInChildren<FloatingObject2>(true);
            var pulse = shrine.GetComponentInChildren<ObjectAnimation>(true);
            Require(floating && pulse && Mathf.Approximately(floating.heightOffset, .25f) && floating.moveSpeed == 2 && Mathf.Approximately(pulse.scale, .3f), "serialized animation values");
            var floatingTransform = floating.transform; floatingTransform.position = new Vector3(4f, 5f, 6f);
            floatingTransform.rotation = Quaternion.Euler(0f, 45f, 0f); Call(floating, "Start"); Call(floating, "Update");
            float phase = floatingTransform.eulerAngles.y * Mathf.Deg2Rad * floating.moveSpeed;
            Require(Vector3.Distance(floatingTransform.position, new Vector3(4f, 5f + Mathf.Sin(phase) * floating.heightOffset, 6f)) < .0001f, "key floats relative to initial world position");
            var t = pulse.transform; t.localScale = new Vector3(2f, 3f, 4f); Call(pulse, "Awake");
            MyTime.time = .375f; MyTime.deltaTime = .1f; Call(pulse, "Update");
            Require(Vector3.Distance(t.localScale, new Vector3(2f, 3f, 4f)) < .0001f, "pulse reaches midpoint scale");
            MyTime.time = 0f; Call(pulse, "Update");
            Require(Vector3.Distance(t.localScale, new Vector3(1.7f, 2.55f, 3.4f)) < .0001f, "pulse lower limit and serialized amplitude");
            MyTime.time = .75f; Call(pulse, "Update");
            Require(Vector3.Distance(t.localScale, new Vector3(2.3f, 3.45f, 4.6f)) < .0001f, "pulse upper limit");
            MyTime.deltaTime = 0f; MyTime.time = 0f; Vector3 before = t.localScale; Call(pulse, "Update");
            Require(t.localScale == before, "game-time pause stops pulsing");
            Debug.Log("QuestObjectAnimationPortCheck passed: real key and challenge shrine float, pulse and pause");
        }
        finally { UnityEngine.Object.DestroyImmediate(key); UnityEngine.Object.DestroyImmediate(shrine); }
    }
}
