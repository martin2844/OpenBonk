using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableCrypt : BaseInteractable
{
	public LocalizedString stringOpen;

	public LocalizedString stringFailed;

	private Enemy bossEnemy;

	private bool isDone;

	public override bool Interact()
	{
		if (isDone) return false;
		if (!IsOpen())
		{
			AlwaysUi.Instance.UiTextPopup.SetText(stringFailed.GetLocalizedString(), new Vector3(Screen.width * 0.55f, Screen.height * 0.55f, 0f), MyColorUtility.interactDisabledOutlineColor, 0.75f);
			return false;
		}
		isDone = true;
		MyPlayer.Instance.isTeleporting = true;
		TransitionUI.Instance.StartTransition(Teleport, 0.25f, 0f);
		AudioManager.Instance.PlayDungeonDoorEnter();
		MyPlayer.Instance.minimapCameraScript.RemoveArrow(transform);
		return true;
	}

	private void Teleport()
	{
		RsgController dungeon = RsgController.Instance;
		dungeon.Generate(MapGenerationController.mapSeed + 1, RsgController.EDungeonType.BossDungeon, out float traversalTime);
		Transform bossEntrance = dungeon.roomBoss.playerTeleportTransform;
		dungeon.rsgEnd.SetTeleportTransform(bossEntrance.position, bossEntrance.forward, bossEntrance.forward);
		GameManager.Instance.StartDungeon(traversalTime);
		MyPlayer.Instance.isTeleporting = false;
		Transform spawn = dungeon.rsgStart.spawnTransform;
		MyPlayer.Instance.TeleportPlayerImmediate(spawn.position, spawn.forward, spawn.forward, 20f);
		foreach (var entry in EnemyManager.Instance.enemies)
			if (entry.Value != null && !entry.Value.IsDead()) entry.Value.ReleaseToPoolNextFrame();
		MyTime.finalSwarmTimer = 0f;
		MyTime.stageTimer = 0f;
		EnemyManager.Instance.summonerController.StopFinalSwarm();
	}

	private void Update()
	{
	}

	public override string GetInteractString()
	{
		return stringOpen.GetLocalizedString();
	}

	private bool IsOpen()
	{
		return MyPlayer.Instance.inventory.itemInventory.GetAmount(EItem.CryptKey) > 3;
	}

	public override bool CanInteract()
	{
		return !isDone;
	}
}
