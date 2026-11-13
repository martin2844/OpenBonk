using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Spawning.New.Summoners;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Spawning.New
{
	public class SummonerController
	{
		public StageSummoner stageSummoner;

		private SpecialSkeletonSummoner specialSkeletonSummoner;

		private BaseSummoner swarmSummoner;

		private BaseSummoner finalSwarmSummoner;

		private List<BaseSummoner> summoners;

		private int id;

		private int currentTimelineEvent = -1;

		private StageTimeline timeline;

		public static Action A_SwarmStarted;

		public static Action A_FinalSwarmStarted;

		public static Action A_MiniBoss;

		public static Action A_FinalSwarmStopped;

		private float lastAddedNewEnemyTime;

		private bool hadEventThisTick;

		private float swarmOverAtTime;

		private List<EEnemy> minibossPool = new List<EEnemy>();

		private bool isFinalSwarmStarted;

		private float bossDeadGracePeriod = 10f;

		private bool isFinalBossDead;

		private bool areSummonersDestroyed;

		public int minibossCount { get; private set; }

		public SummonerController()
		{
			summoners = new List<BaseSummoner>();
			timeline = MapController.currentStage.stageTimeline;
			if (!MapController.IsFinalBossStage())
			{
				stageSummoner = new StageSummoner(id++, timeline.startEnemies);
				specialSkeletonSummoner = new SpecialSkeletonSummoner(id++, new List<EEnemy>());
				AddSummoner(stageSummoner);
				AddSummoner(specialSkeletonSummoner);
			}
			InteractableBossSpawner.A_BossDefeated += OnBossDied;
		}

		public void Tick()
		{
			if (!GameManager.Instance.IsPlaying()) return;
			TickTimeline();
			foreach (BaseSummoner summoner in summoners) summoner.Tick();
		}

		public float GetMaxTimeLength()
		{
			return MapController.IsFinalBossStage() ? 600f : timeline.stageTime;
		}

		private void TickTimeline()
		{
			hadEventThisTick = false;
			if (MyTime.stageTimer >= GetStageTimeMax() && !isFinalSwarmStarted) StartFinalSwarm();
			if (AreEventsDisabled() || MapController.IsFinalBossStage() || isFinalSwarmStarted || isFinalBossDead) return;
			if (swarmSummoner != null && MyTime.stageTimer >= swarmOverAtTime)
			{
				DestroySummoner(swarmSummoner);
				swarmSummoner = null;
			}
			int next = currentTimelineEvent + 1;
			if (next < timeline.events.Count && MyTime.stageTimer >= timeline.events[next].GetTimeSeconds())
			{
				StartEvent(next);
				hadEventThisTick = true;
			}
			if (MyTime.stageTimer >= lastAddedNewEnemyTime + timeline.checkNewEnemyInterval) TryAddNewEnemyCard();
		}

		public float GetStageTimeMax()
		{
			return GameManager.Instance.HasEnteredBossRoom() || MapController.IsFinalBossStage() ? 600f : timeline.stageTime;
		}

		private bool AreEventsDisabled()
		{
			return MapController.IsFinalBossStage() || GameManager.Instance.HasEnteredBossRoom();
		}

		private void TryAddNewEnemyCard()
		{
			lastAddedNewEnemyTime = Mathf.RoundToInt(MyTime.stageTimer);
			if (CanAddNewEnemyCard() && stageSummoner.AddEnemyToPoolAndPickNewCard(null, out EEnemy selected))
				stageSummoner.SetWaveMode(new List<EEnemy> { selected }, timeline.checkNewEnemyInterval * 0.5f);
		}

		public void StartEvent(int eventIndex)
		{
			currentTimelineEvent = eventIndex;
			TimelineEvent timelineEvent = timeline.events[eventIndex];
			switch (timelineEvent.eTimelineEvent)
			{
				case ETimelineEvent.EMiniBoss: EventMiniboss(timelineEvent); break;
				case ETimelineEvent.ESwarm: EventSwarm(timelineEvent); break;
			}
		}

		private void EventSwarm(TimelineEvent timelineEvent)
		{
			swarmSummoner = new SwarmSummoner(id++, timelineEvent.enemies);
			AddSummoner(swarmSummoner);
			stageSummoner.SetSlowmode(0f, timelineEvent.duration);
			swarmOverAtTime = timelineEvent.GetTimeSeconds() + timelineEvent.duration - 1f;
			A_SwarmStarted?.Invoke();
		}

		public void EventMiniboss(TimelineEvent timelineEvent)
		{
			if (minibossPool.Count == 0) minibossPool.AddRange(timeline.minibosses);
			if (minibossPool.Count == 0) return;
			int index = global::Utility.MyRandom.random.Next(minibossPool.Count);
			EEnemy name = minibossPool[index];
			minibossPool.RemoveAt(index);
			EnemyData data = DataManager.Instance.GetEnemyData(name);
			for (int i = 0; i < GameManager.Instance.bossCurses + 1; i++)
			{
				Vector3 position = SpawnPositions.GetEnemySpawnPosition(null, 50, true, 15f);
				if ((position - SpawnPositions.INVALID_POS).sqrMagnitude < 0.0000000001f)
					position = Assets.Scripts.Actors.Player.MyPlayer.Instance.transform.position;
				EnemyManager.Instance.SpawnBoss(data.enemyName, id, EEnemyFlag.SummonerMiniboss, position);
			}
			stageSummoner.SetSlowmode(0.25f, 20f);
			minibossCount++;
			A_MiniBoss?.Invoke();
		}

		private bool CanAddNewEnemyCard()
		{
			return !hadEventThisTick && !isFinalSwarmStarted && swarmSummoner == null;
		}

		public bool IsFinalSwarm()
		{
			return isFinalSwarmStarted;
		}

		private void StartFinalSwarm()
		{
			if (isFinalSwarmStarted) return;
			isFinalSwarmStarted = true;
			TryStopSummoners();
			finalSwarmSummoner = new SwarmSummoner(id++, new List<EEnemy> { EEnemy.Ghost });
			AddSummoner(finalSwarmSummoner);
			A_FinalSwarmStarted?.Invoke();
		}

		public void StopFinalSwarm()
		{
			isFinalSwarmStarted = false;
			areSummonersDestroyed = false;
			TryStopSummoners();
			stageSummoner = new StageSummoner(id++, timeline.startEnemies);
			AddSummoner(stageSummoner);
			areSummonersDestroyed = false;
		}

		private void AddSummoner(BaseSummoner summoner)
		{
			summoners.Add(summoner);
		}

		private void DestroySummoner(BaseSummoner summoner)
		{
			summoners.Remove(summoner);
			summoner.Cleanup();
		}

		public List<Enemy> SpawnStageBoss(Vector3 pos)
		{
			stageSummoner?.SetSlowmode(0.5f, 30f);
			List<Enemy> bosses = new List<Enemy>();
			for (int i = 0; i < GameManager.Instance.bossCurses + 1; i++)
			{
				Vector3 offset = UnityEngine.Random.insideUnitSphere;
				offset.y = 0f;
				pos += offset * 7f;
				Enemy boss = EnemyManager.Instance.SpawnBoss(timeline.boss.enemyName, -1, EEnemyFlag.StageBoss, pos);
				if (boss != null) bosses.Add(boss);
			}
			return bosses;
		}

		private void OnBossDied(bool isLastStage)
		{
			TryStopSummoners();
			isFinalBossDead = true;
			float minimum = GetStageTimeMax() - bossDeadGracePeriod;
			if (MyTime.stageTimer < minimum) MyTime.stageTimer = minimum;
		}

		private void TryStopSummoners()
		{
			if (areSummonersDestroyed) return;
			areSummonersDestroyed = true;
			foreach (BaseSummoner summoner in new List<BaseSummoner>(summoners)) DestroySummoner(summoner);
			summoners = new List<BaseSummoner>();
		}

		public void Cleanup()
		{
			InteractableBossSpawner.A_BossDefeated -= OnBossDied;
			foreach (BaseSummoner summoner in summoners) summoner.Cleanup();
			summoners.Clear();
		}
	}
}
