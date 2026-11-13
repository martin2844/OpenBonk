using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Actors.Player;

public class InteractableGhostBossLeave : BaseInteractable
{
	public LocalizedString stringLeave;

	private bool hasInteracted;

	private bool isBossDead;

	private Vector3 teleportPosition;

	private Vector3 teleportDir;

	public void OpenDoor()
	{
		isBossDead = true;
	}

	public override bool Interact()
	{
		hasInteracted = true;
		MyPlayer.Instance.isTeleporting = true;
		TransitionUI.Instance.StartTransition(Teleport, 0.25f, 0f);
		return true;
	}

	private void Teleport()
	{
		MyPlayer.Instance.isTeleporting = false;
		MyPlayer.Instance.playerMovement.TeleportPlayerBackToBounds(teleportPosition);
		MyPlayer.Instance.playerInput.SetSpawnDirection(teleportDir);
		RsgController.Instance.ClearMap();
		RsgController.Instance.roomBoss.gameObject.SetActive(false);
	}

	public override string GetInteractString()
	{
		return stringLeave.GetLocalizedString();
	}

	public override bool CanInteract()
	{
		return !hasInteracted && isBossDead;
	}

	public void SetTeleportTransform(Vector3 pos, Vector3 dir)
	{
		teleportPosition = pos;
		teleportDir = -dir;
	}
}
