using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemMirror : ItemBase
	{
		public static Action<bool> A_MirrorReady;

		public float cooldown = 8f;

		private float minCooldown = 4f;

		private float damageMultiplier = 1f;

		private float damagePerAmount = .25f;

		private float lastReflectedTime;

		private bool canReflect;

		private string damageSource = EItem.Mirror.ToString();

		private DamageContainer reuseDc = new DamageContainer(1f, EItem.Mirror.ToString());

		protected override void OnInitOrAmountChanged()
		{
			cooldown = Mathf.Max(minCooldown, 8f - amount);
			damageMultiplier = 1f + amount * damagePerAmount;
			A_MirrorReady?.Invoke(true);
		}

		public override void Init()
		{
			PlayerHealth.A_CheckStopDamage += OnCheckStopDamage;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_CheckStopDamage -= OnCheckStopDamage;
			A_MirrorReady?.Invoke(false);
		}

		public override void Tick()
		{
			if (!canReflect && MyTime.time > lastReflectedTime + cooldown)
			{
				canReflect = true;
				A_MirrorReady?.Invoke(true);
			}
		}

		private bool IsReady()
		{
			return canReflect;
		}

		private bool ReflectDamage(DamageContainer dc)
		{
			if (dc == null || dc.enemy == null) return false;
			var player = MyPlayer.Instance;
			if (player == null || player.inventory == null || player.inventory.statusEffects == null || EnemyManager.Instance == null) return false;
			canReflect = false;
			float size = PlayerStats.GetStat(EStat.SizeMultiplier);
			Vector3 direction = (dc.enemy.GetFeetPosition() - player.GetFeetPosition()).normalized;
			if (EffectManager.Instance != null && EffectManager.Instance.mirrorFx != null)
				EffectManager.Instance.SpawnMirrorFx(player.transform.position, direction, 5f * size);
			foreach (Collider collider in global::Utility.RaycastUtility.ConeCastAll(player.transform.position, direction, 10f * size, 60f))
			{
				if (!EnemyManager.Instance.GetEnemy(collider, out Enemy target) || target == null) continue;
				reuseDc.Reuse(1f, damageSource);
				reuseDc.damage = dc.damage * damageMultiplier * PlayerStats.GetStat(EStat.DamageMultiplier);
				reuseDc.enemy = dc.enemy;
				reuseDc.direction = direction;
				reuseDc.knockback = 3f * PlayerStats.GetStat(EStat.KnockbackMultiplier);
				target.DamageFromPlayerWeapon(reuseDc);
				if (EffectManager.Instance != null && PoolManager.Instance != null)
					EffectManager.Instance.EnemyHitEffect(collider.ClosestPoint(player.transform.position), direction, target, damageSource, null, true);
			}
			if (EffectManager.Instance != null && PoolManager.Instance != null)
				EffectManager.Instance.PopupText("REFLECT", MyColorUtility.aegisColor, player.transform.position, 22);
			lastReflectedTime = MyTime.time;
			var modifier = new StatModifier { stat = EStat.DamageReductionMultiplier, modifyType = EStatModifyType.Flat, modification = 1f };
			player.inventory.statusEffects.AddNewEffect(new StatusEffect(EStatusEffect.Invulnerability, MyTime.time + .5f, new[] { modifier }), .5f);
			A_MirrorReady?.Invoke(false);
			return true;
		}

		private void OnCheckStopDamage(DamageContainer dc, bool shieldDamage)
		{
			if (canReflect && ReflectDamage(dc)) dc.damage = (dc.flags & DcFlags.BossDamage) != 0 ? dc.damage * .25f : 0f;
		}

		public ItemMirror(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
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
