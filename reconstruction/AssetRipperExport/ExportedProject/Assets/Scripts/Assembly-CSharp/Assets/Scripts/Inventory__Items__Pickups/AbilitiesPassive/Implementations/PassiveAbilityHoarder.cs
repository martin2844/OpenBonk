using UnityEngine.Localization;
using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityHoarder : PassiveAbility
	{
		private float hoverFallSpeed = 12f;

		private float spawnIntervalStartSeconds = 60f;

		private float spawnIntervalEndSeconds = 120f;

		private float spawnIntervalIncreasePerDrop = 2.5f;

		private float spawnIntervalSeconds = 60f;

		private float currentTimer;

		private int accumulatedTicks;

		private int maxAccumulatedTicks = 1000;

		private float lastAccumulatedTime;

		public override void Cleanup()
		{
		}

		public override void Init()
		{
			currentTimer = spawnIntervalSeconds = spawnIntervalStartSeconds;
		}

		public override void Tick()
		{
			ChestTick();
			MovementTick();
		}

		private void MovementTick()
		{
			if (UiManager.Instance.pause.IsPaused() || accumulatedTicks >= maxAccumulatedTicks || !MyPlayer.Instance.playerInput.IsHoldingJump()) return;
			var movement = MyPlayer.Instance.playerMovement;
			var body = movement.GetRb();
			if (movement.HoarderFallSpeed > 0f)
			{
				body.AddForce(Vector3.up * body.mass * .33f, ForceMode.Impulse);
				if (MyTime.time > lastAccumulatedTime) accumulatedTicks = 0;
				accumulatedTicks++;
				lastAccumulatedTime = MyTime.time;
			}
			if (movement.CanFloat())
			{
				Vector3 velocity = body.velocity;
				velocity.y = Mathf.Max(velocity.y, -hoverFallSpeed);
				body.velocity = velocity;
			}
		}

		private void ChestTick()
		{
			if (GameManager.Instance == null || MyPlayer.Instance == null || GameManager.Instance.isCrypt || MyTime.paused || MyPlayer.Instance.IsDead() || !GameManager.Instance.IsPlaying() || ChallengesTracker.HasChallengeModifier("no_items")) return;
			currentTimer -= MyTime.fixedDeltaTime;
			if (currentTimer > 0f) return;
			if (spawnIntervalSeconds < spawnIntervalEndSeconds) spawnIntervalSeconds += spawnIntervalIncreasePerDrop;
			currentTimer = spawnIntervalSeconds;
			Vector3 position = SpawnPositions.GetEnemySpawnPositionBiased(null, 0f, 50, 35f);
			if (position == SpawnPositions.INVALID_POS) MyLogger.LogError("FAILED TO FIND RANDOM POSITION ON MAP...");
			else EffectManager.Instance.SpawnChestForcePosition(EffectManager.Instance.openChestNormal, position);
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Hoarder;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["value1"] = string.Format("{0}", spawnIntervalStartSeconds),
				["value2"] = string.Format("{0}", spawnIntervalEndSeconds)
			});
		}
	}
}
