using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations
{
	public class PassiveAbilityZooma : PassiveAbility
	{
		private float chargePerMeter = .006f;

		private float checkInterval = .5f;

		private float nextCheckTime;

		private Vector3 lastPos;

		private float accumulatedCharge;

		private float attractionAddPerLevel = .02f;

		private DamageContainer reuseDc = new DamageContainer(1f, "Zap");

		private string damageSource = "Zap";

		public override void Init()
		{
			Enemy.A_Damage += OnEnemyDamage;
			PlayerXp.A_LevelUp += OnLevelup;
		}

		public override void Cleanup()
		{
			Enemy.A_Damage -= OnEnemyDamage;
			PlayerXp.A_LevelUp -= OnLevelup;
		}

		private void OnLevelup(int level)
		{
			SetStat(new StatModifier { stat = EStat.PickupRange, modifyType = EStatModifyType.Addition, modification = level * attractionAddPerLevel });
		}

		private void OnEnemyDamage(Enemy enemy, DamageContainer dc)
		{
			if (accumulatedCharge < 1f) return;
			accumulatedCharge = 0f;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, enemy.transform.position, 6f, out Collider[] targets);
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out var target)) continue;
				Vector3 direction = (enemy.transform.position - MyPlayer.Instance.transform.position).normalized;
				reuseDc = WeaponUtility.GetDamageContainer(reuseDc, GetDamage(), .5f, damageSource, direction, target);
				reuseDc.element = EElement.Lightning;
				target.DamageFromPlayerOther(reuseDc);
			}
			EffectManager.Instance.ZapEffect(enemy.GetCenterPosition());
		}

		private float GetDamage()
		{
			return MyPlayer.Instance.GetBaseDamage() * 3f;
		}

		public override void Tick()
		{
			if (MyTime.time < nextCheckTime || accumulatedCharge >= 1f) return;
			nextCheckTime = MyTime.time + checkInterval;
			Vector3 position = MyPlayer.Instance.transform.position;
			accumulatedCharge = Mathf.Min(1f, accumulatedCharge + Vector3.Distance(position, lastPos) * chargePerMeter);
			lastPos = position;
		}

		public float GetProgress()
		{
			return accumulatedCharge;
		}

		public override EPassive GetPassiveType()
		{
			return EPassive.Zap;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string>
			{
				["stat1"] = EnumUtility.EnumToReadable(EStat.PickupRange) ?? "",
				["value1"] = string.Format("{0}%", attractionAddPerLevel * 100f)
			});
		}
	}
}
