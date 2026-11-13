using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;
using Utility;

namespace Assets.Scripts.Game.Combat
{
	public class DamageUtility
	{
		public const string enemyDamageSource = "Enemy";

		private static DamageContainer reuseDc = new DamageContainer(0f, enemyDamageSource);

		public static DamageContainer GetPlayerDamage(Enemy enemy, Vector3 direction, DcFlags flags)
		{
			float damage = EnemyStats.GetDamage(enemy);
			float knockback = enemy.enemyData.GetKnockback();
			if (enemy.enemyData.isPoison && MyPlayer.Instance != null && MyPlayer.Instance.inventory?.statusEffects != null)
				MyPlayer.Instance.inventory.statusEffects.PoisonPlayer(6f);
			return GetPlayerDamage(damage, knockback, direction, enemy, enemyDamageSource, flags);
		}

		public static DamageContainer GetPlayerDamage(float damage, float knockback, Vector3 direction, Enemy enemy, string damageSource, DcFlags flags)
		{
			reuseDc.Reuse(0f, damageSource);
			reuseDc.flags = flags;
			reuseDc.enemy = enemy;
			reuseDc.knockback = knockback;
			reuseDc.direction = direction;
			float armor = PlayerStats.GetStat(EStat.Armor);
			if ((flags & DcFlags.FinalBossDamage) != 0) armor *= 0.75f;
			else if ((flags & DcFlags.BossDamage) != 0) armor *= 0.5f;
			float reduced = Mathf.Clamp(damage * ((flags & DcFlags.IgnoreArmor) != 0 ? 1f : 1f - armor), 1f, int.MaxValue);
			reuseDc.damage = reduced * (1f - PlayerStats.GetStat(EStat.DamageReductionMultiplier));
			reuseDc.damageBlockedByArmor = (int)(damage - reduced);
			return reuseDc;
		}

		public static bool CheckEvade(Enemy enemy)
		{
			return MyRandom.random.NextDouble() <= Mathf.Max(0f, PlayerStats.GetStat(EStat.Evasion));
		}

		public static bool GetCritDamageMultiplier(float critChance, out float multiplier)
		{
			multiplier = 1f;
			int criticalLevels = Mathf.FloorToInt(critChance);
			if (MyRandom.random.NextDouble() < critChance - criticalLevels) criticalLevels++;
			if (criticalLevels == 0) return false;
			multiplier = criticalLevels == 1 ? 2f : 1f + criticalLevels + Mathf.Pow(criticalLevels * 0.5f, 2f);
			return true;
		}

		public static void ApplyExecute(DamageContainer dc)
		{
			dc.damageEffect = EDamageEffect.Execute;
			dc.isExecute = true;
			dc.damage = !dc.enemy.IsBoss() && dc.enemy.enemyData.canBeExecuted ? dc.enemy.hp : dc.enemy.maxHp * 0.02f;
		}
	}
}
