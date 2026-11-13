using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSoulHarvester : ItemBase
	{
		private string damageSource = EItem.SoulHarvester.ToString();

		public const int maxProjectiles = 100;

		private int numProjectiles = 2;

		private float damageMultiplier;

		private float range = 50f;

		private Dictionary<GameObject, ItemProjectile> projectileLookup = new Dictionary<GameObject, ItemProjectile>(maxProjectiles);

		public override void Init()
		{
			Enemy.A_EnemyReleasedFromPool += OnEnemyDied;
		}

		public override void Cleanup()
		{
			Enemy.A_EnemyReleasedFromPool -= OnEnemyDied;
		}

		protected override void OnInitOrAmountChanged()
		{
			damageMultiplier = amount;
		}

		private float GetDamage()
		{
			return MyPlayer.Instance.GetBaseDamage() * damageMultiplier * 1.75f - 9f;
		}

		private void OnEnemyDied(Enemy enemy)
		{
			if (enemy == null) return;
			for (int i = 0; i < numProjectiles; i++) SpawnProjectile(enemy.transform.position);
		}

		private void SpawnProjectile(Vector3 pos)
		{
			if (PoolManager.Instance == null || PoolManager.Instance.angrySoulPool == null || MyPlayer.Instance == null) return;
			var pool = PoolManager.Instance.angrySoulPool;
			GameObject obj = pool.Get();
			if (obj == null) return;
			if (!projectileLookup.TryGetValue(obj, out ItemProjectile projectile))
				projectileLookup[obj] = projectile = obj.GetComponent<ItemProjectile>();
			if (projectile == null) { pool.Release(obj); return; }
			projectile.Set(pos, GetDamage(), .85f, damageSource, pool, 0, 1, 3.5f, range);
		}

		public override void Tick()
		{
		}

		public ItemSoulHarvester(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
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
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}", numProjectiles) };
		}
	}
}
