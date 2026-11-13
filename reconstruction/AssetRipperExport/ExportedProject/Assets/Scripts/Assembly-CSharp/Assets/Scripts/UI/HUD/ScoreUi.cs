using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.UI.HUD
{
	public class ScoreUi : MonoBehaviour
	{
		public RandomSfx scoreSound;

		public RandomSfx negativeSound;

		private bool moveDesc;

		private Queue<ScoreContainer> scoreQueue = new Queue<ScoreContainer>();

		private List<ScoreUiPrefab> prefabs = new List<ScoreUiPrefab>();

		public GameObject prefab;

		private bool isActive;

		private float readyTime;

		public void AddScore(string description, string header, bool isPositive = true, bool useSfx = true, float sizeMultiplier = 1f)
		{
			scoreQueue.Enqueue(new ScoreContainer(header, description, isPositive, useSfx, sizeMultiplier));
			if (MyTime.time >= readyTime) readyTime = MyTime.time + Time.deltaTime;
		}

		public void AddScore(StatModifier statModifier, bool isPositive, bool useSfx = true, float sizeMultiplier = 1f)
		{
			string description = StatUtility.EncapsulateNumber(StatUtility.GetModificationString(statModifier, true), MyColorUtility.GetStatTextColor(isPositive)) ?? "";
			string header = EnumUtility.EnumToReadable(statModifier.stat) ?? "";
			if (!isPositive) header = "<color=#" + MyColorUtility.ColorToHex(new Color(1f, .9f, .9f, 1f)) + ">" + header;
			AddScore(description, header, isPositive, useSfx, sizeMultiplier);
		}

		private void Update()
		{
			if (MyTime.time >= readyTime && scoreQueue.Count > 0) SetScore();
		}

		private void SetScore()
		{
			readyTime = MyTime.time + .5f;
			ScoreContainer score = scoreQueue.Dequeue();
			if (score.useSfx) (score.isPositive ? scoreSound : negativeSound).Play();
			ScoreUiPrefab available = null;
			foreach (ScoreUiPrefab candidate in prefabs)
				if (!candidate.gameObject.activeInHierarchy) { available = candidate; break; }
			if (available == null)
			{
				available = Instantiate(prefab, prefab.transform.parent).GetComponent<ScoreUiPrefab>();
				prefabs.Add(available);
			}
			available.SetScore(score.description, score.header, score.sizeMultiplier);
		}

		public int GetQueueCount()
		{
			return scoreQueue.Count;
		}
	}
}
