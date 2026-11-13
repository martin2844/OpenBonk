using System;
using System.Collections.Generic;
using Actors.Enemies;
using UnityEngine;

namespace Assets.Scripts.Game.Spawning.New.Timelines
{
	[Serializable]
	public class TimelineEvent
	{
		public ETimelineEvent eTimelineEvent;

		public List<EEnemy> enemies;

		public float timeMinutes;

		public float duration;

		public float GetTimeSeconds()
		{
			return Mathf.RoundToInt(timeMinutes * 60f);
		}
	}
}
