using System;
using System.Collections.Generic;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups.Xp_and_Levels;

namespace Assets.Scripts.UI.InGame.Levelup
{
	public class EncounterWindows : MonoBehaviour
	{
		public BaseEncounterWindow levelupScreen;

		public BaseEncounterWindow genericEncounterWindow;

		public BaseEncounterWindow chestWindow;

		public BaseEncounterWindow itemPickWindow;

		public BaseEncounterWindow microwaveWindow;

		private BaseEncounterWindow activeEncounterWindow;

		private Queue<EEncounter> rewardQueue = new Queue<EEncounter>();

		private bool closedEncounterThisFrame;

		private static List<EEncounter> nextMapQueue = new List<EEncounter>();

		public static Action A_WindowOpened;

		public static Action A_WindowClosed;

		public int currentLevel;

		public bool encounterInProgress { get; private set; }

		private void Awake()
		{
			PlayerXp.A_LevelUp += OnLevelUp;
			MyPlayer.A_PlayerInventoryInitialized += OnInventoryInitialized;
			GameManager.A_GameOver += OnGameOver;
			SpawnPlayerPortal.A_PortalClosed += OnPortalClosed;
		}

		private void OnDestroy()
		{
			PlayerXp.A_LevelUp -= OnLevelUp;
			MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInitialized;
			GameManager.A_GameOver -= OnGameOver;
			SpawnPlayerPortal.A_PortalClosed -= OnPortalClosed;
		}

		private void OnGameOver()
		{
			rewardQueue.Clear();
			nextMapQueue.Clear();
		}

		private void OnPortalClosed()
		{
			foreach (EEncounter queued in nextMapQueue) rewardQueue.Enqueue(queued);
			nextMapQueue.Clear();
			if (!encounterInProgress && rewardQueue.Count > 0) PopReward();
		}

		private bool QueueEncountersForNextMap()
		{
			return MyPlayer.Instance != null && MyPlayer.Instance.isTeleporting;
		}

		public bool HasEncounter()
		{
			return encounterInProgress || closedEncounterThisFrame;
		}

		public void AddEncounter(EEncounter rewardWindowType)
		{
			if (QueueEncountersForNextMap()) { nextMapQueue.Add(rewardWindowType); return; }
			rewardQueue.Enqueue(rewardWindowType);
			if (!encounterInProgress) PopReward();
		}

		public void RewardFinished()
		{
			if (activeEncounterWindow == null) return;
			activeEncounterWindow.OnClose();
			activeEncounterWindow = null;
			encounterInProgress = false;
			closedEncounterThisFrame = true;
			while (rewardQueue.Count > 1 && rewardQueue.Peek() == EEncounter.Levelup && IsPlayerMaxed()) rewardQueue.Dequeue();
			if (rewardQueue.Count > 0) PopReward();
			else { MyTime.Unpause(); A_WindowClosed?.Invoke(); }
		}

		private void PopReward()
		{
			if (MyPlayer.Instance == null || MyPlayer.Instance.IsDead() || rewardQueue.Count == 0) return;
			encounterInProgress = true;
			EEncounter kind = rewardQueue.Dequeue();
			switch (kind)
			{
				case EEncounter.Levelup: case EEncounter.Moai: case EEncounter.ShadyGuy: activeEncounterWindow = levelupScreen; break;
				case EEncounter.ChestNormal: case EEncounter.ChestFree: case EEncounter.ChestEvil: case EEncounter.ChestFreeCrypt: case EEncounter.ChestGhost: activeEncounterWindow = chestWindow; break;
				case EEncounter.Microwave: activeEncounterWindow = microwaveWindow; break;
				default: activeEncounterWindow = genericEncounterWindow; break;
			}
			MyTime.Pause();
			activeEncounterWindow?.Open(kind);
			A_WindowOpened?.Invoke();
		}

		private void OnInventoryInitialized(PlayerInventory inventory)
		{
			currentLevel = inventory.playerXp.level;
		}

		private void LateUpdate()
		{
			closedEncounterThisFrame = false;
		}

		private void Start()
		{
			if (MyPlayer.Instance != null && MyPlayer.Instance.inventory != null) currentLevel = MyPlayer.Instance.inventory.playerXp.level;
		}

		private void OnLevelUp(int level)
		{
			if (!IsPlayerMaxed())
			{
				var settings = SaveManager.Instance?.config?.cfGameSettings;
				bool autoSelect = settings != null && settings.auto_select_upgrades == 1 && currentLevel >= settings.auto_select_after_level;
				for (int i = currentLevel; i < level; i++)
				{
					if (!autoSelect || activeEncounterWindow == levelupScreen) AddEncounter(EEncounter.Levelup);
					else if (IsPlayerMaxed()) break;
					else UpgradePicker.AutoSelectUpgrade();
				}
			}
			currentLevel = level;
		}

		private bool IsPlayerMaxed()
		{
			var inventory = MyPlayer.Instance.inventory;
			return inventory.weaponInventory.IsMaxed() && inventory.tomeInventory.IsMaxed();
		}
	}
}
