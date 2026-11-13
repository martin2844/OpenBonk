using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGlovesCursed : ItemBase
	{
		public float procChancePerAmount = .05f;

		private float procChance;

		private float difficultyPerAmount = .1f;

		private float maxHpMultiplierPerAmount = .8f;

		private float baseDamageMultiplier = .85f;

		private float baseRadius = 4f;

		private static string damageSource = EItem.GloveCurse.ToString();

		private DamageContainer reuseDc = new DamageContainer(0f, damageSource);

		private EffectPlayer fx;

		private int maxProcsPerTick = 250;

		private int numProcsThisTick;

		public ItemGlovesCursed(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		protected override void OnInitOrAmountChanged()
		{
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, .5f, 8f);
			SetStat(new StatModifier { stat = EStat.MaxHealth, modifyType = EStatModifyType.Multiplication, modification = Mathf.Pow(maxHpMultiplierPerAmount, amount) });
			SetStat(new StatModifier { stat = EStat.Difficulty, modifyType = EStatModifyType.Flat, modification = difficultyPerAmount * amount });
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (numProcsThisTick >= maxProcsPerTick || !ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			numProcsThisTick++;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.transform.position, baseRadius, out Collider[] targets);
			if (count <= 0) return;
			reuseDc = WeaponUtility.GetDamageContainer(reuseDc, GetDamage(), 0f, damageSource, Vector3.zero, dc.enemy);
			float damage = reuseDc.damage;
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out Enemy enemy)) continue;
				reuseDc.damage = damage;
				reuseDc.enemy = enemy;
				enemy.DamageFromPlayerOther(reuseDc);
			}
			if (fx == null) fx = Object.Instantiate(EffectManager.Instance.gloveCurse).GetComponent<EffectPlayer>();
			fx.gameObject.SetActive(true);
			fx.transform.position = dc.enemy.transform.position;
			fx.Play();
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		private float GetDamage()
		{
			return baseDamageMultiplier * amount * MyPlayer.Instance.GetBaseDamage();
		}

		public override void Tick()
		{
			numProcsThisTick = 0;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object>
			{
				["chance"] = string.Format("{0}%", procChancePerAmount * 100f),
				["stat1"] = EnumUtility.EnumToReadable(EStat.Difficulty) ?? "",
				["value1"] = string.Format("+{0}%", difficultyPerAmount * 100f),
				["stat2"] = EnumUtility.EnumToReadable(EStat.MaxHealth) ?? "",
				["value2"] = string.Format("{0}x", maxHpMultiplierPerAmount)
			};
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}
	}
}
