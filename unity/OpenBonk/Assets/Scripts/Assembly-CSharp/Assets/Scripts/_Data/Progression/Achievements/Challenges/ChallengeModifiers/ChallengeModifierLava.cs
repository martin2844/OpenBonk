using UnityEngine;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Utility;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/Lava", order = 1)]
	public class ChallengeModifierLava : ChallengeModifier
	{
		private GameObject lavaObject;

		private Vector3 topPosition;

		private Vector3 lowPosition;

		private float riseTime = 30f;

		private float stayTop = 15f;

		private float lowerTime = 15f;

		private float stayBottom = 20f;

		private float cycleDuration;

		private float startDelay = 10f;

		private float startTime;

		public override void Init(ChallengeData challengeData)
		{
			MapGenerationController.A_GenerationComplete += OnGenerationComplete;
		}

		public override void Cleanup()
		{
			MapGenerationController.A_GenerationComplete -= OnGenerationComplete;
		}

		private void OnGenerationComplete()
		{
			GameObject prefab = EffectManager.Instance.floorIsLava;
			lavaObject = Object.Instantiate(prefab, MapInfo.mapCenter + Vector3.down * MapInfo.mapSize.y, prefab.transform.rotation);
			lavaObject.GetComponent<Water>().SetFloorIsLava();
			Bounds bounds = Object.FindAnyObjectByType<MapGenerationController>().proceduralMapMeshGenerator.display.meshCollider.bounds;
			lowPosition = bounds.center + Vector3.down * (bounds.size.y * 0.5f + 5f);
			topPosition = bounds.center + Vector3.up * (bounds.size.y * 0.375f);
			cycleDuration = riseTime + stayTop + lowerTime + stayBottom;
			startTime = Time.time + startDelay;
		}

		public override void Tick()
		{
			if (lavaObject == null) return;
			if (Time.time < startTime) { lavaObject.transform.position = lowPosition; return; }
			float elapsed = Mathf.Max(MyTime.stageTimer - 5f, 0f) % cycleDuration;
			float progress;
			if (elapsed < riseTime) progress = Mathf.SmoothStep(0f, 1f, elapsed / riseTime);
			else if (elapsed < riseTime + stayTop) progress = 1f;
			else if (elapsed < riseTime + stayTop + lowerTime) progress = Mathf.SmoothStep(1f, 0f, (elapsed - riseTime - stayTop) / lowerTime);
			else progress = 0f;
			lavaObject.transform.position = Vector3.Lerp(lowPosition, topPosition, progress);
		}
	}
}
