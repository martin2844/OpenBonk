using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public class ItemSuckyHoof : ItemBase
	{
		private float range = 100f;

		private float interval;

		private float nextSuckTime;

		protected override void OnInitOrAmountChanged()
		{
			interval = 15f / amount;
			range = 100f + 50f * amount;
		}

		public override void Tick()
		{
			if (MyTime.time < nextSuckTime) return;
			nextSuckTime = MyTime.time + interval;
			if (EffectManager.Instance != null) EffectManager.Instance.MagnetEffect();
			var player = MyPlayer.Instance;
			if (player == null || PickupManager.Instance == null) return;
			foreach (Collider hit in Physics.OverlapSphere(player.transform.position, range, PickupManager.Instance.whatIsPickups))
			{
				if (hit == null) continue;
				Pickup pickup = hit.GetComponent<Pickup>();
				if (pickup != null && pickup.ePickup == EPickup.Xp) pickup.StartFollowingPlayer(player.transform);
			}
		}

		public ItemSuckyHoof(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
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
