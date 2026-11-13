using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemToxicBarrel : ItemBase
	{
		public static Action<float> A_OnUse;

		private float radius;

		private float radiusPerAmount = 1f;

		private int poisonStacksPerAmount = 5;

		private int poisonStacks;

		private float cooldown = 0.25f;

		private float readyAtTime;

		private float poisonDuration = 5f;

		private string damageSource;

		private DamageContainer dc;

		protected override void OnInitOrAmountChanged()
		{
			radius = 7f + radiusPerAmount * amount;
			poisonStacks = poisonStacksPerAmount * amount;
		}

		public override void Init()
		{
			PlayerHealth.A_TakeDamage += OnTakeDamage;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_TakeDamage -= OnTakeDamage;
		}

		private void OnTakeDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
		{
			if (readyAtTime > MyTime.time) return;
			readyAtTime = MyTime.time + cooldown;
			Activate();
		}

		private void Activate()
		{
			if (MyPlayer.Instance == null || EnemyManager.Instance == null) return;
			float actualRadius = radius * PlayerStats.GetStat(EStat.SizeMultiplier);
			int count = EnemyTargeting.GetEnemiesInRadiusSafe(this, MyPlayer.Instance.transform.position, actualRadius, out Collider[] targets);
			for (int i = 0; i < count; i++)
				if (EnemyManager.Instance.GetEnemy(targets[i], out var enemy)) enemy.AddDebuff(EDebuff.Poison, null, poisonDuration, poisonStacks);
			A_OnUse?.Invoke(actualRadius);
		}

		public override void Tick()
		{
		}

		public ItemToxicBarrel(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.ToxicBarrel.ToString();
			dc = new DamageContainer(0.5f, damageSource);
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
