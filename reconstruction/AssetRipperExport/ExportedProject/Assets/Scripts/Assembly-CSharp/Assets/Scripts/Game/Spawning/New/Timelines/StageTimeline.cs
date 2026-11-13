using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Game.Spawning.New.Timelines
{
	[Serializable]
	public class StageTimeline
	{
		public float stageTime = 600f;

		public float checkNewEnemyInterval = 60f;

		public List<EEnemy> startEnemies;

		public List<TimelineEvent> events;

		public EnemyData boss;

		public List<EEnemy> minibosses;

		public float GetStageTime()
		{
			return MapController.IsFinalBossStage() ? 600f : stageTime;
		}

		public void Sort()
		{
			events.Sort((x, y) => x.timeMinutes.CompareTo(y.timeMinutes));
		}
	}
}
