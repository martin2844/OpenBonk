using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemElectricPlug : ItemBase
	{
		public static string damageSource = EItem.ElectricPlug.ToString();

		private DamageContainer reuseDc = new DamageContainer(.5f, EItem.ElectricPlug.ToString());

		private float radius = 13f;

		private float radiusPerAmount = 4f;

		private int targets = 15;

		private int targetsPerAmount = 4;

		private int targetsDefault = 6;

		protected override void OnInitOrAmountChanged()
		{
			radius = 12f + radiusPerAmount * PlayerStats.GetStat(EStat.SizeMultiplier);
			targets = targetsDefault + (amount - 1) * targetsPerAmount;
		}

		private float GetDamage()
		{
			return MyPlayer.Instance.GetBaseDamage() * .8f;
		}

		public override void Init()
		{
			PlayerHealth.A_DamagePlayerCalled += OnPlayerHit;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_DamagePlayerCalled -= OnPlayerHit;
		}

		private void OnPlayerHit()
		{
			var player = MyPlayer.Instance;
			if (player == null || player.inventory == null || EnemyManager.Instance == null || GameManager.Instance == null) return;
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, player.transform.position, radius, out Collider[] colliders);
			int struck = 0;
			for (int i = 0; i < count; i++)
			{
				if (!EnemyManager.Instance.GetEnemy(colliders[i], out var enemy) || enemy == null) continue;
				Vector3 direction = (enemy.transform.position - player.transform.position).normalized;
				reuseDc = WeaponUtility.GetDamageContainer(reuseDc, GetDamage(), .5f, damageSource, direction, enemy);
				reuseDc.element = EElement.Lightning;
				reuseDc.knockback = PlayerStats.GetStat(EStat.KnockbackMultiplier) * 1.5f;
				enemy.DamageFromPlayerOther(reuseDc);
				if (EffectManager.Instance != null && PoolManager.Instance != null)
					EffectManager.Instance.EnemyHitEffect(enemy.GetCenterPosition(), Vector3.zero, true, damageSource, null, true);
				if (++struck >= targets) break;
			}
			if (EffectManager.Instance != null && EffectManager.Instance.electricPlugFx != null)
				EffectManager.Instance.ElectricalPlugEffect();
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public ItemElectricPlug(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
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
