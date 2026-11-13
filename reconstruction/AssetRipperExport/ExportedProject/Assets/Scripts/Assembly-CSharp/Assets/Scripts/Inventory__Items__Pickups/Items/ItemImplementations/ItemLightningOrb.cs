using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemLightningOrb : ItemBase
	{
		public float procChancePerAmount;

		private float procChance;

		private float stunChancePerAmount;

		private float stunChance;

		private float baseRadius;

		public float damageRatio;

		public float damageRatioPerAmount;

		private float foundEnemiesAtTime;

		private string damageSource;

		private DamageContainer yepDc;

		private List<int> availableIndexes;

		private int numEnemies;

		private Collider[] enemies;

		public ItemLightningOrb(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			procChancePerAmount = 0.25f;
			stunChancePerAmount = 0.25f;
			baseRadius = 40f;
			damageRatio = 0.4f;
			damageRatioPerAmount = 0.4f;
			damageSource = EItem.LightningOrb.ToString();
			yepDc = new DamageContainer(0f, damageSource);
			availableIndexes = new List<int>();
			enemies = new Collider[EnemyManager.maxNumEnemiesPooled + 1];
		}

		protected override void OnInitOrAmountChanged()
		{
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, 0.9f, 0.6f);
			stunChance = StatScaling.HyperbolicScaling(amount * stunChancePerAmount, 0.9f, 0.6f);
			damageRatio = amount * damageRatioPerAmount;
		}

		public override void Tick()
		{
			availableIndexes.Clear();
			numEnemies = -1;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (dc.element == EElement.Lightning) TryProcStun(dc);
			if (!ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			if (numEnemies == -1)
			{
				numEnemies = EnemyTargeting.GetEnemiesInRadiusSafe(this, MyPlayer.Instance.transform.position, baseRadius, out enemies);
				availableIndexes.Clear();
				for (int i = 0; i < numEnemies; i++) availableIndexes.Add(i);
			}
			if (numEnemies == 0 || availableIndexes.Count == 0) return;
			int selected = UnityEngine.Random.Range(0, availableIndexes.Count);
			if (!EnemyManager.Instance.GetEnemy(enemies[availableIndexes[selected]], out var target)) return;
			yepDc.Reuse(0f, damageSource);
			yepDc.damage = damageRatio * dc.damage;
			yepDc.element = EElement.Lightning;
			yepDc.enemy = target;
			WeaponUtility.LightningStrike(target, 0, yepDc, 8f * PlayerStats.GetStat(EStat.SizeMultiplier), 0f);
			TryProcStun(yepDc, 1f);
			if (target.IsDeadOrDyingNextFrame())
			{
				availableIndexes[selected] = availableIndexes[availableIndexes.Count - 1];
				availableIndexes.RemoveAt(availableIndexes.Count - 1);
			}
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		private void TryProcStun(DamageContainer dc, float overrideProcCoefficient = -1f)
		{
			float coefficient = overrideProcCoefficient == -1f ? dc.procCoefficient : overrideProcCoefficient;
			if (ItemUtility.TryProc(coefficient, stunChance)) dc.enemy.AddDebuff(EDebuff.Stun, dc, 3f);
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}%", procChancePerAmount * 100f) };
		}
	}
}
