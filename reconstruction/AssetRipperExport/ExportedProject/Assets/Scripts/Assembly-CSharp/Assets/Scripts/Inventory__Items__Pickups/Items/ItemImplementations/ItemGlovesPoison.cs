using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGlovesPoison : ItemBase
	{
		public float readyAtTime;

		private float cooldown = 8.5f;

		private float baseDamageMultiplier = 1.5f;

		private float baseRadius = 15f;

		private int poisonStacksPerAmount = 10;

		private static string damageSource = EItem.GlovePoison.ToString();

		private DamageContainer reuseDc = new DamageContainer(0f, damageSource);

		private EffectPlayer fx;

		public ItemGlovesPoison(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (readyAtTime > MyTime.time) return;
			readyAtTime = MyTime.time + .02f;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, dc.enemy.transform.position, baseRadius, out Collider[] targets);
			if (count <= 0) return;
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(targets[i], out Enemy enemy)) continue;
				reuseDc = WeaponUtility.GetDamageContainer(reuseDc, GetDamage(), 0f, damageSource, Vector3.zero, enemy);
				enemy.DamageFromPlayerOther(reuseDc);
				enemy.AddDebuff(EDebuff.Poison, reuseDc, 5f, poisonStacksPerAmount);
			}
			readyAtTime = MyTime.time + cooldown;
			if (fx == null) fx = Object.Instantiate(EffectManager.Instance.glovePoison).GetComponent<EffectPlayer>();
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

		protected override void OnInitOrAmountChanged()
		{
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
