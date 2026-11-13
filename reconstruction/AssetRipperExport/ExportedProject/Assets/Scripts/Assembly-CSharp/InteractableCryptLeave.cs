using System;
using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.Utility;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.UI.HUD.Chatbox;

public class InteractableCryptLeave : BaseInteractable
{
	public LocalizedString stringLeave;

	private bool hasInteracted;

	public static Action<float> A_FirstDungeonCompleted;

	private RsgController.EDungeonType dungeonType;

	private Vector3 teleportPosition;

	private Vector3 teleportDir;

	private Vector3 teleportDirCamera;

	public override bool Interact()
	{
		hasInteracted = true;
		MyPlayer.Instance.isTeleporting = true;
		AudioManager.Instance.PlayDungeonDoorEnter();
		TransitionUI.Instance.StartTransition(Teleport, 0.25f, 0f);
		return true;
	}

	public void SetType(RsgController.EDungeonType dungeonType)
	{
		this.dungeonType = dungeonType;
	}

	private void Teleport()
	{
		GameManager.Instance.StopDungeon();
		MyPlayer.Instance.isTeleporting = false;
		MyPlayer.Instance.TeleportPlayerImmediate(teleportPosition, teleportDir, teleportDirCamera, 20f);
		if (dungeonType == RsgController.EDungeonType.BossDungeon)
		{
			RsgController.Instance.roomBoss.Activate();
			MusicController.Instance.StopMusic();
		}
		else
		{
			if (!ChallengesTracker.HasChallengeModifier("crypt")) UiManager.Instance.mapTile.StartAnimation();
			ShowTime();
			UiManager.Instance.objective.GraveyardKeys();
			A_FirstDungeonCompleted?.Invoke(MyTime.cryptTimer);
		}
		RsgController.Instance.ClearMap();
	}

	private void Update()
	{
	}

	private void ShowTime()
	{
		float time = MyTime.cryptTimer;
		float previousBest = 0f;
		bool personalBest = false;
		var save = SaveManager.Instance;
		var times = save != null && save.stats != null ? save.stats.times : null;
		if (times != null)
		{
			times.TryGetValue(ESpeedrunTime.Crypt, out previousBest);
			if (previousBest == 0f || time < previousBest)
			{
				times[ESpeedrunTime.Crypt] = time;
				save.SaveStats();
				personalBest = true;
			}
		}
		string formatted = TimerUtility.TimerToString(time);
		string message = personalBest
			? "[PB] Escape time: <color=#" + MyColorUtility.ColorToHex(MyColorUtility.GetItemRarityColor(EItemRarity.Legendary)) + ">" + formatted + "</color>!"
			: "Escape time: " + formatted + "!";
		if (previousBest > 0f)
		{
			float delta = time - previousBest;
			string deltaText = delta.ToString("F2");
			if (delta >= 0f) deltaText = "+" + deltaText;
			message += " (<color=" + (personalBest ? MyColorUtility.positiveColorString : MyColorUtility.negativeColorString) + ">" + deltaText + "</color>)";
		}
		if (UiManager.Instance != null && UiManager.Instance.feed != null)
			UiManager.Instance.feed.SetFeed(message, 8.5f);
	}

	public override string GetInteractString()
	{
		return stringLeave.GetLocalizedString();
	}

	public override bool CanInteract()
	{
		return !hasInteracted;
	}

	public void SetTeleportTransform(Vector3 pos, Vector3 dir, Vector3 cameraDir)
	{
		teleportPosition = pos;
		teleportDir = dir;
		teleportDirCamera = cameraDir;
	}
}
