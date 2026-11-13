using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemCactus : ItemBase
	{
		private float damagePerAmount = 5f;

		private int numProjectilesPerAmount = 2;

		private float damage;

		private int numProjectiles;

		public static string damageSource = EItem.Cactus.ToString();

		private List<Vector3> projectileDirections = new List<Vector3>();

		protected static readonly RaycastHit[] raycastBuffer = new RaycastHit[1];

		protected override void OnInitOrAmountChanged()
		{
			damage = damagePerAmount * amount;
			numProjectiles = numProjectilesPerAmount * amount + 1;
		}

		public override void Init()
		{
			PlayerHealth.A_TakeDamage += OnTakeDamage;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_TakeDamage -= OnTakeDamage;
		}

		private void OnTakeDamage(PlayerHealth ph, DamageContainer dc, bool isShieldDamage)
		{
			float totalDamage = damage + PlayerStats.GetStat(EStat.Thorns);
			var player = MyPlayer.Instance;
			GameObject effect = PoolManager.Instance?.cactusPool?.Get();
			if (effect != null)
			{
				effect.transform.position = player.transform.position;
				effect.transform.rotation = player.playerRenderer.transform.rotation;
				var emission = effect.GetComponent<ParticleSystem>().emission;
				var burst = emission.GetBurst(0);
				burst.count = Mathf.Clamp(numProjectiles, 3, 40);
				emission.SetBurst(0, burst);
			}
			projectileDirections.Clear();
			Transform directionTransform = player.playerRenderer.transform;
			int count = Mathf.Clamp(numProjectiles, 2, 50);
			for (int i = 0; i < count; i++)
				projectileDirections.Add(Quaternion.AngleAxis(i * (360f / count), directionTransform.up) * directionTransform.forward);
			foreach (Vector3 direction in projectileDirections)
			{
				int hits = Physics.SphereCastNonAlloc(player.transform.position, .5f, direction, raycastBuffer, 15f, GameManager.Instance.whatIsEnemy, QueryTriggerInteraction.UseGlobal);
				for (int i = 0; i < hits; i++)
				{
					if (!EnemyManager.Instance.GetEnemy(raycastBuffer[i].collider, out var enemy)) continue;
					var hit = WeaponUtility.GetDamageContainer(null, totalDamage, .5f, damageSource, direction, enemy);
					enemy.DamageFromPlayerOther(hit);
					if (EffectManager.Instance != null) EffectManager.Instance.EnemyHitEffect(enemy.GetCenterPosition(), Vector3.zero, true, damageSource, null, true);
				}
			}
		}

		public ItemCactus(ItemInventory itemInventoryRef)
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, string> { ["stat1"] = EnumUtility.EnumToReadable(EStat.Thorns) ?? "" });
		}
	}
}
