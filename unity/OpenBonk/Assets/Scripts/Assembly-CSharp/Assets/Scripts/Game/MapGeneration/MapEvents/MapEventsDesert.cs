using System.Collections.Generic;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using Utility;

namespace Assets.Scripts.Game.MapGeneration.MapEvents
{
	public class MapEventsDesert : MapEvents
	{
		public bool ENABLE_SANDSTORM = true;

		private int numStorms;

		private List<float> stormTimes = new List<float>();

		private float minDuration = 8f;

		private float maxDuration = 18f;

		private static bool isActiveStorm;

		private float nextStormTime = float.MaxValue;

		private float stormOverAtTime;

		private int stormIndex;

		public static float currentStormStartedAtTime;

		private float tumbleweedSpawnInterval = 2.2f;

		private float lastSpawnedTumbleweedTime;

		private float minGapBetweenStorms => maxDuration + 5f;

		public override void Init()
		{
			isActiveStorm = false;
			if (MapController.IsFinalBossStage()) return;
			EffectManager.Instance.SpawnTornadoes(25);
			numStorms = MyRandom.random.Next(1, 4);
			float available = MapController.currentStage.stageTimeline.stageTime - 120f - numStorms * minGapBetweenStorms;
			if (available < 0f) return;
			var weights = new float[numStorms + 1]; float sum = 0f;
			for (int i = 0; i < weights.Length; i++) { weights[i] = -Mathf.Log(Random.value); sum += weights[i]; }
			for (int i = 0; i < weights.Length; i++) weights[i] = weights[i] / sum * available;
			float next = weights[0];
			for (int i = 0; i < numStorms; i++) { stormTimes.Add(next + 30f); next += minGapBetweenStorms + weights[i + 1]; }
			stormTimes.Sort();
			nextStormTime = stormTimes[0];
		}

		public override void Cleanup()
		{
			isActiveStorm = false;
		}

		public static bool IsActiveStorm()
		{
			return isActiveStorm;
		}

		public override void Tick()
		{
			if (MapController.IsFinalBossStage()) return;
			TickStorms();
			SpawnTumbleWeeds();
		}

		private void TickStorms()
		{
			if (!isActiveStorm && MyTime.stageTimer > nextStormTime) StartStorm();
			if (isActiveStorm && MyTime.stageTimer > stormOverAtTime) StopStorm();
		}

		private void SpawnTumbleWeeds()
		{
			if (lastSpawnedTumbleweedTime + tumbleweedSpawnInterval > MyTime.time) return;
			lastSpawnedTumbleweedTime = MyTime.time;
			EffectManager.Instance.SpawnTumbleWeeds(1);
		}

		private void StartStorm()
		{
			isActiveStorm = true;
			stormOverAtTime = MyTime.stageTimer + Random.Range(minDuration, maxDuration);
			currentStormStartedAtTime = MyTime.stageTimer;
			EffectManager.Instance.GetDesertStorm().FadeIn();
		}

		private void StopStorm()
		{
			isActiveStorm = false;
			stormIndex++;
			nextStormTime = stormIndex < stormTimes.Count ? stormTimes[stormIndex] : float.MaxValue;
			EffectManager.Instance.GetDesertStorm().FadeOut();
		}
	}
}
