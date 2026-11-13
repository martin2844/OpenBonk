using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public static class BossLampPortCheck
{
	public static void Run()
	{
		GameObject root = new GameObject("lamp check"); root.SetActive(false);
		GameObject playerObject = new GameObject("lamp player"); playerObject.SetActive(false);
		MyPlayer oldPlayer = MyPlayer.Instance;
		float oldDelta = MyTime.deltaTime, oldFixed = MyTime.fixedDeltaTime;
		float oldDarkness = GraveyardBossRoom.GetDarknessIntensityMultiplier();
		System.Random oldRandom = global::Utility.MyRandom.random;
		GameObject explosion = null;
		try
		{
			BossLamp lamp = root.AddComponent<BossLamp>();
			GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube); zone.transform.SetParent(root.transform);
			lamp.zoneRenderer = zone.GetComponent<Renderer>();
			lamp.lampRenderer = root.AddComponent<MeshRenderer>();
			lamp.minimapIcon = Child(root, "map icon"); lamp.altarIcon = Child(root, "altar icon");
			lamp.circleProgress = Child(root, "progress").AddComponent<Image>();
			lamp.circleParent = Child(root, "canvas").AddComponent<CanvasGroup>();
			lamp.audioStart = Child(root, "start audio").AddComponent<AudioSource>();
			lamp.audioLoop = Child(root, "loop audio").AddComponent<AudioSource>();
			lamp.audioComplete = Child(root, "complete audio").AddComponent<AudioSource>();
			lamp.audioAbort = Child(root, "abort audio").AddComponent<AudioSource>();
			lamp.light = Child(root, "light"); lamp.light.SetActive(false);
			lamp.lampExplosionPrefab = Child(root, "lamp explosion fixture");
			lamp.finishedFx = Child(root, "finished fx").AddComponent<EffectPlayer>();
			lamp.randomGoOutEffect = Child(root, "out fx").AddComponent<EffectPlayer>();
			lamp.finishedFx.ps = lamp.finishedFx.gameObject.AddComponent<ParticleSystem>();
			lamp.randomGoOutEffect.ps = lamp.randomGoOutEffect.gameObject.AddComponent<ParticleSystem>();
			lamp.zoneColor = Color.white;
			MyPlayer.Instance = playerObject.AddComponent<MyPlayer>();
			global::Utility.MyRandom.random = new System.Random(123);
			typeof(GraveyardBossRoom).GetField("darknessLightIntensityMultiplier", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, 1f);
			Invoke(lamp, "Start");
			int activated = 0, deactivated = 0;
			Action on = () => activated++, off = () => deactivated++;
			BossLamp.A_Activate += on; BossLamp.A_Deactivate += off;
			try
			{
				Invoke(lamp, "OnTriggerEnter");
				MyTime.deltaTime = 1f; Invoke(lamp, "Update");
				Check(Mathf.Abs((float)Get(lamp, "chargeProgress") - 1f / 3f) < 0.001f, "three-second charge");
				Invoke(lamp, "OnTriggerExit");
				MyTime.deltaTime = 0.3f; Invoke(lamp, "Update");
				Check((float)Get(lamp, "chargeProgress") == 0f, "charge drains after exit");
				Invoke(lamp, "OnTriggerEnter"); MyTime.deltaTime = 3f; Invoke(lamp, "Update");
				explosion = GameObject.Find("lamp explosion fixture(Clone)");
				Check(activated == 1 && lamp.light.activeSelf && lamp.HasPlayer(), "activation, light and player shelter");
				playerObject.transform.position = new Vector3(5f, 0f, 0f);
				Check(!lamp.HasPlayer(), "player outside shelter");
				Set(lamp, "timeToDeactivate", 0.1f); MyTime.fixedDeltaTime = 0.2f;
				Invoke(lamp, "CheckRandomDeactivate");
				Check(deactivated == 1 && !lamp.light.activeSelf && !lamp.HasPlayer(), "unoccupied lamp goes out");
				lamp.Deactivate(); Check(deactivated == 1, "deactivation is idempotent");
			}
			finally { BossLamp.A_Activate -= on; BossLamp.A_Deactivate -= off; }
			Debug.Log("BossLampPortCheck passed");
		}
		finally
		{
			if (explosion != null) UnityEngine.Object.DestroyImmediate(explosion);
			UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObject);
			MyPlayer.Instance = oldPlayer; MyTime.deltaTime = oldDelta; MyTime.fixedDeltaTime = oldFixed;
			global::Utility.MyRandom.random = oldRandom;
			typeof(GraveyardBossRoom).GetField("darknessLightIntensityMultiplier", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, oldDarkness);
		}
	}
	private static GameObject Child(GameObject root, string name) { GameObject child = new GameObject(name); child.transform.SetParent(root.transform); return child; }
	private static void Invoke(object value, string name) => value.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null);
	private static object Get(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
	private static void Set(object value, string name, object field) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, field);
	private static void Check(bool value, string name) { if (!value) throw new Exception("BossLampPortCheck failed: " + name); }
}
