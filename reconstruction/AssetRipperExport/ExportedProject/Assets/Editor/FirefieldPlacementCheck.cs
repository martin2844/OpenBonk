using System;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using UnityEditor;
using UnityEngine;

public static class FirefieldPlacementCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception("FirefieldPlacementCheck: " + message); }
    public static void Run()
    {
        var gmObject = new GameObject("manager"); var gm = gmObject.AddComponent<GameManager>(); GameManager.Instance = gm;
        gm.whatIsGroundAndObjects = 1 << LayerMask.NameToLayer("Ground");
        var fieldObject = new GameObject("field"); var field = fieldObject.AddComponent<Firefield>(); field.enabled = false;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "ground"; ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.position = new Vector3(0f, -.5f, 0f); ground.transform.localScale = new Vector3(8f, 1f, 8f);
        var overhead = GameObject.CreatePrimitive(PrimitiveType.Cube); overhead.layer = ground.layer; overhead.transform.position = new Vector3(0f, 1.5f, 0f); overhead.transform.localScale = new Vector3(2f, .2f, 2f);
        var slope = GameObject.CreatePrimitive(PrimitiveType.Cube); slope.layer = ground.layer; slope.transform.position = new Vector3(20f, 0f, 0f); slope.transform.localScale = new Vector3(8f, 1f, 8f); slope.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
        var deep = GameObject.CreatePrimitive(PrimitiveType.Cube); deep.layer = ground.layer; deep.transform.position = new Vector3(40f, -200f, 0f); deep.transform.localScale = new Vector3(8f, 1f, 8f);
        Physics.SyncTransforms();
        field.transform.localScale = Vector3.one * 9f; field.Set(Vector3.zero, Vector3.zero, 4f, 2f, 0f, null, "check");
        Check(Mathf.Abs(field.transform.position.y - .05f) < .001f, "ray origin excludes overhead platform");
        Check(field.transform.localScale == Vector3.zero, "scale resets on set");
        var damage = (float)typeof(Firefield).GetField("damage", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(field); Check(damage == 1f, "minimum damage");
        Vector3 position = new Vector3(20f, 3f, 0f); Check(Physics.Raycast(new Ray(position + Vector3.up, Vector3.down), out var hit, 9999f, gm.whatIsGroundAndObjects), "slope ray hit");
        field.Set(position, Vector3.zero, 4f, 2f, 2f, null, "check");
        Check(Vector3.Distance(field.transform.position, hit.point + hit.normal * .05f) < .001f, "slope position and surface offset");
        Check(Vector3.Dot(field.transform.up, hit.normal) > .9999f && Vector3.Dot(field.transform.forward, Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized) > .9999f, "slope orientation");
        Quaternion previous = field.transform.rotation; Vector3 fallback = new Vector3(100f, 7f, 0f);
        field.Set(new Vector3(100f, 0f, 0f), fallback, 4f, 2f, 2f, null, "check");
        Check(Vector3.Distance(field.transform.position, fallback + Vector3.up * .05f) < .001f && Quaternion.Angle(field.transform.rotation, previous) < .001f, "no-hit fallback and retained orientation");
        field.Set(new Vector3(40f, 0f, 0f), fallback, 4f, 2f, 2f, null, "check");
        Check(field.transform.position.y < -190f, "deep ground reached by original ray distance");
        UnityEngine.Object.DestroyImmediate(gmObject); UnityEngine.Object.DestroyImmediate(fieldObject); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(overhead); UnityEngine.Object.DestroyImmediate(slope); UnityEngine.Object.DestroyImmediate(deep);
        Debug.Log("FirefieldPlacementCheck passed: ray origin, slope alignment, fallback, deep ground and pool scale reset");
    }
}
