using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

public static class ObjectiveArrowPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("arrow check"); root.SetActive(false);
		GameObject playerObject = new GameObject("arrow player"); playerObject.SetActive(false);
		GameObject cameraObject = new GameObject("arrow camera"); cameraObject.tag = "MainCamera";
		MyPlayer oldPlayer = MyPlayer.Instance; float oldTime = MyTime.time;
		try
		{
			Camera camera = cameraObject.AddComponent<Camera>();
			MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
			ObjectiveArrow arrow = root.AddComponent<ObjectiveArrow>();
			GameObject canvas = new GameObject("canvas", typeof(RectTransform)); canvas.transform.SetParent(root.transform);
			arrow.canvasRect = canvas.GetComponent<RectTransform>(); arrow.canvasRect.sizeDelta = new Vector2(1000f, 500f);
			arrow.tutorialArrow = new GameObject("marker", typeof(RectTransform)).transform; arrow.tutorialArrow.SetParent(canvas.transform);
			Transform target = new GameObject("target").transform; target.SetParent(root.transform); target.position = new Vector3(100f, 0f, 10f);
			Invoke(arrow, "Awake");
			Check(!arrow.tutorialArrow.gameObject.activeSelf && arrow.tutorialArrow.localScale == Vector3.zero, "initially hidden");
			MyTime.time = 10f; arrow.SetTarget(target, Vector3.up, 6f, 60f, 1f);
			Check(arrow.target == target && arrow.tutorialArrow.gameObject.activeSelf && (Vector3)Get(arrow, "targetSize") == Vector3.one, "target shows marker");
			Vector3 reflected = (Vector3)typeof(ObjectiveArrow).GetMethod("calculateWorldPosition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(arrow, new object[] { new Vector3(2f, 0f, -5f), camera });
			Check(Vector3.Distance(reflected, new Vector3(2f, 0f, 5f)) < 0.001f, "behind-camera target reflects forward");
			arrow.tutorialArrow.localScale = Vector3.one;
			Invoke(arrow, "TargetFollowItem");
			Check(Mathf.Abs(arrow.tutorialArrow.localPosition.x) <= 425.01f && Mathf.Abs(arrow.tutorialArrow.localPosition.y) <= 212.51f, "marker stays within canvas bounds");
			MyTime.time = 71f; Invoke(arrow, "Update");
			Check((Vector3)Get(arrow, "targetSize") == Vector3.zero, "marker fades after timeout");
			arrow.SetTarget(target, Vector3.zero, 6f, 60f);
			target.position = playerObject.transform.position; MyTime.time = 74f; Invoke(arrow, "Update");
			Check((Vector3)Get(arrow, "targetSize") == Vector3.zero, "marker fades near player after minimum time");
			arrow.ClearTarget(); Check(arrow.target == null && (Vector3)Get(arrow, "targetSize") == Vector3.zero, "clear target");
			Debug.Log("ObjectiveArrowPortCheck passed");
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(cameraObject);
			MyPlayer.Instance = oldPlayer; MyTime.time = oldTime;
		}
	}
	private static void Invoke(object value, string name) => value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null);
	private static object Get(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
	private static void Check(bool value, string name) { if (!value) throw new Exception("ObjectiveArrowPortCheck failed: " + name); }
}
