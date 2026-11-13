using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityShadowstep : PassiveAbility
	{
		private float evadePerLevel = .005f;

		public const string damageSource = "Shadowstep";

		private DamageContainer reuseDc = new DamageContainer(.1f, damageSource);

		public override void Init()
		{
			PlayerXp.A_LevelUp += OnLevelup;
			PlayerHealth.A_Evaded += OnEvade;
		}

		public override void Cleanup()
		{
			PlayerXp.A_LevelUp -= OnLevelup;
			PlayerHealth.A_Evaded -= OnEvade;
		}

		public override void Tick()
		{
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.Evasion, modifyType = EStatModifyType.Flat, modification = level * evadePerLevel });
		}

		private void OnEvade(Enemy enemy)
		{
			if (enemy == null) return;
			reuseDc.Reuse(.1f, damageSource);
			reuseDc.enemy = enemy;
			reuseDc.damageEffect = EDamageEffect.Execute;
			reuseDc.isExecute = true;
			reuseDc.damage = !enemy.IsBoss() && enemy.enemyData.canBeExecuted ? enemy.hp : enemy.maxHp * .02f;
			enemy.DamageFromPlayerOther(reuseDc);
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Shadowstep;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.Evasion) ?? "",
				["value1"] = string.Format("{0}%", evadePerLevel * 100f),
				["Execute"] = LocalizationUtility.GetLocalizedString("DamageSources", EDamageEffect.Execute.ToString()) ?? ""
			});
		}
	}
}
