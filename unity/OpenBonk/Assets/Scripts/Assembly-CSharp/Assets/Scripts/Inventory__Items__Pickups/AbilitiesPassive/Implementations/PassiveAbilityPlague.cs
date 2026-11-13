using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityPlague : PassiveAbility
	{
		private int levelsPerStack = 5;

		private float radius = 4f;

		private float maxRadius = 12f;

		private float radiusPerLevel = .05f;

		private float duration = 4f;

		private float poisonDamagePerLevel = .01f;

		public override void Init()
		{
			Enemy.A_EnemyDied += OnEnemyDied;
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Cleanup()
		{
			Enemy.A_EnemyDied -= OnEnemyDied;
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		private void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			if (!enemy.debuffs.ContainsKey(EDebuff.Poison)) return;
			int stacks = Mathf.FloorToInt((float)MyPlayer.Instance.inventory.GetCharacterLevel() / levelsPerStack) + 1;
			Vector3 center = enemy.GetCenterPosition();
			float size = PlayerStats.GetStat(EStat.SizeMultiplier);
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, center, radius * size, out Collider[] targets);
			for (int i = 0; i < count; i++)
				if (EnemyManager.Instance.GetEnemy(targets[i], out Enemy target))
					target.AddDebuff(EDebuff.Poison, null, duration, stacks);
			GameObject effect = PoolManager.Instance != null && PoolManager.Instance.poisonPool != null ? PoolManager.Instance.poisonPool.Get() : null;
			if (effect != null) { effect.transform.position = center; effect.transform.localScale = Vector3.one * (2f * size); }
		}

		public override void Tick()
		{
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.PoisonDamageMultiplier, modifyType = EStatModifyType.Flat, modification = level * poisonDamagePerLevel });
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Plague;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.PoisonDamageMultiplier) ?? "",
				["value1"] = string.Format("{0}%", poisonDamagePerLevel * 100f)
			});
		}
	}
}
