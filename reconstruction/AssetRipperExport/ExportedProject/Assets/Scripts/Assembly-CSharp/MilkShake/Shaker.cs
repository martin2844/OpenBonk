using System.Collections.Generic;
using UnityEngine;

namespace MilkShake
{
	[AddComponentMenu("MilkShake/Shaker")]
	public class Shaker : MonoBehaviour
	{
		public static List<Shaker> GlobalShakers = new List<Shaker>();

		[SerializeField]
		private bool addToGlobalShakers;

		private List<ShakeInstance> activeShakes = new List<ShakeInstance>();

		public static ShakeInstance ShakeAll(IShakeParameters shakeData, int? seed = null)
		{
			var instance = new ShakeInstance(shakeData, seed);
			AddShakeAll(instance);
			return instance;
		}

		public static void ShakeAllSeparate(IShakeParameters shakeData, List<ShakeInstance> shakeInstances = null, int? seed = null)
		{
			foreach (var shaker in GlobalShakers)
			{
				if (shaker == null || !shaker.gameObject.activeInHierarchy) continue;
				var instance = shaker.Shake(shakeData, seed);
				shakeInstances?.Add(instance);
			}
		}

		public static void ShakeAllFromPoint(Vector3 point, float maxDistance, IShakeParameters shakeData, List<ShakeInstance> shakeInstances = null, int? seed = null)
		{
			foreach (var shaker in GlobalShakers)
			{
				if (shaker == null || !shaker.gameObject.activeInHierarchy) continue;
				var instance = shaker.ShakeFromPoint(point, maxDistance, shakeData, seed);
				if (instance != null) shakeInstances?.Add(instance);
			}
		}

		public static void AddShakeAll(ShakeInstance shakeInstance)
		{
			foreach (var shaker in GlobalShakers)
				if (shaker != null && shaker.gameObject.activeInHierarchy) shaker.AddShake(shakeInstance);
		}

		private void Awake()
		{
			if (addToGlobalShakers) GlobalShakers.Add(this);
		}

		private void OnDestroy()
		{
			if (addToGlobalShakers) GlobalShakers.Remove(this);
		}

		private void Update()
		{
			var result = new ShakeResult();
			for (int i = 0; i < activeShakes.Count; i++)
			{
				if (activeShakes[i].IsFinished) { activeShakes.RemoveAt(i--); continue; }
				result += activeShakes[i].UpdateShake(Time.deltaTime);
			}
			transform.localPosition = result.PositionShake;
			transform.localEulerAngles = result.RotationShake;
		}

		public ShakeInstance Shake(IShakeParameters shakeData, int? seed = null)
		{
			var instance = new ShakeInstance(shakeData, seed);
			AddShake(instance);
			return instance;
		}

		public ShakeInstance ShakeFromPoint(Vector3 point, float maxDistance, IShakeParameters shakeData, int? seed = null)
		{
			float distance = Vector3.Distance(point, transform.position);
			if (distance >= maxDistance) return null;
			var instance = new ShakeInstance(shakeData, seed);
			instance.StrengthScale = instance.RoughnessScale = 1f - Mathf.Clamp01(distance / maxDistance);
			AddShake(instance);
			return instance;
		}

		public void AddShake(ShakeInstance shakeInstance)
		{
			activeShakes.Add(shakeInstance);
		}
	}
}
