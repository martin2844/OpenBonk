using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public static class DungeonPressurePlatePortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception("DungeonPressurePlatePortCheck: " + message); }
    static void Call(DungeonPressurePlate p, string method, params object[] args) => typeof(DungeonPressurePlate).GetMethod(method, Private).Invoke(p, args);
    static void Set(DungeonPressurePlate p, string field, object value) => typeof(DungeonPressurePlate).GetField(field, Private).SetValue(p, value);
    static T Get<T>(DungeonPressurePlate p, string field) => (T)typeof(DungeonPressurePlate).GetField(field, Private).GetValue(p);
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GeneratedMap.unity", OpenSceneMode.Single);
        DungeonPressurePlate plate = null;
        foreach (var root in scene.GetRootGameObjects()) { plate = root.GetComponentInChildren<DungeonPressurePlate>(true); if (plate != null) break; }
        Require(plate != null && plate.blocker != null && plate.sfxPlate != null && plate.sfxBlocker != null && plate.shakePreset != null, "scene bindings");
        var other = new GameObject("nonplayer test"); var collider = other.AddComponent<BoxCollider>();
        Call(plate, "OnTriggerEnter", collider); Require(!Get<bool>(plate, "opened"), "non-player must not activate");
        other.layer = LayerMask.NameToLayer("Player"); Vector3 start = plate.transform.position; Vector3 blockerStart = plate.blocker.transform.position; Vector3 blockerUp = plate.blocker.transform.up;
        Call(plate, "OnTriggerEnter", collider);
        Require(Get<bool>(plate, "opened") && Vector3.Distance(Get<Vector3>(plate, "endPos"), start + Vector3.down * .25f) < .0001f, "activation and plate target");
        Set(plate, "progressTimer", .5f); Call(plate, "Update");
        float progress = Get<float>(plate, "progressTimer");
        Require(Vector3.Distance(plate.transform.position, Vector3.Lerp(start, start + Vector3.down * .25f, Easing.InOutQuad(progress))) < .0001f, "eased movement");
        Call(plate, "FinishMove");
        Require(Vector3.Distance(plate.blocker.transform.position, blockerStart + blockerUp * 5.5f) < .0001f, "blocker movement");
        Vector3 shifted = plate.blocker.transform.position; Call(plate, "OnTriggerEnter", collider);
        Require(plate.blocker.transform.position == shifted && Get<float>(plate, "progressTimer") >= .5f, "repeat contact does not restart");
        UnityEngine.Object.DestroyImmediate(other);
        Debug.Log("DungeonPressurePlatePortCheck passed: actual scene, player gate, easing, blocker and repeat contact");
    }
}
