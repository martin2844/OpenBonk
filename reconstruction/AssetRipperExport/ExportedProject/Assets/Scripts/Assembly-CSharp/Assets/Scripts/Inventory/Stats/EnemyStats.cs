using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory.Stats
{
	public static class EnemyStats
	{
		private static float overrideMaxSpeedAtTime = 720f;

		private static float maxSpeedMultiplier = 90f;

		private static float maxSpeedMultiplierOverride = 250f;

		private static int maxStunsAndFreezes = 15;

		private static float startStunImmunityAtTime = 1200f;

		private static float decreaseCcImmunityOverTime = 1800f;

		private static int lastFoundCcCap = 15;

		private static float lastFoundCcCapTime;

		private static float startPenetrationAtTime = 3600f;

		private static float penetrationPerMinute = 0.0002f;

		public static float GetHp(Enemy enemy)
		{
			EnemyData data = enemy.enemyData;
			int baseHp = data.hp;
			if (data.enemyName == EEnemy.FrogGreen || data.enemyName == EEnemy.FrogBlue || data.enemyName == EEnemy.FrogRed)
				baseHp = 500 + 20 * MyPlayer.Instance.inventory.GetCharacterLevel();
			else if (data.enemyName == EEnemy.GoldenSkeleton || data.enemyName == EEnemy.XpSkeleton)
				baseHp = 30 + MyPlayer.Instance.inventory.GetCharacterLevel();
			if (data.enemyName == EEnemy.GhostInvincible) return float.MaxValue;
			float hp = MapController.runConfig.GetEnemyHp(baseHp);
			float statMultiplier = PlayerStats.GetStat(EStat.EnemyHpMultiplier);
			float scaling = 1f + CombatScaling.GetHpMultiplierAddition(out _, out _, out _) +
				PlayerStats.GetStat(EStat.Difficulty) * 1.1f;
			if (enemy.IsEliteChallenge()) scaling *= 15f;
			else if (enemy.IsElite()) scaling *= 10f;
			else if (enemy.IsChallenge()) scaling *= 7f;
			if (enemy.IsBoss())
			{
				if (MapController.GetStageIndex() == 1) scaling *= 2.5f;
				else if (MapController.GetStageIndex() == 2) scaling *= 5f;
			}
			float result = hp * scaling * statMultiplier * OpenBonkMapLuaHooks.EnemyMultiplier(MapController.currentMap, MapController.index + 1, data.enemyName, 0);
			return float.IsNaN(result) || float.IsInfinity(result) ? float.MaxValue : Mathf.Max(0f, result);
		}

		public static float GetSpeed(EnemyData enemyData)
		{
			float baseSpeed = enemyData.speed;
			if (enemyData.enemyName == EEnemy.GhostKing) return baseSpeed;
			baseSpeed = MapController.runConfig.GetEnemySpeed(baseSpeed);
			float speedMultiplier = PlayerStats.GetStat(EStat.EnemySpeedMultiplier);
			float scaling = CombatScaling.GetSpeedMultiplierAddition(out _, out _, out _);
			float multiplier = 1f + scaling + PlayerStats.GetStat(EStat.Difficulty) * 0.5f;
			if (GameManager.Instance.isCrypt)
				multiplier += GetParkourMultiplier();
			return Mathf.Clamp(baseSpeed * multiplier * speedMultiplier * OpenBonkMapLuaHooks.EnemyMultiplier(MapController.currentMap, MapController.index + 1, enemyData.enemyName, 1), 0f, GetMaxSpeed());
		}

		private static float GetMaxSpeed()
		{
			if (MyTime.finalSwarmTimer < overrideMaxSpeedAtTime) return maxSpeedMultiplier;
			return Mathf.Min(maxSpeedMultiplier + (MyTime.finalSwarmTimer - overrideMaxSpeedAtTime) / 60f * 6f,
				maxSpeedMultiplierOverride);
		}

		public static float GetDamage(Enemy enemy)
		{
			float damage = MapController.runConfig.GetEnemyDamage(enemy.enemyData.damage);
			float statMultiplier = PlayerStats.GetStat(EStat.EnemyDamageMultiplier);
			float scaling = 1f + CombatScaling.GetDamageMultiplierAddition(out _, out _, out _) +
				PlayerStats.GetStat(EStat.Difficulty);
			if (enemy.IsElite()) scaling *= 1.5f;
			return Mathf.Clamp(damage * scaling * statMultiplier * OpenBonkMapLuaHooks.EnemyMultiplier(MapController.currentMap, MapController.index + 1, enemy.enemyData.enemyName, 2), 0f, float.MaxValue);
		}

		public static float GetEliteChance(EnemyData enemyData)
		{
			return enemyData.canBeElite ? PlayerStats.GetStat(EStat.EliteSpawnIncrease) * 0.006f : 0f;
		}

		public static float GetKnockbackResistance(Enemy enemy)
		{
			float multiplier = 1f + CombatScaling.GetKnockbackResistanceMultiplierAddition(out _, out _, out _) + PlayerStats.GetStat(EStat.Difficulty);
			if (enemy.IsElite()) multiplier *= 1.5f;
			return (enemy.enemyData.knockbackResistance + 1f + enemy.GetExtraKnockback()) * multiplier;
		}

		public static int GetCapCC()
		{
			if (lastFoundCcCapTime >= MyTime.time) return lastFoundCcCap;
			lastFoundCcCapTime = MyTime.time;
			lastFoundCcCap = maxStunsAndFreezes;
			if (MyTime.finalSwarmTimer >= startStunImmunityAtTime)
				lastFoundCcCap = Mathf.Max(0, maxStunsAndFreezes - (int)(((MyTime.finalSwarmTimer - startStunImmunityAtTime) / decreaseCcImmunityOverTime) * maxStunsAndFreezes));
			return lastFoundCcCap;
		}

		public static float GetEvasionAndArmorPenetration()
		{
			if (MyTime.finalSwarmTimer < startPenetrationAtTime) return 0f;
			return Mathf.FloorToInt((MyTime.finalSwarmTimer - startPenetrationAtTime) / 60f) * penetrationPerMinute;
		}

		private static float GetParkourMultiplier()
		{
			return Mathf.Max(0f, (MyTime.cryptTimer - GameManager.Instance.dungeonTimeToComplete) * 0.2f);
		}
	}
}
