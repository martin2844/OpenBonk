using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Pickups
{
	public class PickupStackableList
	{
		private int maxObjects;

		private EPickup ePickup;

		public LinkedList<Pickup> pickupsList;

		private int combineThreshold;

		public PickupStackableList(int nMax, EPickup ePickup)
		{
			maxObjects = nMax;
			this.ePickup = ePickup;
			pickupsList = new LinkedList<Pickup>();
			combineThreshold = 500;
		}

		public Pickup AddPickup(Vector3 pos)
		{
			if (pickupsList.Count >= maxObjects) CombineOldestObjects();
			Pickup pickup = PickupManager.Instance.GetNewPickup(ePickup, pos).GetComponent<Pickup>();
			pickup.linkedListNode = pickupsList.AddLast(pickup);
			return pickup;
		}

		private void CombineOldestObjects()
		{
			var oldest = pickupsList.First;
			var next = oldest?.Next;
			if (next == null) return;
			next.Value.AddValue(oldest.Value);
			pickupsList.Remove(oldest);
			oldest.Value.linkedListNode = null;
			PickupManager.Instance.DespawnPickup(oldest.Value);
			if (next.Value.GetValue() >= combineThreshold)
			{
				pickupsList.Remove(next);
				pickupsList.AddLast(next);
			}
		}

		public void RemovePickup(Pickup pickup)
		{
			pickupsList.Remove(pickup);
		}
	}
}
