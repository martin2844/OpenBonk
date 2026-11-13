using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemEnergyCore : ItemBase
	{
		private int orbsPerAmount = 2;

		private int numOrbs = 4;

		private float range = 70f;

		private float cooldown = 4f;

		private float cooldownPerOrb = .2f;

		private float nextSpawnTime;

		private int orbsLeftToShoot;

		private float nextOrbTime;

		private string damageSource;

		protected override void OnInitOrAmountChanged()
		{
			numOrbs = 5 + orbsPerAmount * amount;
			cooldownPerOrb = Mathf.Min(.3f * numOrbs, 1.5f) / numOrbs;
		}

		private float GetDamage()
		{
			return MyPlayer.Instance != null ? 8f + 2f * MyPlayer.Instance.GetBaseDamage() : 0f;
		}

		public override void Tick()
		{
			if (orbsLeftToShoot <= 0 && MyTime.time >= nextSpawnTime)
			{
				nextSpawnTime = MyTime.time + cooldown;
				orbsLeftToShoot = numOrbs;
				nextOrbTime = MyTime.time;
			}
			if (orbsLeftToShoot > 0 && MyTime.time >= nextOrbTime)
			{
				FireOrb(numOrbs - orbsLeftToShoot);
				orbsLeftToShoot--;
				nextOrbTime = MyTime.time + cooldownPerOrb;
			}
		}

		private void FireOrb(int index)
		{
			if (PoolManager.Instance == null || PoolManager.Instance.orbPool == null || MyPlayer.Instance == null) return;
			var pool = PoolManager.Instance.orbPool;
			GameObject obj = pool.Get();
			if (obj == null) return;
			obj.SetActive(true);
			ItemProjectile projectile = obj.GetComponent<ItemProjectile>();
			if (projectile == null) { pool.Release(obj); return; }
			projectile.Set(MyPlayer.Instance.transform.position, GetDamage(), 1f, damageSource, pool, index, numOrbs, 5f, range);
		}

		public ItemEnergyCore(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.EnergyCore.ToString();
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}
	}
}
