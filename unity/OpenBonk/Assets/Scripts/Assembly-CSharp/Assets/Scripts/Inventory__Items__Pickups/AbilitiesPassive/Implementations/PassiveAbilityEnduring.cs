using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityEnduring : PassiveAbility
	{
		private float sizePerLevel = .0075f;

		private float maxSize = 4f;

		private float damageMultiplierPerFrozenEnemy = .015f;

		private float maxDamageFromFrozenEnemies = 3f;

		private int lastNumFrozenEnemies;

		public override void Init()
		{
			DebuffIce.numFrozenEnemies = 0;
			PlayerXp.A_LevelUp += OnLevelup;
			GameManager.A_StageStarted += OnStageStarted;
		}

		private void OnStageStarted()
		{
			DebuffIce.numFrozenEnemies = 0;
		}

		public override void Tick()
		{
			if (lastNumFrozenEnemies == DebuffIce.numFrozenEnemies) return;
			lastNumFrozenEnemies = DebuffIce.numFrozenEnemies;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Multiplication,
				modification = Mathf.Min(maxDamageFromFrozenEnemies, 1f + lastNumFrozenEnemies * damageMultiplierPerFrozenEnemy) });
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.SizeMultiplier, modifyType = EStatModifyType.Flat,
				modification = Mathf.Min(maxSize, level * sizePerLevel) });
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
			GameManager.A_StageStarted -= OnStageStarted;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Enduring;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.SizeMultiplier) ?? "",
				["value1"] = string.Format("{0}%", sizePerLevel * 100f),
				["damage"] = EnumUtility.EnumToReadable(EStat.DamageMultiplier) ?? ""
			});
		}
	}
}
