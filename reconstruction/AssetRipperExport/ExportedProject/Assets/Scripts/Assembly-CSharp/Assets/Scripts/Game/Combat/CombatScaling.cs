using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Combat
{
	public static class CombatScaling
	{
		private static float speedMultiplicationPerMinute = 0.025f;

		private static float hpMultiplicationPerMinute = 0.1f;

		private static float damageMultiplicationPerMinute = 0.028f;

		private static float knockbackResistancePerMinute = 0.028f;

		private static float stageSpeedMultiplier = 0.15f;

		private static float stageDamageMultiplier = 0.5f;

		private static float stageKnockbackResMultiplier = 0.5f;

		private static float GetMinutes()
		{
			return MyTime.difficultyTimer / 60f;
		}

		public static float GetStageMultiplier()
		{
			return MapController.GetStageIndex();
		}

		public static float GetStageSpeedMultiplier()
		{
			int stage = MapController.GetStageIndex();
			if (stage == 0) return 0f;
			if (stage == 1) return 0.18f;
			if (stage == 2) return 0.46f;
			return Mathf.Pow(5f, stage) - 1f;
		}

		public static float GetStageHpMultiplier()
		{
			float difficulty = PlayerStats.GetStat(EStat.Difficulty);
			int stage = MapController.GetStageIndex();
			if (stage == 0) return 0f;
			float modifier = 1f + 0.45f * difficulty;
			if (stage == 1) return 27f * modifier;
			if (stage == 2) return 145f * modifier;
			return Mathf.Pow(20f, stage) - 1f;
		}

		public static float GetStageDamageMultiplier()
		{
			int stage = MapController.GetStageIndex();
			if (stage == 0) return 0f;
			if (stage == 1) return 2f;
			if (stage == 2) return 4f;
			return Mathf.Pow(20f, stage) - 1f;
		}

		public static float GetSpeedMultiplierAddition(out float baseAddition, out float swarmAddition, out float stageAddition)
		{
			baseAddition = GetMinutes() * speedMultiplicationPerMinute * PlayerStats.GetStat(EStat.EnemyScalingMultiplier);
			swarmAddition = GetSwarmSpeedMultiplier();
			stageAddition = GetStageSpeedMultiplier();
			return baseAddition + swarmAddition + stageAddition;
		}

		public static float GetHpMultiplierAddition(out float baseAddition, out float swarmAddition, out float stageAddition)
		{
			baseAddition = GetMinutes() * hpMultiplicationPerMinute * PlayerStats.GetStat(EStat.EnemyScalingMultiplier);
			swarmAddition = GetFinalSwarmHpMultiplier();
			stageAddition = GetStageHpMultiplier();
			return baseAddition + swarmAddition + stageAddition;
		}

		public static float GetDamageMultiplierAddition(out float baseAddition, out float swarmAddition, out float stageAddition)
		{
			baseAddition = GetMinutes() * damageMultiplicationPerMinute * PlayerStats.GetStat(EStat.EnemyScalingMultiplier);
			swarmAddition = GetFinalSwarmDamageMultiplier();
			stageAddition = GetStageDamageMultiplier();
			return baseAddition + swarmAddition + stageAddition;
		}

		public static float GetKnockbackResistanceMultiplierAddition(out float baseAddition, out float swarmAddition, out float stageAddition)
		{
			baseAddition = GetMinutes() * knockbackResistancePerMinute * PlayerStats.GetStat(EStat.EnemyScalingMultiplier);
			swarmAddition = GetFinalSwarmMultiplier();
			stageAddition = GetStageMultiplier() * stageKnockbackResMultiplier;
			return baseAddition + swarmAddition + stageAddition;
		}

		public static float GetFinalSwarmMultiplier()
		{
			return Mathf.Pow(2f, MyTime.finalSwarmTimer / 60f * 2f) - 1f;
		}

		public static float GetFinalSwarmHpMultiplier()
		{
			return Mathf.Pow(2f, MyTime.finalSwarmTimer / 60f * 2f) - 1f;
		}

		public static float GetFinalSwarmDamageMultiplier()
		{
			return Mathf.Pow(2f, MyTime.finalSwarmTimer / 60f) - 1f;
		}

		public static float GetSwarmSpeedMultiplier()
		{
			return Mathf.Clamp(GetFinalSwarmMultiplier() * 0.5f, 0f, 20f);
		}
	}
}
