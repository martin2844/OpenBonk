using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public static class RailPlacementPortCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("RailPlacementPortCheck: " + message); }
    public static void Run()
    {
        var root = new GameObject("rail placement check");
        var previous = GameManager.Instance;
        try
        {
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsBlockingRails = 1 << 30;
            for (int n = 1; n <= 6; n++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/GameObject/Rail{n}.prefab");
                Require(prefab != null, "missing prefab " + n);
                var obj = UnityEngine.Object.Instantiate(prefab, root.transform);
                obj.transform.SetPositionAndRotation(new Vector3(100, 20, 100), Quaternion.Euler(0, 47, 0));
                var rail = obj.GetComponentInChildren<Rail>(true);
                Require(rail != null && rail.splineContainer != null, "rail bindings " + n);
                Physics.SyncTransforms(); Require(rail.IsValidPosition(), "clear path " + n);
                Vector3 start = default, end = default; float longest = 0;
                for (int i = 0; i < 20; i++)
                {
                    var t = rail.splineContainer.transform;
                    Vector3 a = t.TransformPoint(rail.splineContainer.Spline.EvaluatePosition(i / 20f)) + Vector3.up * .5f;
                    Vector3 b = t.TransformPoint(rail.splineContainer.Spline.EvaluatePosition((i + 1) / 20f)) + Vector3.up * .5f;
                    if ((b - a).magnitude > longest) { start = a; end = b; longest = (b - a).magnitude; }
                }
                Require(longest > .01f, "nonzero segment " + n);
                var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.SetParent(root.transform);
                obstacle.layer = 30; obstacle.transform.position = Vector3.Lerp(start, end, .5f);
                obstacle.transform.rotation = Quaternion.LookRotation(end - start);
                obstacle.transform.localScale = new Vector3(.1f, .1f, longest * .3f);
                Physics.SyncTransforms(); Require(!rail.IsValidPosition(), "obstacle blocks " + n);
                var originalCollider = rail.renderCollider; rail.renderCollider = obstacle.GetComponent<Collider>();
                Require(rail.IsValidPosition(), "own collider ignored " + n);
                rail.renderCollider = originalCollider; obstacle.layer = 29; Physics.SyncTransforms();
                Require(rail.IsValidPosition(), "other layers ignored " + n);
                UnityEngine.Object.DestroyImmediate(obstacle); UnityEngine.Object.DestroyImmediate(obj);
            }
            Debug.Log("RailPlacementPortCheck passed: six real rail prefabs, transformed splines, blockers, self collider and layer mask");
        }
        finally { GameManager.Instance = previous; UnityEngine.Object.DestroyImmediate(root); }
    }
}
