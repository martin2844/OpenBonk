using System;
using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Inventory__Items__Pickups.Interactables
{
	public class OpenChest : MonoBehaviour
	{
		public EChest chestType;

		private float delay = .6f;

		private float readyForPickupTime;

		private bool pickedup;

		public static Action A_Open;

		private void Awake()
		{
			readyForPickupTime = MyTime.time + delay;
		}

		private void OnTriggerStay(Collider other)
		{
			if (!CanPickup() || other == null || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
			if (SaveManager.Instance.config.cfGameSettings.skip_chest_animation == 1) ChestUtility.OpenChestNoAnimation(chestType);
			else UiManager.Instance.encounterWindows.AddEncounter(ChestUtility.ChestTypeToEncounter(chestType));
			gameObject.SetActive(false);
			A_Open?.Invoke();
		}

		private bool CanPickup()
		{
			return !pickedup && MyTime.time > readyForPickupTime;
		}
	}
}
