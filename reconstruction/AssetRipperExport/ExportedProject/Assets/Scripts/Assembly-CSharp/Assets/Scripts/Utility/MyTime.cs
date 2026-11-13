using System;
using Assets.Scripts.Managers;
using UnityEngine;

namespace Assets.Scripts.Utility
{
	public static class MyTime
	{
		public static bool paused;

		public static float time;

		public static float deltaTime;

		public static float fixedDeltaTime;

		public static int tick;

		public static int unpauseTick;

		public static float stageTimer;

		public static float runTimer;

		public static float finalSwarmTimer;

		public static float difficultyTimer;

		public static float cryptTimer;

		public static Action<bool> A_Pause;

		public static Action A_Tick;

		public static Action A_TimeScaleChange;

		private static float timescaleTimeRemaining;

		public static float timeScale { get; private set; } = 1f;

		public static void Init()
		{
			GameManager.A_StageStarted += OnNewStageStarted;
			GameManager.A_RunStarted += OnNewRunStarted;
		}

		public static void Cleanup()
		{
			GameManager.A_StageStarted -= OnNewStageStarted;
			GameManager.A_RunStarted -= OnNewRunStarted;
		}

		private static void OnNewRunStarted()
		{
			Unpause();
			stageTimer = 0f;
			runTimer = 0f;
			difficultyTimer = 0f;
			timeScale = 1f;
		}

		private static void OnNewStageStarted()
		{
			if (MapController.IsFirstStage())
			{
				runTimer = 0f;
				difficultyTimer = 0f;
			}
			else runTimer = Mathf.FloorToInt(runTimer);
			Unpause();
			stageTimer = 0f;
			finalSwarmTimer = 0f;
			tick = 0;
			unpauseTick = 0;
			timeScale = 1f;
			timescaleTimeRemaining = 0f;
		}

		public static void Update()
		{
			deltaTime = paused ? 0f : Time.deltaTime * timeScale;
			time += deltaTime;
			if (GameManager.Instance != null && GameManager.Instance.IsPlaying())
			{
				if (!GameManager.Instance.isCrypt) stageTimer += deltaTime;
				else if (GameManager.Instance.isDungeonTimerStarted) cryptTimer += deltaTime;
				runTimer += deltaTime;
				if (GameManager.Instance.IsFinalSwarm()) finalSwarmTimer += deltaTime;
				if (!MapController.IsFinalBossStage()) difficultyTimer += deltaTime;
			}
			if (timescaleTimeRemaining > 0f && !paused)
			{
				timescaleTimeRemaining -= Time.deltaTime;
				if (timescaleTimeRemaining <= 0f) ResetTimeScale();
			}
		}

		public static void FixedUpdate()
		{
			if (paused) { fixedDeltaTime = 0f; return; }
			fixedDeltaTime = Time.fixedDeltaTime * timeScale;
			tick++;
			A_Tick?.Invoke();
		}

		public static void SetTimeScale(float newTimeScale, float duration)
		{
			timeScale = newTimeScale;
			timescaleTimeRemaining = duration;
			A_TimeScaleChange?.Invoke();
		}

		private static void ResetTimeScale()
		{
			timeScale = 1f;
			A_TimeScaleChange?.Invoke();
		}

		public static void Pause()
		{
			paused = true;
			Physics.simulationMode = SimulationMode.Script;
			A_Pause?.Invoke(true);
		}

		public static void Unpause()
		{
			paused = false;
			Physics.simulationMode = SimulationMode.FixedUpdate;
			A_Pause?.Invoke(false);
			unpauseTick = tick;
		}

		public static void StartCryptBoss()
		{
			stageTimer = 0f;
		}
	}
}
