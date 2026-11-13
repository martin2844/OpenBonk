using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGlovesPower : ItemBase
	{
		private float knockbackForce = 9999f;

		public float procChancePerAmount = .08f;

		private float procChance;

		private float baseDamageMultiplier = 1.25f;

		private float radiusPerAmount = 5f;

		private float radius = 9f;

		private static string damageSource = EItem.GlovePower.ToString();

		private DamageContainer reuseDc = new DamageContainer(0f, damageSource);

		private EffectPlayer fx;

		private float readyAtTime;

		private float cooldown = 1f;

		public ItemGlovesPower(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		protected override void OnInitOrAmountChanged()
		{
			cooldown = Mathf.Clamp(3.2f - amount * .2f, .2f, 1.5f);
			radius = 10f + radiusPerAmount * amount;
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, 1f, .5f);
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (readyAtTime > MyTime.time || !ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.transform.position, radius, out Collider[] targets);
			if (count <= 0) return;
			readyAtTime = MyTime.time + cooldown;
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out Enemy enemy)) continue;
				Vector3 direction = enemy.transform.position - MyPlayer.Instance.transform.position;
				reuseDc = WeaponUtility.GetDamageContainer(reuseDc, GetDamage(), 0f, damageSource, direction, enemy);
				reuseDc.knockback = knockbackForce;
				enemy.DamageFromPlayerOther(reuseDc);
			}
			if (fx == null)
			{
				fx = Object.Instantiate(EffectManager.Instance.glovePower).GetComponent<EffectPlayer>();
				fx.transform.SetParent(MyPlayer.Instance.feet);
				fx.transform.localPosition = Vector3.up * .01f;
			}
			fx.transform.localScale = Vector3.one * radius;
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["chance"] = string.Format("{0}%", procChancePerAmount * 100f) };
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
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
